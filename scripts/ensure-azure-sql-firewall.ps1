# Allow the current machine's public IP to connect to Azure SQL Server.
#
# Usage:
#   az login
#   .\scripts\ensure-azure-sql-firewall.ps1
#   .\scripts\ensure-azure-sql-firewall.ps1 -IpAddress 188.92.252.91
#   .\scripts\ensure-azure-sql-firewall.ps1 -ServerName bancoalimentar -ResourceGroup BancoAlimentarSQL
#
# Requires: Azure CLI (az), network access to resolve public IP.

[CmdletBinding()]
param(
    [string]$SubscriptionId,
    [string]$ServerName = "bancoalimentar",
    [string]$ResourceGroup = "BancoAlimentarSQL",
    [string]$IpAddress = "",
    [string]$RuleName = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-PublicIpAddress {
    $endpoints = @(
        "https://ifconfig.me/ip",
        "https://api.ipify.org"
    )

    foreach ($endpoint in $endpoints) {
        try {
            $response = Invoke-RestMethod -Uri $endpoint -TimeoutSec 15
            $candidate = ($response | Out-String).Trim()
            if ($candidate -match '^\d{1,3}(\.\d{1,3}){3}$') {
                return $candidate
            }
        }
        catch {
            Write-Verbose "Failed to resolve public IP from $endpoint : $($_.Exception.Message)"
        }
    }

    throw "Could not determine the current public IP address."
}

function Get-DefaultRuleName {
    param([string]$Ip)
    $user = $env:USERNAME
    if (-not [string]::IsNullOrWhiteSpace($user)) {
        $sanitizedUser = ($user -replace '[^A-Za-z0-9]', '-').Trim('-')
        if ($sanitizedUser.Length -gt 40) {
            $sanitizedUser = $sanitizedUser.Substring(0, 40)
        }

        if (-not [string]::IsNullOrWhiteSpace($sanitizedUser)) {
            return "dev-$sanitizedUser"
        }
    }

    return ("client-ip-{0}" -f ($Ip -replace '\.', '-'))
}

if (-not [string]::IsNullOrWhiteSpace($SubscriptionId)) {
    az account set --subscription $SubscriptionId | Out-Null
}

if ([string]::IsNullOrWhiteSpace($IpAddress)) {
    Write-Host "Resolving public IP address..." -ForegroundColor Cyan
    $IpAddress = Get-PublicIpAddress
}

if ([string]::IsNullOrWhiteSpace($RuleName)) {
    $RuleName = Get-DefaultRuleName -Ip $IpAddress
}

Write-Host "Target server : $ServerName ($ResourceGroup)" -ForegroundColor Cyan
Write-Host "Firewall rule : $RuleName" -ForegroundColor Cyan
Write-Host "IP address    : $IpAddress" -ForegroundColor Cyan

$existingRules = az sql server firewall-rule list `
    --resource-group $ResourceGroup `
    --server $ServerName `
    -o json | ConvertFrom-Json

$existingRule = $existingRules | Where-Object { $_.name -eq $RuleName } | Select-Object -First 1

if ($null -ne $existingRule) {
    if ($existingRule.startIpAddress -eq $IpAddress -and $existingRule.endIpAddress -eq $IpAddress) {
        Write-Host "Firewall rule already allows $IpAddress." -ForegroundColor Green
    }
    else {
        Write-Host "Updating firewall rule $RuleName..." -ForegroundColor Yellow
        az sql server firewall-rule update `
            --resource-group $ResourceGroup `
            --server $ServerName `
            --name $RuleName `
            --start-ip-address $IpAddress `
            --end-ip-address $IpAddress | Out-Null
        Write-Host "Updated firewall rule $RuleName for $IpAddress." -ForegroundColor Green
    }
}
else {
    Write-Host "Creating firewall rule $RuleName..." -ForegroundColor Yellow
    az sql server firewall-rule create `
        --resource-group $ResourceGroup `
        --server $ServerName `
        --name $RuleName `
        --start-ip-address $IpAddress `
        --end-ip-address $IpAddress | Out-Null
    Write-Host "Created firewall rule $RuleName for $IpAddress." -ForegroundColor Green
}

Write-Host ""
Write-Host "Azure SQL firewall changes can take up to 5 minutes to apply. Retry your command if connection still fails." -ForegroundColor Yellow
