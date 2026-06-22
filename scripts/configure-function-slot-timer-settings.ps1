# Disable Azure Functions timer triggers on a non-production deployment slot.
# Settings are marked slot-sticky so they are not swapped to production.
#
# Usage (local, after az login):
#   .\scripts\configure-function-slot-timer-settings.ps1 -SlotName preprod
#
# Usage (Azure DevOps, AzureCLI@2 with service connection):
#   .\scripts\configure-function-slot-timer-settings.ps1 `
#     -FunctionAppName AlimentaEstaIdeia-tools `
#     -ResourceGroupName AlimenteEstaIdeia `
#     -SlotName preprod
#
# Related:
#   Documentation/Azure-Functions.md
#   azure-pipelines/preprod-release.yml
#   azure-pipelines/developer-debug.yml

[CmdletBinding()]
param(
    [string]$FunctionAppName = "AlimentaEstaIdeia-tools",
    [string]$ResourceGroupName = "AlimenteEstaIdeia",
    [Parameter(Mandatory = $true)]
    [string]$SlotName,
    [switch]$WhatIf
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$TimerFunctions = @(
    "GenerateDonationReportFunction",
    "GenerateSiteHealthReportFunction",
    "DeleteOldSubscriptionFunction",
    "MultiBancoPaymentNotificationFunction",
    "UpdateSubscriptions"
)

$normalizedSlot = $SlotName.Trim()
if ([string]::IsNullOrWhiteSpace($normalizedSlot)) {
    throw "SlotName is required."
}

if ($normalizedSlot -eq "production" -or $normalizedSlot -eq "Production") {
    throw "Refusing to disable timers on the production slot. Use a non-production slot name (e.g. preprod, developer)."
}

$slotSettings = @()
foreach ($functionName in $TimerFunctions) {
    $slotSettings += "AzureWebJobs.$functionName.Disabled=true"
}

Write-Host "Function app: $FunctionAppName"
Write-Host "Resource group: $ResourceGroupName"
Write-Host "Slot: $normalizedSlot"
Write-Host "Slot-sticky settings to apply:"
foreach ($setting in $slotSettings) {
    Write-Host "  $setting"
}

if ($WhatIf) {
    Write-Host "WhatIf: no changes applied." -ForegroundColor Yellow
    return
}

$azVersion = & az version 2>&1
if ($LASTEXITCODE -ne 0) {
    throw "Azure CLI (az) is required. Install from https://learn.microsoft.com/cli/azure/install-azure-cli"
}

Write-Host ""
Write-Host "Applying slot settings..." -ForegroundColor Cyan

& az functionapp config appsettings set `
    --name $FunctionAppName `
    --resource-group $ResourceGroupName `
    --slot $normalizedSlot `
    --slot-settings $slotSettings `
    --output none

if ($LASTEXITCODE -ne 0) {
    throw "az functionapp config appsettings set failed with exit code $LASTEXITCODE"
}

Write-Host "Timer functions disabled on slot '$normalizedSlot'." -ForegroundColor Green
