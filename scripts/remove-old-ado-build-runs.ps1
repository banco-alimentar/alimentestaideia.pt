# TEMPORARY: Prune Azure DevOps build runs for a pipeline definition.
#
# Default target: developer-debug (definition id=11)
# https://dev.azure.com/BancoAlimentar/Alimentestaideia.pt/_build?definitionId=11
#
# Rules (same as remove-old-github-workflow-runs.ps1):
#   1. Delete every build older than 30 days (by queue time, UTC).
#   2. For each calendar day within the retention window, keep only the last
#      completed build; delete all other finished builds from that day.
#
# In-progress / queued builds are never deleted.
#
# Requires: AZDO_PAT or AZURE_DEVOPS_EXT_PAT with Build (Read & execute), including delete builds.
#
# Usage:
#   $env:AZDO_PAT = "<pat>"
#   .\scripts\remove-old-ado-build-runs.ps1
#   .\scripts\remove-old-ado-build-runs.ps1 -Confirm
#   .\scripts\remove-old-ado-build-runs.ps1 -DefinitionId 11 -RetentionDays 30 -Confirm
#
# Delete this script after the one-off cleanup.

[CmdletBinding()]
param(
    [string]$Organization = 'BancoAlimentar',
    [string]$Project = 'Alimentestaideia.pt',
    [int]$DefinitionId = 11,
    [int]$RetentionDays = 30,
    [string]$Pat = $env:AZDO_PAT,
    [switch]$Confirm
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$apiVersion = '7.1'

function Get-AdoAuthHeader {
    param([string]$Token)

    return @{
        Authorization = 'Basic ' + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$Token"))
    }
}

function Invoke-AdoGetPage {
    param(
        [hashtable]$Headers,
        [string]$Uri
    )

    $response = Invoke-WebRequest -Uri $Uri -Headers $Headers -Method Get -UseBasicParsing
    $body = $response.Content | ConvertFrom-Json
    $continuationToken = $null

    if ($response.Headers.ContainsKey('x-ms-continuationtoken')) {
        $continuationToken = $response.Headers['x-ms-continuationtoken']
        if ($continuationToken -is [string[]]) {
            $continuationToken = $continuationToken[0]
        }
    }

    return [pscustomobject]@{
        Body              = $body
        ContinuationToken = $continuationToken
    }
}

function Invoke-AdoDelete {
    param(
        [hashtable]$Headers,
        [string]$Uri
    )

    Invoke-WebRequest -Uri $Uri -Headers $Headers -Method Delete -UseBasicParsing | Out-Null
}

function Get-AllBuilds {
    param(
        [hashtable]$Headers,
        [string]$BaseUrl,
        [string]$ProjectSegment,
        [int]$BuildDefinitionId
    )

    $builds = [System.Collections.Generic.List[object]]::new()
    $continuationToken = $null

    do {
        $query = "definitions=$BuildDefinitionId&queryOrder=queueTimeDescending&`$top=100&api-version=$apiVersion"
        if (-not [string]::IsNullOrWhiteSpace($continuationToken)) {
            $query += "&continuationToken=$([uri]::EscapeDataString($continuationToken))"
        }

        $uri = "$BaseUrl/$ProjectSegment/_apis/build/builds?$query"
        $page = Invoke-AdoGetPage -Headers $Headers -Uri $uri

        if ($null -eq $page.Body.value -or $page.Body.value.Count -eq 0) {
            break
        }

        foreach ($build in $page.Body.value) {
            $queueTime = if ($build.queueTime) {
                [datetimeoffset]::Parse($build.queueTime).UtcDateTime
            }
            else {
                [datetimeoffset]::Parse($build.lastChangedDate).UtcDateTime
            }

            $finishTime = if ($build.finishTime) {
                [datetimeoffset]::Parse($build.finishTime).UtcDateTime
            }
            else {
                $queueTime
            }

            $definitionName = if ($build.definition) { [string]$build.definition.name } else { 'unknown' }

            $builds.Add([pscustomobject]@{
                Id          = [int64]$build.id
                Name        = $definitionName
                BuildNumber = [string]$build.buildNumber
                Status      = [string]$build.status
                Result      = [string]$build.result
                CreatedAt   = $queueTime
                UpdatedAt   = $finishTime
                WebUrl      = if ($build._links.web.href) { [string]$build._links.web.href } else { '' }
            })
        }

        $continuationToken = $page.ContinuationToken
    } while (-not [string]::IsNullOrWhiteSpace($continuationToken))

    return $builds
}

function Test-IsActiveRun {
    param([string]$Status)

    return $Status -in @('inProgress', 'cancelling', 'notStarted', 'postponed')
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

    foreach ($run in $Runs) {
        if ($run.CreatedAt -lt $CutoffUtc) {
            Add-DeleteCandidate -Run $run -Reason 'older than retention window'
        }
    }

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

            Add-DeleteCandidate -Run $run -Reason "same-day duplicate (keeping build $($keeper.Id) on $($dayGroup.Name))"
        }
    }

    $deleteList = $Runs | Where-Object { $toDelete.Contains($_.Id) } | Sort-Object CreatedAt
    return [pscustomobject]@{
        Runs    = $deleteList
        Reasons = $reasons
    }
}

if ([string]::IsNullOrWhiteSpace($Pat)) {
    $Pat = $env:AZURE_DEVOPS_EXT_PAT
}

if ([string]::IsNullOrWhiteSpace($Pat)) {
    throw 'Set AZDO_PAT or AZURE_DEVOPS_EXT_PAT with Build (Read & execute), including permission to delete builds.'
}

$projectSegment = [uri]::EscapeDataString($Project)
$baseUrl = "https://dev.azure.com/$Organization"
$headers = Get-AdoAuthHeader -Token $Pat
$cutoffUtc = (Get-Date).ToUniversalTime().Date.AddDays(-$RetentionDays)

Write-Host "Organization : $Organization"
Write-Host "Project      : $Project"
Write-Host "Definition   : id=$DefinitionId (developer-debug)"
Write-Host "Retention    : keep builds queued on or after $($cutoffUtc.ToString('yyyy-MM-dd')) UTC"
Write-Host "Pipeline URL : $baseUrl/$projectSegment/_build?definitionId=$DefinitionId"
Write-Host 'Fetching builds...'

$definition = Invoke-RestMethod -Uri "$baseUrl/$projectSegment/_apis/build/definitions/$DefinitionId?api-version=$apiVersion" -Headers $headers -Method Get
Write-Host "Pipeline name: $($definition.name)"
Write-Host ''

$allRuns = Get-AllBuilds -Headers $headers -BaseUrl $baseUrl -ProjectSegment $projectSegment -BuildDefinitionId $DefinitionId
Write-Host "Loaded $($allRuns.Count) build(s)."

if ($allRuns.Count -eq 0) {
    Write-Host 'Nothing to do.'
    exit 0
}

$plan = Get-RunsToDelete -Runs $allRuns -CutoffUtc $cutoffUtc
$deleteRuns = @($plan.Runs)

Write-Host ''
Write-Host "Builds to delete: $($deleteRuns.Count)"
Write-Host "Builds to keep:   $($allRuns.Count - $deleteRuns.Count)"
Write-Host ''

if ($deleteRuns.Count -eq 0) {
    Write-Host 'Nothing to delete.'
    exit 0
}

$deleteRuns |
    Sort-Object CreatedAt |
    ForEach-Object {
        $reason = $plan.Reasons[$_.Id]
        $label = if ($_.Result) { "$($_.Status)/$($_.Result)" } else { $_.Status }
        Write-Host ("  {0:yyyy-MM-dd HH:mm}Z  #{1,-8}  {2,-22}  build {3}  ({4})" -f `
            $_.CreatedAt, $_.Id, $label, $_.BuildNumber, $reason)
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
        $deleteUri = "$baseUrl/$projectSegment/_apis/build/builds/$($run.Id)?api-version=$apiVersion"
        Invoke-AdoDelete -Headers $headers -Uri $deleteUri
        $deleted++
        Write-Host "Deleted build $($run.Id) ($($run.BuildNumber))"
    }
    catch {
        $failed++
        Write-Warning "Failed to delete build $($run.Id): $($_.Exception.Message)"
    }

    Start-Sleep -Milliseconds 250
}

Write-Host ''
Write-Host "Done. Deleted: $deleted. Failed: $failed."

if ($failed -gt 0) {
    exit 1
}
