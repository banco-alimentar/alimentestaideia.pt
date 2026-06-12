# Switch Azure DevOps build and release definitions from self-hosted "Default"
# to Microsoft-hosted "Azure Pipelines" (windows-latest).
#
# Usage:
#   $env:AZDO_PAT = "<pat with Build (Read & execute), Release (Read, write, & execute), Agent Pools (Read)>"
#   .\scripts\switch-ado-pools-to-hosted.ps1 -WhatIf
#   .\scripts\switch-ado-pools-to-hosted.ps1
#
# After running, re-queue or cancel/re-run stuck builds in Azure DevOps.

[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$Organization = "BancoAlimentar",
    [string]$Project = "Alimentestaideia.pt",
    [string]$FromPoolName = "Default",
    [string]$ToPoolName = "Azure Pipelines",
    [string]$Pat = $env:AZDO_PAT
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($Pat)) {
    $Pat = $env:AZURE_DEVOPS_EXT_PAT
}

if ([string]::IsNullOrWhiteSpace($Pat)) {
    throw "Set AZDO_PAT or AZURE_DEVOPS_EXT_PAT with Build + Release + Agent Pools scopes."
}

$apiVersion = "7.1"
$projectSegment = [uri]::EscapeDataString($Project)
$baseUrl = "https://dev.azure.com/$Organization"
$releaseBaseUrl = "https://vsrm.dev.azure.com/$Organization"
$authHeader = "Basic " + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$Pat"))

function Invoke-AdoApi {
    param(
        [Parameter(Mandatory = $true)][string]$Uri,
        [ValidateSet("Get", "Put")]
        [string]$Method = "Get",
        $Body = $null
    )

    $params = @{
        Uri     = $Uri
        Method  = $Method
        Headers = @{ Authorization = $authHeader }
    }

    if ($null -ne $Body) {
        $params.Body = ($Body | ConvertTo-Json -Depth 100 -Compress)
        $params.ContentType = "application/json"
    }

    return Invoke-RestMethod @params
}

function Write-Section {
    param([string]$Title)
    Write-Host ""
    Write-Host ("=" * 80) -ForegroundColor Cyan
    Write-Host $Title -ForegroundColor Cyan
    Write-Host ("=" * 80) -ForegroundColor Cyan
}

function Get-PoolByName {
    param([string]$Name)

    $poolsResponse = Invoke-AdoApi -Uri "$baseUrl/_apis/distributedtask/pools?api-version=$apiVersion"
    $pool = @($poolsResponse.value | Where-Object { $_.name -eq $Name } | Select-Object -First 1)
    if ($pool.Count -eq 0) {
        throw "Agent pool '$Name' was not found."
    }

    return $pool
}

function Get-ProjectQueueForPool {
    param(
        [int]$PoolId,
        [string]$PoolName
    )

    $queuesUri = "$baseUrl/$projectSegment/_apis/distributedtask/queues?poolId=$PoolId&api-version=$apiVersion"
    $queuesResponse = Invoke-AdoApi -Uri $queuesUri
    $queue = @($queuesResponse.value | Sort-Object id | Select-Object -First 1)
    if ($queue.Count -eq 0) {
        throw "No queue found for pool '$PoolName' (id=$PoolId) in project '$Project'."
    }

    return $queue
}

function Test-UsesPool {
    param(
        $QueueObject,
        [int]$PoolId,
        [string]$PoolName
    )

    if ($null -eq $QueueObject) { return $false }

    $queuePoolId = $null
    if ($QueueObject.PSObject.Properties.Match("pool").Count -gt 0 -and $null -ne $QueueObject.pool) {
        $queuePoolId = $QueueObject.pool.id
    }

    if ($null -ne $queuePoolId) {
        return $queuePoolId -eq $PoolId
    }

    return $QueueObject.name -eq $PoolName
}

Write-Section "Switch agent pools to Microsoft-hosted"
Write-Host "Organization : $Organization"
Write-Host "Project      : $Project"
Write-Host "From pool    : $FromPoolName"
Write-Host "To pool      : $ToPoolName"
Write-Host "WhatIf       : $WhatIfPreference"

$fromPool = Get-PoolByName -Name $FromPoolName
$toPool = Get-PoolByName -Name $ToPoolName

if ($fromPool.isHosted) {
    Write-Warning "Source pool '$FromPoolName' is already Microsoft-hosted. Nothing to migrate."
    exit 0
}

if (-not $toPool.isHosted) {
    throw "Target pool '$ToPoolName' is not Microsoft-hosted. Pick a hosted pool such as 'Azure Pipelines'."
}

$targetQueue = Get-ProjectQueueForPool -PoolId $toPool.id -PoolName $ToPoolName
Write-Host ""
Write-Host "Target queue : $($targetQueue.name) (id=$($targetQueue.id), poolId=$($toPool.id))"

$buildChanges = 0
$releaseChanges = 0

Write-Section "Build definitions"
$definitionsUri = "$baseUrl/$projectSegment/_apis/build/definitions?api-version=$apiVersion&`$top=100"
$definitionsResponse = Invoke-AdoApi -Uri $definitionsUri
$definitions = @($definitionsResponse.value | Sort-Object name)

if ($definitions.Count -eq 0) {
    Write-Host "No build definitions found."
}
else {
    foreach ($summary in $definitions) {
        $definitionUri = "$baseUrl/$projectSegment/_apis/build/definitions/$($summary.id)?api-version=$apiVersion"
        $definition = Invoke-AdoApi -Uri $definitionUri

        if (-not (Test-UsesPool -QueueObject $definition.queue -PoolId $fromPool.id -PoolName $FromPoolName)) {
            continue
        }

        $buildChanges++
        $currentQueue = if ($definition.queue) { $definition.queue.name } else { "(unknown)" }
        Write-Host "  [$($definition.name)] id=$($definition.id) queue=$currentQueue -> $($targetQueue.name)"

        if ($PSCmdlet.ShouldProcess($definition.name, "Switch build queue to $($targetQueue.name)")) {
            $definition.queue = @{
                id   = $targetQueue.id
                name = $targetQueue.name
                pool = @{
                    id   = $toPool.id
                    name = $ToPoolName
                }
            }

            Invoke-AdoApi -Uri $definitionUri -Method Put -Body $definition | Out-Null
            Write-Host "    Updated." -ForegroundColor Green
        }
    }

    if ($buildChanges -eq 0) {
        Write-Host "No build definitions use pool '$FromPoolName'."
    }
}

Write-Section "Release definitions"
$releaseDefinitionsUri = "$releaseBaseUrl/$projectSegment/_apis/release/definitions?api-version=$apiVersion&`$top=100"
$releaseResponse = Invoke-AdoApi -Uri $releaseDefinitionsUri
$releaseDefinitions = @($releaseResponse.value | Sort-Object name)

if ($releaseDefinitions.Count -eq 0) {
    Write-Host "No release definitions found."
}
else {
    foreach ($summary in $releaseDefinitions) {
        $releaseUri = "$releaseBaseUrl/$projectSegment/_apis/release/definitions/$($summary.id)?api-version=$apiVersion"
        $release = Invoke-AdoApi -Uri $releaseUri
        $environmentsToUpdate = @()

        foreach ($environment in @($release.environments)) {
            foreach ($phase in @($environment.deployPhases)) {
                $deploymentInput = $phase.deploymentInput
                if ($null -eq $deploymentInput) { continue }

                $usesDefault = $false
                if ($deploymentInput.PSObject.Properties.Match("queueId").Count -gt 0) {
                    $fromQueuesUri = "$baseUrl/$projectSegment/_apis/distributedtask/queues?poolId=$($fromPool.id)&api-version=$apiVersion"
                    $fromQueues = @((Invoke-AdoApi -Uri $fromQueuesUri).value | ForEach-Object { $_.id })
                    if ($fromQueues -contains $deploymentInput.queueId) {
                        $usesDefault = $true
                    }
                }

                if ($usesDefault) {
                    $environmentsToUpdate += $environment.name
                    $deploymentInput.queueId = $targetQueue.id
                }
            }
        }

        if ($environmentsToUpdate.Count -eq 0) {
            continue
        }

        $releaseChanges++
        $envList = ($environmentsToUpdate | Select-Object -Unique) -join ", "
        Write-Host "  [$($release.name)] id=$($release.id) environments=$envList -> $($targetQueue.name)"

        if ($PSCmdlet.ShouldProcess($release.name, "Switch release queue to $($targetQueue.name)")) {
            Invoke-AdoApi -Uri $releaseUri -Method Put -Body $release | Out-Null
            Write-Host "    Updated." -ForegroundColor Green
        }
    }

    if ($releaseChanges -eq 0) {
        Write-Host "No release environments use pool '$FromPoolName'."
    }
}

Write-Section "Summary"
Write-Host "Build definitions to update : $buildChanges"
Write-Host "Release definitions to update: $releaseChanges"

if ($WhatIfPreference) {
    Write-Host ""
    Write-Host "Dry run only. Re-run without -WhatIf to apply changes." -ForegroundColor Yellow
}
else {
    Write-Host ""
    Write-Host "Done. Cancel queued jobs on '$FromPoolName' and re-run pipelines, or wait for new runs to use hosted agents." -ForegroundColor Green
    Write-Host "Build queue: $baseUrl/$projectSegment/_build"
}
