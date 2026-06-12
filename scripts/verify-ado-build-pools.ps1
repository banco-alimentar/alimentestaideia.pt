# Verify Azure DevOps build/release agent pool assignments.
#
# Usage:
#   $env:AZDO_PAT = "<pat>"
#   .\scripts\verify-ado-build-pools.ps1

[CmdletBinding()]
param(
    [string]$Organization = "BancoAlimentar",
    [string]$Project = "Alimentestaideia.pt",
    [string]$Pat = $env:AZDO_PAT
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($Pat)) {
    $Pat = $env:AZURE_DEVOPS_EXT_PAT
}

if ([string]::IsNullOrWhiteSpace($Pat)) {
    throw "Set AZDO_PAT or AZURE_DEVOPS_EXT_PAT."
}

$apiVersion = "7.1"
$projectSegment = [uri]::EscapeDataString($Project)
$baseUrl = "https://dev.azure.com/$Organization"
$releaseBaseUrl = "https://vsrm.dev.azure.com/$Organization"
$authHeader = "Basic " + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$Pat"))

function Invoke-AdoGet {
    param([Parameter(Mandatory = $true)][string]$Uri)
    return Invoke-RestMethod -Uri $Uri -Headers @{ Authorization = $authHeader } -Method Get
}

function Get-QueueSummary {
    param($Queue)

    if ($null -eq $Queue) { return "(no queue)" }

    $queueName = $Queue.name
    $queueId = $Queue.id
    $poolName = if ($Queue.pool) { $Queue.pool.name } else { "?" }
    $poolId = if ($Queue.pool) { $Queue.pool.id } else { "?" }
    $hosted = if ($Queue.pool -and $Queue.pool.PSObject.Properties.Match("isHosted").Count -gt 0) {
        if ($Queue.pool.isHosted) { "hosted" } else { "self-hosted" }
    }
    elseif ($poolId -eq 9 -or $poolName -eq "Azure Pipelines") { "hosted (inferred)" }
    elseif ($poolId -eq 1 -or $poolName -eq "Default") { "self-hosted (inferred)" }
    else { "unknown" }

    return "queue '$queueName' id=$queueId | pool '$poolName' id=$poolId | $hosted"
}

Write-Host ""
Write-Host "Build definitions in $Project" -ForegroundColor Cyan
$definitions = @(Invoke-AdoGet -Uri "$baseUrl/$projectSegment/_apis/build/definitions?api-version=$apiVersion&`$top=100").value | Sort-Object name

foreach ($summary in $definitions) {
    $definition = Invoke-AdoGet -Uri "$baseUrl/$projectSegment/_apis/build/definitions/$($summary.id)?api-version=$apiVersion"
    Write-Host "  [$($definition.name)] id=$($definition.id)"
    Write-Host "    $(Get-QueueSummary $definition.queue)"
}

Write-Host ""
Write-Host "Release definitions (first deploy phase queue per environment)" -ForegroundColor Cyan
$releases = @(Invoke-AdoGet -Uri "$releaseBaseUrl/$projectSegment/_apis/release/definitions?api-version=$apiVersion&`$top=100").value | Sort-Object name

foreach ($summary in $releases) {
    $release = Invoke-AdoGet -Uri "$releaseBaseUrl/$projectSegment/_apis/release/definitions/$($summary.id)?api-version=$apiVersion"
    Write-Host "  [$($release.name)] id=$($release.id)"
    foreach ($environment in @($release.environments)) {
        $phase = @($environment.deployPhases | Select-Object -First 1)
        if ($phase.Count -eq 0) { continue }
        $queueId = if ($phase[0].deploymentInput) { $phase[0].deploymentInput.queueId } else { $null }
        Write-Host "    $($environment.name): queueId=$queueId"
    }
}

Write-Host ""
Write-Host "Project queues (name vs pool - both can be called 'Default'):" -ForegroundColor Cyan
$queues = @(Invoke-AdoGet -Uri "$baseUrl/$projectSegment/_apis/distributedtask/queues?api-version=$apiVersion").value | Sort-Object name, id
foreach ($queue in $queues) {
    Write-Host "  $(Get-QueueSummary $queue)"
}

Write-Host ""
Write-Host "IMPORTANT: queue id=10 is self-hosted Default (pool id=1)." -ForegroundColor Yellow
Write-Host "           queue id=18 is Microsoft-hosted Azure Pipelines (pool id=9)." -ForegroundColor Yellow
Write-Host "If builds still queue on pool id=1, cancel them and queue a NEW build (Re-run keeps the old pool)." -ForegroundColor Yellow
