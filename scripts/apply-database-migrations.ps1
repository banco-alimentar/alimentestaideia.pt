# Apply Entity Framework migrations for ApplicationDbContext (Web) and InfrastructureDbContext (Sas.Model).
#
# Usage (local, after az login and Key Vault access):
#   $cs = az keyvault secret show --vault-name alimentaestaideia-key --name ConnectionStrings--DefaultConnection --query value -o tsv
#   .\scripts\apply-database-migrations.ps1 -DefaultConnection $cs
#
# Usage (Azure DevOps, after AzureKeyVault@2):
#   The task exposes secrets as pipeline variables. Pass them explicitly or rely on env vars:
#   - CONNECTIONSTRINGS__DEFAULTCONNECTION
#   - CONNECTIONSTRINGS__INFRASTRUCTURE  (optional; secret name may be ConnectionStrings:Infrastructure)
#
# Related:
#   azure-pipelines/preprod-release.yml
#   Documentation/CI-Azure-DevOps.md

[CmdletBinding()]
param(
    [string]$DefaultConnection = "",
    [string]$InfrastructureConnection = "",
    [string]$RepositoryRoot = "",
    [string]$DotNetEfVersion = "9.0.3",
    [switch]$SkipInfrastructure,
    [switch]$WhatIf
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host $Message -ForegroundColor Cyan
}

function Resolve-ConnectionString {
    param(
        [string]$ExplicitValue,
        [string[]]$EnvironmentVariableNames
    )

    if (-not [string]::IsNullOrWhiteSpace($ExplicitValue)) {
        return $ExplicitValue.Trim()
    }

    foreach ($name in $EnvironmentVariableNames) {
        $candidate = [Environment]::GetEnvironmentVariable($name)
        if (-not [string]::IsNullOrWhiteSpace($candidate)) {
            return $candidate.Trim()
        }
    }

    return ""
}

function Get-RepositoryRoot {
    param(
        [string]$StartPath,
        [string]$ScriptRoot
    )

    if (-not [string]::IsNullOrWhiteSpace($StartPath)) {
        return (Resolve-Path $StartPath).Path
    }

    return (Resolve-Path (Join-Path $ScriptRoot "..")).Path
}

function Ensure-DotNetEf {
    param([string]$Version)

    $efCheck = & dotnet ef --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        $installed = ($efCheck | Out-String).Trim()
        Write-Host "Using $installed" -ForegroundColor Green
        return
    }

    Write-Step "Installing dotnet-ef $Version (global tool)..."
    dotnet tool install --global dotnet-ef --version $Version
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to install dotnet-ef $Version. Run: dotnet tool install --global dotnet-ef"
    }

    $efVersion = (& dotnet ef --version 2>&1 | Out-String).Trim()
    Write-Host "Using $efVersion" -ForegroundColor Green
}

function Invoke-EfDatabaseUpdate {
    param(
        [string]$DisplayName,
        [string]$ProjectPath,
        [string]$ConnectionString,
        [string]$StartupProjectPath = "",
        [Parameter(Mandatory = $true)]
        [string]$Context
    )

    if ([string]::IsNullOrWhiteSpace($StartupProjectPath)) {
        $StartupProjectPath = $ProjectPath
    }

    Write-Step "Pending migrations ($DisplayName)..."
    Write-Host "dotnet ef migrations list --context $Context" -ForegroundColor DarkGray

    & dotnet ef migrations list `
        --project $ProjectPath `
        --startup-project $StartupProjectPath `
        --context $Context `
        --connection $ConnectionString `
        --no-build
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to list migrations for $DisplayName."
    }

    if ($WhatIf) {
        Write-Host "WhatIf: skipping database update for $DisplayName." -ForegroundColor Yellow
        return
    }

    Write-Step "Applying migrations ($DisplayName)..."
    Write-Host "dotnet ef database update --context $Context" -ForegroundColor DarkGray

    & dotnet ef database update `
        --project $ProjectPath `
        --startup-project $StartupProjectPath `
        --context $Context `
        --connection $ConnectionString `
        --verbose
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to apply migrations for $DisplayName."
    }

    Write-Host "Migrations applied for $DisplayName." -ForegroundColor Green
}

$repoRoot = Get-RepositoryRoot -StartPath $RepositoryRoot -ScriptRoot $PSScriptRoot
Push-Location $repoRoot
try {
    $defaultConnection = Resolve-ConnectionString `
        -ExplicitValue $DefaultConnection `
        -EnvironmentVariableNames @(
            "CONNECTIONSTRINGS__DEFAULTCONNECTION",
            "ConnectionStrings--DefaultConnection"
        )

    if ([string]::IsNullOrWhiteSpace($defaultConnection)) {
        throw @"
DefaultConnection is required.
Provide -DefaultConnection, set CONNECTIONSTRINGS__DEFAULTCONNECTION, or run AzureKeyVault@2 before this script.
"@
    }

    $infrastructureConnection = Resolve-ConnectionString `
        -ExplicitValue $InfrastructureConnection `
        -EnvironmentVariableNames @(
            "CONNECTIONSTRINGS__INFRASTRUCTURE",
            "ConnectionStrings--Infrastructure",
            "ConnectionStrings:Infrastructure"
        )

    $webProject = Join-Path $repoRoot "BancoAlimentar.AlimentaEstaIdeia.Web\BancoAlimentar.AlimentaEstaIdeia.Web.csproj"
    $infraProject = Join-Path $repoRoot "BancoAlimentar.AlimentaEstaIdeia.Sas.Model\BancoAlimentar.AlimentaEstaIdeia.Sas.Model.csproj"

    if (-not (Test-Path $webProject)) {
        throw "Web project not found: $webProject"
    }

    Write-Step "Restoring solution (required for dotnet ef)..."
    dotnet restore BancoAlimentar.AlimentaEstaIdeia.Web.sln --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet restore failed."
    }

    Write-Step "Building Web project..."
    dotnet build $webProject --configuration Release --no-restore --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed for Web project."
    }

    Ensure-DotNetEf -Version $DotNetEfVersion

    Invoke-EfDatabaseUpdate `
        -DisplayName "ApplicationDbContext (tenant database)" `
        -ProjectPath $webProject `
        -ConnectionString $defaultConnection `
        -Context "ApplicationDbContext"

    if (-not $SkipInfrastructure -and -not [string]::IsNullOrWhiteSpace($infrastructureConnection)) {
        if (-not (Test-Path $infraProject)) {
            throw "Infrastructure project not found: $infraProject"
        }

        Write-Step "Building Sas.Model project..."
        dotnet build $infraProject --configuration Release --verbosity minimal
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet build failed for Sas.Model project."
        }

        Invoke-EfDatabaseUpdate `
            -DisplayName "InfrastructureDbContext" `
            -ProjectPath $infraProject `
            -StartupProjectPath $webProject `
            -ConnectionString $infrastructureConnection `
            -Context "InfrastructureDbContext"
    }
    elseif (-not $SkipInfrastructure) {
        Write-Host "Infrastructure connection not provided; skipping InfrastructureDbContext migrations." -ForegroundColor Yellow
    }

    Write-Host ""
    Write-Host "Database migration step completed successfully." -ForegroundColor Green
}
finally {
    Pop-Location
}
