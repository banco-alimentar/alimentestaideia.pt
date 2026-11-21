#requires -Version 7.0
[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Plan', 'DryRun', 'Apply')]
    [string] $Mode,

    [Parameter(Mandatory)]
    [string] $Database,

    [Parameter(Mandatory)]
    [string] $PlanPath,

    [string] $ServerInstance = '.',

    [ValidateRange(1, 3600)]
    [int] $QueryTimeoutSeconds = 120
)

$ErrorActionPreference = 'Stop'
if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    throw 'sqlcmd is required. This command uses Windows authentication only.'
}

$scriptPath = Join-Path $PSScriptRoot '../BancoAlimentar.AlimentaEstaIdeia.Web/Migrations/Scripts/MergeDuplicateEasyPayPaymentIds.sql'
$sql = [System.IO.File]::ReadAllText($scriptPath)
$fullPlanPath = [System.IO.Path]::GetFullPath($PlanPath)
if ([System.IO.Path]::GetExtension($fullPlanPath) -ne '.json') {
    throw 'PlanPath must be a JSON file.'
}

$modeDeclaration = "DECLARE @Mode nvarchar(10) = N'Plan';"
$dryRunDeclaration = 'DECLARE @DryRun bit = 1;'
$planDeclaration = 'DECLARE @ApprovedPlan nvarchar(max) = NULL;'
foreach ($declaration in @($modeDeclaration, $dryRunDeclaration, $planDeclaration)) {
    if ([regex]::Matches($sql, [regex]::Escape($declaration)).Count -ne 1) {
        throw 'The SQL script configuration has changed. Refusing to substitute execution settings.'
    }
}

if ($Mode -eq 'Plan') {
    if (Test-Path -LiteralPath $fullPlanPath) {
        throw 'The plan file already exists. Choose a new path; existing approvals are never overwritten.'
    }
    if (-not (Test-Path -LiteralPath (Split-Path $fullPlanPath) -PathType Container)) {
        throw 'The plan output directory must already exist.'
    }
    if ($WhatIfPreference) {
        Write-Output "Would generate a read-only plan from $ServerInstance/$Database at $fullPlanPath."
        return
    }
}
else {
    $approvedJson = [System.IO.File]::ReadAllText($fullPlanPath)
    $document = [System.Text.Json.JsonDocument]::Parse($approvedJson)
    $document.Dispose()
    $sql = $sql.Replace($modeDeclaration, "DECLARE @Mode nvarchar(10) = N'Reconcile';")
    $sql = $sql.Replace($planDeclaration, "DECLARE @ApprovedPlan nvarchar(max) = N'$($approvedJson.Replace("'", "''"))';")
    if ($Mode -eq 'Apply') {
        if (-not $PSCmdlet.ShouldProcess("$ServerInstance/$Database", 'Commit reviewed EasyPay actions, including any explicit DeleteGroup deletions')) {
            return
        }
        $sql = $sql.Replace($dryRunDeclaration, 'DECLARE @DryRun bit = 0;')
    }
    elseif ($WhatIfPreference) {
        Write-Output "Would validate and dry-run the approved plan on $ServerInstance/$Database."
        return
    }
}

$settings = @'
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
SET QUOTED_IDENTIFIER ON;
'@
$output = ($settings + "`n" + $sql) | & sqlcmd -S $ServerInstance -E -b -I -x -l 15 -t $QueryTimeoutSeconds -d $Database -y 0 -w 65535
if ($LASTEXITCODE -ne 0) {
    throw "SQL reconciliation failed. No success is implied by any returned rows.`n$($output -join "`n")"
}

if ($Mode -eq 'Plan') {
    $document = [System.Text.Json.JsonDocument]::Parse(($output -join ''))
    try {
        $options = [System.Text.Json.JsonSerializerOptions]::new()
        $options.WriteIndented = $true
        $formatted = [System.Text.Json.JsonSerializer]::Serialize([object] $document.RootElement, $options)
        $stream = [System.IO.File]::Open($fullPlanPath, [System.IO.FileMode]::CreateNew)
        try {
            $writer = [System.IO.StreamWriter]::new($stream, [System.Text.UTF8Encoding]::new($false))
            try { $writer.WriteLine($formatted) }
            finally { $writer.Dispose() }
        }
        finally { $stream.Dispose() }
    }
    finally { $document.Dispose() }
    Write-Output "Plan written to $fullPlanPath. Suggestions are not approvals; review every group and fill all approval fields."
}
else {
    $output
}