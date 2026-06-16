# Remove the Playwright E2E environment from classic release "[DEV] AlimentaEstaIdeia".
# The EndToEndTests project was removed; browser tests run locally only (see Documentation/TESTS.md).
#
# Usage:
#   $env:AZDO_PAT = "<pat with Release (Read, write, & execute)>"
#   .\scripts\remove-dev-release-e2e.ps1 -WhatIf
#   .\scripts\remove-dev-release-e2e.ps1

[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$Organization = "BancoAlimentar",
    [string]$Project = "Alimentestaideia.pt",
    [int]$ReleaseDefinitionId = 3,
    [string[]]$EnvironmentNames = @("E2E tests"),
    [string]$Pat = $env:AZDO_PAT
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($Pat)) {
    $Pat = $env:AZURE_DEVOPS_EXT_PAT
}

if ([string]::IsNullOrWhiteSpace($Pat)) {
    throw "Set AZDO_PAT or AZURE_DEVOPS_EXT_PAT with Release (Read, write, & execute) scope."
}

$apiVersion = "7.1"
$projectSegment = [uri]::EscapeDataString($Project)
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

function Repair-ReleaseEnvironmentRanks {
    param(
        [Parameter(Mandatory = $true)]
        [array]$Environments
    )

    $ordered = @($Environments | Sort-Object { [int]$_.rank }, { [int]$_.id })
    for ($index = 0; $index -lt $ordered.Count; $index++) {
        $ordered[$index].rank = $index + 1
    }

    return $ordered
}

function Remove-EnvironmentConditionsReferencingIds {
    param(
        [Parameter(Mandatory = $true)]
        [array]$Environments,
        [Parameter(Mandatory = $true)]
        [int[]]$RemovedEnvironmentIds
    )

    foreach ($releaseEnvironment in $Environments) {
        if ($null -eq $releaseEnvironment.conditions) {
            continue
        }

        $releaseEnvironment.conditions = @(
            $releaseEnvironment.conditions | Where-Object {
                $_.name -ne "Environment" -or [int]$_.value -notin $RemovedEnvironmentIds
            }
        )
    }
}

$releaseUri = "$releaseBaseUrl/$projectSegment/_apis/release/definitions/${ReleaseDefinitionId}?api-version=$apiVersion"
$release = Invoke-AdoApi -Uri $releaseUri

Write-Host "Release: $($release.name) (id=$($release.id), revision=$($release.revision))"

$toRemove = @($release.environments | Where-Object { $EnvironmentNames -contains $_.name })
if ($toRemove.Count -eq 0) {
    Write-Host "No matching environments ($($EnvironmentNames -join ', ')). Nothing to do." -ForegroundColor Green
    exit 0
}

foreach ($releaseEnvironment in $toRemove) {
    Write-Host "  Will remove environment: $($releaseEnvironment.name) (id=$($releaseEnvironment.id))"
}

$remaining = @($release.environments | Where-Object { $EnvironmentNames -notcontains $_.name })
$removedIds = @($toRemove | ForEach-Object { [int]$_.id })
Remove-EnvironmentConditionsReferencingIds -Environments $remaining -RemovedEnvironmentIds $removedIds
$remaining = Repair-ReleaseEnvironmentRanks -Environments $remaining

Write-Host "Environments after: $(($remaining | ForEach-Object { "$($_.name) (rank=$($_.rank))" }) -join ' -> ')"

if ($PSCmdlet.ShouldProcess($release.name, "Remove E2E environment(s)")) {
    $release.environments = $remaining
    Invoke-AdoApi -Uri $releaseUri -Method Put -Body $release | Out-Null

    $verified = Invoke-AdoApi -Uri $releaseUri
    $stillPresent = @($verified.environments | Where-Object { $EnvironmentNames -contains $_.name })
    if ($stillPresent.Count -gt 0) {
        throw "PUT succeeded but environment(s) still present. Remove manually in Azure DevOps: Pipelines -> Releases -> Edit -> delete 'E2E tests'."
    }

    Write-Host "Removed E2E environment from release definition." -ForegroundColor Green
    Write-Host "Re-run or create a new release; in-flight releases may still show the old stage." -ForegroundColor Yellow
}

if ($WhatIfPreference) {
    Write-Host "Dry run only. Re-run without -WhatIf to apply." -ForegroundColor Yellow
}
