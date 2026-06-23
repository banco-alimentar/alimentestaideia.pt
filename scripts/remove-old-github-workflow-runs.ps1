# TEMPORARY: Prune GitHub Actions workflow runs for the current repo.
#
# Rules:
#   1. Delete every run older than 30 days (by created date, UTC).
#   2. For each calendar day within the retention window, keep only the last
#      completed run; delete all other finished runs from that day.
#
# In-progress / queued runs are never deleted.
#
# Requires: GitHub CLI (gh) authenticated with repo admin (delete runs).
#
# Usage:
#   .\scripts\remove-old-github-workflow-runs.ps1              # preview only
#   .\scripts\remove-old-github-workflow-runs.ps1 -Confirm       # delete for real
#   .\scripts\remove-old-github-workflow-runs.ps1 -Repo banco-alimentar/alimentestaideia.pt -RetentionDays 30 -Confirm
#
# Delete this script after the one-off cleanup.

[CmdletBinding()]
param(
    [string]$Repo = '',
    [int]$RetentionDays = 30,
    [switch]$Confirm
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-RepoSlug {
    if (-not [string]::IsNullOrWhiteSpace($Repo)) {
        return $Repo
    }

    return (gh repo view --json nameWithOwner -q .nameWithOwner)
}

function Get-AllWorkflowRuns {
    param([string]$RepoSlug)

    $runs = [System.Collections.Generic.List[object]]::new()
    $page = 1

    do {
        $uri = "repos/$RepoSlug/actions/runs?per_page=100&page=$page"
        $response = gh api $uri | ConvertFrom-Json
        if ($null -eq $response.workflow_runs -or $response.workflow_runs.Count -eq 0) {
            break
        }

        foreach ($run in $response.workflow_runs) {
            $runs.Add([pscustomobject]@{
                Id          = [int64]$run.id
                Name        = [string]$run.name
                RunNumber   = [int]$run.run_number
                Status      = [string]$run.status
                Conclusion  = [string]$run.conclusion
                CreatedAt   = [datetimeoffset]::Parse($run.created_at).UtcDateTime
                UpdatedAt   = [datetimeoffset]::Parse($run.updated_at).UtcDateTime
                HtmlUrl     = [string]$run.html_url
            })
        }

        $page++
    } while ($response.workflow_runs.Count -eq 100)

    return $runs
}

function Test-IsActiveRun {
    param([string]$Status)

    return $Status -in @('queued', 'in_progress', 'pending', 'waiting', 'requested', 'action_required')
}

function Get-RunsToDelete {
    param(
        [System.Collections.Generic.List[object]]$Runs,
        [datetime]$CutoffUtc
    )

    $toDelete = [System.Collections.Generic.HashSet[int64]]::new()
    $reasons = @{}

    function Add-DeleteCandidate {
        param(
            [object]$Run,
            [string]$Reason
        )

        if (Test-IsActiveRun -Status $Run.Status) {
            return
        }

        [void]$toDelete.Add($Run.Id)
        $reasons[$Run.Id] = $Reason
    }

    # Rule 1: everything older than the retention window.
    foreach ($run in $Runs) {
        if ($run.CreatedAt -lt $CutoffUtc) {
            Add-DeleteCandidate -Run $run -Reason 'older than retention window'
        }
    }

    # Rule 2: within the window, keep only the last completed run per UTC day.
    $recentRuns = $Runs | Where-Object { $_.CreatedAt -ge $CutoffUtc }
    $runsByDay = $recentRuns | Group-Object { $_.CreatedAt.ToString('yyyy-MM-dd') }

    foreach ($dayGroup in $runsByDay) {
        $completedRuns = @(
            $dayGroup.Group |
                Where-Object { $_.Status -eq 'completed' } |
                Sort-Object UpdatedAt -Descending
        )

        if ($completedRuns.Count -eq 0) {
            continue
        }

        $keeper = $completedRuns[0]
        foreach ($run in $dayGroup.Group) {
            if ($run.Id -eq $keeper.Id) {
                continue
            }

            Add-DeleteCandidate -Run $run -Reason "same-day duplicate (keeping run $($keeper.Id) on $($dayGroup.Name))"
        }
    }

    $deleteList = $Runs | Where-Object { $toDelete.Contains($_.Id) } | Sort-Object CreatedAt
    return [pscustomobject]@{
        Runs    = $deleteList
        Reasons = $reasons
    }
}

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw 'GitHub CLI (gh) is not installed or not on PATH.'
}

$null = gh auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    throw 'gh is not authenticated. Run: gh auth login'
}

$repoSlug = Get-RepoSlug
$cutoffUtc = (Get-Date).ToUniversalTime().Date.AddDays(-$RetentionDays)

Write-Host "Repository: $repoSlug"
Write-Host "Retention: keep runs created on or after $($cutoffUtc.ToString('yyyy-MM-dd')) UTC"
Write-Host "Fetching workflow runs..."

$allRuns = Get-AllWorkflowRuns -RepoSlug $repoSlug
Write-Host "Loaded $($allRuns.Count) run(s)."

if ($allRuns.Count -eq 0) {
    Write-Host 'Nothing to do.'
    exit 0
}

$plan = Get-RunsToDelete -Runs $allRuns -CutoffUtc $cutoffUtc
$deleteRuns = @($plan.Runs)

Write-Host ''
Write-Host "Runs to delete: $($deleteRuns.Count)"
Write-Host "Runs to keep:   $($allRuns.Count - $deleteRuns.Count)"
Write-Host ''

if ($deleteRuns.Count -eq 0) {
    Write-Host 'Nothing to delete.'
    exit 0
}

$deleteRuns |
    Sort-Object CreatedAt |
    ForEach-Object {
        $reason = $plan.Reasons[$_.Id]
        Write-Host ("  {0:yyyy-MM-dd HH:mm}Z  #{1,-6}  {2,-12}  {3}  ({4})" -f `
            $_.CreatedAt, $_.Id, $_.Status, $_.Name, $reason)
    }

if (-not $Confirm) {
    Write-Host ''
    Write-Host 'Preview only. Re-run with -Confirm to delete.'
    exit 0
}

Write-Host ''
Write-Host 'Deleting...'

$deleted = 0
$failed = 0

foreach ($run in $deleteRuns) {
    try {
        gh api --method DELETE "repos/$repoSlug/actions/runs/$($run.Id)" | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "gh api exited with code $LASTEXITCODE"
        }

        $deleted++
        Write-Host "Deleted run $($run.Id) ($($run.Name))"
    }
    catch {
        $failed++
        Write-Warning "Failed to delete run $($run.Id): $($_.Exception.Message)"
    }

    Start-Sleep -Milliseconds 250
}

Write-Host ''
Write-Host "Done. Deleted: $deleted. Failed: $failed."

if ($failed -gt 0) {
    exit 1
}
