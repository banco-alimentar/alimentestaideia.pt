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

function Get-ProjectQueues {
    $queuesUri = "$baseUrl/$projectSegment/_apis/distributedtask/queues?api-version=$apiVersion"
    return @((Invoke-AdoApi -Uri $queuesUri).value)
}

function Get-ProjectQueueForPool {
    param(
        [int]$PoolId,
        [string]$PoolName
    )

    $matching = @(Get-ProjectQueues | Where-Object {
            $_.pool -and $_.pool.id -eq $PoolId
        })

    if ($matching.Count -eq 0) {
        throw "No queue found for pool '$PoolName' (id=$PoolId) in project '$Project'."
    }

    $preferred = @($matching | Where-Object { $_.name -eq $PoolName } | Select-Object -First 1)
    if ($preferred.Count -gt 0) {
        return $preferred[0]
    }

    return ($matching | Sort-Object id | Select-Object -First 1)
}

function Get-QueueIdsForPool {
    param([int]$PoolId)

    return @(Get-ProjectQueues | Where-Object { $_.pool -and $_.pool.id -eq $PoolId } | ForEach-Object { $_.id })
}

function Format-QueueLabel {
    param($Queue)

    if ($null -eq $Queue) { return "(unknown queue)" }

    $queueName = if ($Queue.PSObject.Properties.Match("name").Count -gt 0) { $Queue.name } else { "?" }
    $queueId = if ($Queue.PSObject.Properties.Match("id").Count -gt 0) { $Queue.id } else { "?" }
    $poolName = if ($Queue.pool) { $Queue.pool.name } else { "?" }
    $poolId = if ($Queue.pool) { $Queue.pool.id } else { "?" }
    $hosted = if ($Queue.pool -and $Queue.pool.PSObject.Properties.Match("isHosted").Count -gt 0) {
        if ($Queue.pool.isHosted) { "hosted" } else { "self-hosted" }
    }
    elseif ($poolId -eq 9) { "hosted" }
    elseif ($poolId -eq 1) { "self-hosted" }
    else { "unknown" }

    return "$queueName (queueId=$queueId, pool=$poolName id=$poolId, $hosted)"
}

function Test-UsesFromPool {
    param(
        $QueueObject,
        [int]$FromPoolId,
        [int[]]$FromQueueIds
    )

    if ($null -eq $QueueObject) { return $false }

    if ($QueueObject.PSObject.Properties.Match("id").Count -gt 0 -and $QueueObject.id -in $FromQueueIds) {
        return $true
    }

    if ($QueueObject.PSObject.Properties.Match("pool").Count -gt 0 -and $null -ne $QueueObject.pool) {
        return $QueueObject.pool.id -eq $FromPoolId
    }

    return $false
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
$fromQueueIds = Get-QueueIdsForPool -PoolId $fromPool.id
Write-Host ""
Write-Host ("Target queue : {0}" -f (Format-QueueLabel -Queue $targetQueue))
Write-Host ("From pool queue ids (self-hosted): {0}" -f (($fromQueueIds | Sort-Object) -join ", "))
if ($targetQueue.id -in $fromQueueIds) {
    throw "Target queue id=$($targetQueue.id) still belongs to self-hosted pool id=$($fromPool.id). Aborting."
}

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

        if (-not (Test-UsesFromPool -QueueObject $definition.queue -FromPoolId $fromPool.id -FromQueueIds $fromQueueIds)) {
            continue
        }

        $buildChanges++
        Write-Host "  [$($definition.name)] id=$($definition.id)"
        Write-Host "    $(Format-QueueLabel -Queue $definition.queue)"
        Write-Host "    -> $(Format-QueueLabel -Queue $targetQueue)"

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

            $verified = Invoke-AdoApi -Uri $definitionUri
            $verifiedPoolId = if ($verified.queue -and $verified.queue.pool) { $verified.queue.pool.id } else { $null }
            if ($verifiedPoolId -eq $toPool.id) {
                Write-Host "    Updated and verified (pool id=$verifiedPoolId)." -ForegroundColor Green
            }
            else {
                Write-Host "    WARNING: PUT succeeded but definition still shows pool id=$verifiedPoolId (expected $($toPool.id))." -ForegroundColor Red
                Write-Host "    Change the agent pool manually in Azure DevOps: Edit pipeline -> Agent pool -> Azure Pipelines." -ForegroundColor Red
            }
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

                $usesFromPool = $false
                if ($deploymentInput.PSObject.Properties.Match("queueId").Count -gt 0) {
                    if ($deploymentInput.queueId -in $fromQueueIds) {
                        $usesFromPool = $true
                    }
                }

                if ($usesFromPool) {
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
        $toLabel = Format-QueueLabel -Queue $targetQueue
        Write-Host "  [$($release.name)] id=$($release.id) environments=$envList"
        Write-Host "    -> $toLabel"

        if ($PSCmdlet.ShouldProcess($release.name, "Switch release queue to $($targetQueue.name)")) {
            Invoke-AdoApi -Uri $releaseUri -Method Put -Body $release | Out-Null
            Write-Host "    Updated (queueId=$($targetQueue.id))." -ForegroundColor Green
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
    Write-Host "Done. Pipelines now target pool '$ToPoolName' (Microsoft-hosted), not self-hosted '$FromPoolName'." -ForegroundColor Green
    Write-Host "Cancel any jobs still queued on the offline self-hosted agent, then re-run builds." -ForegroundColor Green
    Write-Host "Build queue: $baseUrl/$projectSegment/_build"
}
