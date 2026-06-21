# Reports DonationItems rows with no parent donation (GitHub issue #788).
# Usage: .\scripts\diagnose-orphan-donation-items.ps1 [-ConnectionString "..."]

param(
    [string]$ConnectionString
)

$ErrorActionPreference = "Stop"

if (-not $ConnectionString) {
    $ConnectionString = $env:ConnectionStrings__DefaultConnection
}

if (-not $ConnectionString) {
    throw "Provide -ConnectionString or set ConnectionStrings__DefaultConnection."
}

$query = @"
SELECT
    ProductCatalogueId,
    COUNT(*) AS OrphanCount,
    SUM(CAST(Quantity AS bigint)) AS TotalQuantity
FROM DonationItems
WHERE DonationId IS NULL
GROUP BY ProductCatalogueId
ORDER BY OrphanCount DESC;
"@

Add-Type -AssemblyName System.Data
$connection = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
$command = $connection.CreateCommand()
$command.CommandText = $query
$adapter = New-Object System.Data.SqlClient.SqlDataAdapter $command
$table = New-Object System.Data.DataTable

try {
    $connection.Open()
    [void]$adapter.Fill($table)
}
finally {
    if ($connection.State -eq "Open") {
        $connection.Close()
    }
}

if ($table.Rows.Count -eq 0) {
    Write-Host "No orphan DonationItems found."
    exit 0
}

Write-Host "Orphan DonationItems by ProductCatalogueId:"
$table | Format-Table -AutoSize
exit 1
