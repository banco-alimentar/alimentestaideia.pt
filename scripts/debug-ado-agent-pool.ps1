# Temporary diagnostic script for Azure DevOps agent pool queueing.
# Usage:
#   $env:AZDO_PAT = "<personal-access-token>"
#   .\scripts\debug-ado-agent-pool.ps1
#   .\scripts\debug-ado-agent-pool.ps1 -Organization BancoAlimentar -PoolName Default
#
# PAT scopes: Agent Pools (Read), Build (Read), Project and Team (Read)

[CmdletBinding()]
param(
    [string]$Organization = "BancoAlimentar",
    [string]$Project = "Alimentestaideia.pt",
    [string]$PoolName = "Default",
    [string]$Pat = $env:AZDO_PAT,
    [switch]$IncludeAllPools
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($Pat)) {
    $Pat = $env:AZURE_DEVOPS_EXT_PAT
}

if ([string]::IsNullOrWhiteSpace($Pat)) {
    throw "Set AZDO_PAT or AZURE_DEVOPS_EXT_PAT to a Personal Access Token with read access to pools and builds."
}

$apiVersion = "7.1"
$jobRequestApiVersion = "7.1-preview.1"
$baseUrl = "https://dev.azure.com/$Organization"

function Invoke-AdoGet {
    param(
        [Parameter(Mandatory = $true)][string]$Uri
    )

    $headers = @{
        Authorization = "Basic " + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$Pat"))
    }

    return Invoke-RestMethod -Uri $Uri -Headers $headers -Method Get
}

function Write-Section {
    param([string]$Title)
    Write-Host ""
    Write-Host ("=" * 80) -ForegroundColor Cyan
    Write-Host $Title -ForegroundColor Cyan
    Write-Host ("=" * 80) -ForegroundColor Cyan
}

function Format-When {
    param($Value)
    if ($null -eq $Value) { return "-" }
    return $Value
}

function Get-AdoProperty {
    param(
        [Parameter(Mandatory = $true)]$Object,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if ($null -eq $Object) { return $null }
    if ($Object.PSObject.Properties.Match($Name).Count -eq 0) { return $null }
    return $Object.$Name
}

function Get-JobRequestState {
    param($Request)

    $assignTime = Get-AdoProperty $Request "assignTime"
    $finishTime = Get-AdoProperty $Request "finishTime"
    $result = Get-AdoProperty $Request "result"

    if ($null -ne $assignTime -and $null -eq $finishTime) { return "RUNNING" }
    if ($null -eq $assignTime) { return "QUEUED" }
    if ($null -ne $result) { return "OTHER ($result)" }
    return "OTHER"
}

Write-Section "Azure DevOps agent pool diagnostics"
Write-Host "Organization : $Organization"
Write-Host "Project      : $Project"
Write-Host "Pool filter  : $PoolName"
Write-Host "Time (UTC)   : $((Get-Date).ToUniversalTime().ToString('u'))"

Write-Section "Organization / parallel jobs (manual check)"
Write-Host "Parallel job limits are not exposed reliably via REST for all orgs."
Write-Host "Open as org admin:"
Write-Host "  $baseUrl/_settings/agentpools"
Write-Host "  $baseUrl/_admin/_buildQueue?_a=resourceLimits"
Write-Host "Look for 'Microsoft-hosted parallel jobs' (purchased vs in use)."

Write-Section "Agent pools"
$poolsResponse = Invoke-AdoGet -Uri "$baseUrl/_apis/distributedtask/pools?api-version=$apiVersion"
$pools = @($poolsResponse.value | Sort-Object name)

if ($pools.Count -eq 0) {
    Write-Warning "No agent pools returned. Check PAT scope 'Agent Pools (Read)' and organization name."
    exit 1
}

$pools | ForEach-Object {
    $hosted = if ($_.isHosted) { "hosted" } else { "self-hosted" }
    Write-Host ("[{0}] id={1} poolType={2} size={3} {4}" -f $_.name, $_.id, $_.poolType, $_.size, $hosted)
}

$targetPools = @($pools | Where-Object { $IncludeAllPools -or $_.name -eq $PoolName })
if ($targetPools.Count -eq 0) {
    Write-Warning "Pool '$PoolName' not found. Re-run with -IncludeAllPools or fix -PoolName."
    Write-Host "Available pools: $($pools.name -join ', ')"
    exit 1
}

foreach ($pool in $targetPools) {
    Write-Section "Pool '$($pool.name)' (id=$($pool.id))"
    Write-Host "isHosted     : $($pool.isHosted)"
    Write-Host "poolType     : $($pool.poolType)"
    Write-Host "size (quota) : $($pool.size)"

    $jobRequestsUri = $baseUrl + "/_apis/distributedtask/pools/" + $pool.id + "/jobrequests?completedRequestCount=0&api-version=" + $jobRequestApiVersion
    $jobRequestsResponse = Invoke-AdoGet -Uri $jobRequestsUri
    $jobRequests = @($jobRequestsResponse.value)

    Write-Host ""
    Write-Host "Agents:" -ForegroundColor Yellow
    $agentsResponse = Invoke-AdoGet -Uri "$baseUrl/_apis/distributedtask/pools/$($pool.id)/agents?api-version=$apiVersion"
    $agents = @($agentsResponse.value)

    if ($agents.Count -eq 0) {
        Write-Host "  (no agents registered - typical for empty self-hosted pool or MS-hosted pool listing)"
    }
    else {
        $agents | Sort-Object name | ForEach-Object {
            $enabled = if ($_.enabled) { "enabled" } else { "DISABLED" }
            Write-Host ("  {0} | status={1} | {2} | version={3}" -f $_.name, $_.status, $enabled, $_.version)
        }

        $online = @($agents | Where-Object { $_.status -eq "online" }).Count
        $offline = @($agents | Where-Object { $_.status -eq "offline" }).Count
        Write-Host ""
        Write-Host "Agent summary: online=$online offline=$offline total=$($agents.Count)"
        if ($pool.isHosted) {
            Write-Host ""
            Write-Host "Note: Microsoft-hosted agents show offline when idle. They spin up when a job is queued." -ForegroundColor DarkGray
            if ($jobRequests.Count -gt 0 -and $online -eq 0) {
                Write-Host "Jobs are queued but no agent yet - check org parallel job limits (link above)." -ForegroundColor Yellow
            }
        }
        elseif ($online -eq 0 -and $agents.Count -gt 0) {
            Write-Host ""
            Write-Host "WARNING: No online agents in this self-hosted pool. Queued jobs cannot start until an agent comes online." -ForegroundColor Red
        }
    }

    Write-Host ""
    Write-Host "Active job requests (queued / running on this pool):" -ForegroundColor Yellow

    if ($jobRequests.Count -eq 0) {
        Write-Host "  (no queued or running job requests)"
    }
    else {
        $jobRequests | Sort-Object { Get-AdoProperty $_ "queueTime" } | ForEach-Object {
            $definition = Get-AdoProperty $_ "definition"
            $definitionName = if ($null -ne $definition) { Get-AdoProperty $definition "name" } else { "(no definition name)" }
            $ownerObj = Get-AdoProperty $_ "owner"
            $owner = if ($null -ne $ownerObj) { Get-AdoProperty $ownerObj "name" } else { Get-AdoProperty $_ "serviceOwner" }
            $reservedAgent = Get-AdoProperty $_ "reservedAgent"
            $agentName = if ($null -ne $reservedAgent) { Get-AdoProperty $reservedAgent "name" } else { "-" }
            $state = Get-JobRequestState $_

            Write-Host ""
            Write-Host "  [$state] $definitionName"
            Write-Host "    requestId   : $(Get-AdoProperty $_ 'requestId')"
            Write-Host "    owner       : $(Format-When $owner)"
            Write-Host "    queueTime   : $(Format-When (Get-AdoProperty $_ 'queueTime'))"
            Write-Host "    assignTime  : $(Format-When (Get-AdoProperty $_ 'assignTime'))"
            Write-Host "    finishTime  : $(Format-When (Get-AdoProperty $_ 'finishTime'))"
            Write-Host "    planId/jobId: $(Get-AdoProperty $_ 'planId') / $(Get-AdoProperty $_ 'jobId')"
            Write-Host "    agent       : $agentName"
            Write-Host "    result      : $(Format-When (Get-AdoProperty $_ 'result'))"
        }

        $queuedCount = @($jobRequests | Where-Object {
            $null -eq (Get-AdoProperty $_ "assignTime") -and $null -eq (Get-AdoProperty $_ "finishTime")
        }).Count
        $runningCount = @($jobRequests | Where-Object {
            $null -ne (Get-AdoProperty $_ "assignTime") -and $null -eq (Get-AdoProperty $_ "finishTime")
        }).Count
        Write-Host ""
        Write-Host "Active job summary: queued=$queuedCount running=$runningCount total=$($jobRequests.Count)"
    }
}

Write-Section "In-progress classic builds (all projects in org)"
$projectsResponse = Invoke-AdoGet -Uri ($baseUrl + "/_apis/projects?api-version=" + $apiVersion + "&`$top=100")
$projects = @($projectsResponse.value | Sort-Object name)
$totalInProgress = 0

foreach ($proj in $projects) {
    $projectSegment = [uri]::EscapeDataString($proj.name)
    $buildsUri = $baseUrl + "/" + $projectSegment + "/_apis/build/builds?statusFilter=inProgress&`$top=25&api-version=" + $apiVersion
    try {
        $buildsResponse = Invoke-AdoGet -Uri $buildsUri
        $builds = @($buildsResponse.value)
        if ($builds.Count -eq 0) { continue }

        Write-Host ""
        Write-Host "Project: $($proj.name)" -ForegroundColor Yellow
        foreach ($build in $builds) {
            $totalInProgress++
            $defName = if ($build.definition) { $build.definition.name } else { "build #$($build.id)" }
            Write-Host ("  [{0}] id={1} def='{2}' requestedBy={3} queueTime={4} url={5}" -f `
                $build.status, $build.id, $defName, $build.requestedBy.displayName, `
                (Format-When $build.queueTime), $build._links.web.href)
        }
    }
    catch {
        Write-Warning "Could not read builds for project '$($proj.name)': $($_.Exception.Message)"
    }
}

if ($totalInProgress -eq 0) {
    Write-Host "No in-progress classic builds found across $($projects.Count) projects."
}
else {
    Write-Host ""
    Write-Host "Total in-progress classic builds: $totalInProgress"
}

Write-Section "Recent YAML pipeline runs (state=inProgress, all projects)"
$totalPipelineRuns = 0

foreach ($proj in $projects) {
    $projectSegment = [uri]::EscapeDataString($proj.name)
    $runsUri = $baseUrl + "/" + $projectSegment + "/_apis/pipelines/runs?`$top=30&api-version=" + $apiVersion
    try {
        $runsResponse = Invoke-AdoGet -Uri $runsUri
        $runs = @($runsResponse.value | Where-Object { $_.state -eq "inProgress" })
        if ($runs.Count -eq 0) { continue }

        Write-Host ""
        Write-Host "Project: $($proj.name)" -ForegroundColor Yellow
        foreach ($run in $runs) {
            $totalPipelineRuns++
            $pipeName = if ($run.pipeline) { $run.pipeline.name } else { "pipeline run $($run.id)" }
            Write-Host ("  [{0}] runId={1} pipeline='{2}' created={3} url={4}" -f `
                $run.state, $run.id, $pipeName, (Format-When $run.createdDate), $run._links.web.href)
        }
    }
    catch {
        Write-Warning "Could not read pipeline runs for project '$($proj.name)': $($_.Exception.Message)"
    }
}

if ($totalPipelineRuns -eq 0) {
    Write-Host "No in-progress YAML pipeline runs found (last 30 runs per project scanned)."
}
else {
    Write-Host ""
    Write-Host "Total in-progress YAML pipeline runs: $totalPipelineRuns"
}

Write-Section "Interpretation"
Write-Host @"
- If job requests show RUNNING entries, those definitions/agents are consuming the pool.
- If you are QUEUED at position 1 but 'running' is empty, org parallel job limit may be 1 and
  the slot is held by a job in another project, or a stuck run not visible here (cancel old runs).
- Pool 'size' is quota/capacity metadata; MS-hosted parallelism is usually org-wide, not per pool.
- Self-hosted 'Default' with 1 online agent => only one job at a time across the org for that pool.
- Delete this script when finished: scripts/debug-ado-agent-pool.ps1
"@
