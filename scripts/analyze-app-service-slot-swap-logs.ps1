# Analyze Azure App Service logs and configuration for slot swap failures.
#
# Usage:
#   az login
#   .\scripts\analyze-app-service-slot-swap-logs.ps1
#   .\scripts\analyze-app-service-slot-swap-logs.ps1 -HoursBack 6 -SinceUtc "2026-06-16T15:30:00Z"
#   .\scripts\analyze-app-service-slot-swap-logs.ps1 -OutputDir .\slot-swap-analysis
#
# Requires: Azure CLI (az), logged-in account with read access to the web app and App Insights.

[CmdletBinding()]
param(
    [string]$SubscriptionId,
    [string]$ResourceGroup = "AlimenteEstaIdeia",
    [string]$WebAppName = "alimentaestaideia",
    [string]$SlotName = "preprod",
    [int]$HoursBack = 24,
    [datetime]$SinceUtc,
    [string]$OutputDir = "",
    [switch]$SkipLogDownload,
    [switch]$SkipAppInsights
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Section {
    param([string]$Title)
    Write-Host ""
    Write-Host ("=" * 88) -ForegroundColor Cyan
    Write-Host $Title -ForegroundColor Cyan
    Write-Host ("=" * 88) -ForegroundColor Cyan
}

function Write-Finding {
    param(
        [ValidateSet("OK", "WARN", "ERROR", "INFO")]
        [string]$Level,
        [string]$Message
    )

    $color = switch ($Level) {
        "OK" { "Green" }
        "WARN" { "Yellow" }
        "ERROR" { "Red" }
        default { "Gray" }
    }

    Write-Host ("[{0}] {1}" -f $Level, $Message) -ForegroundColor $color
}

function Invoke-AzCli {
    param(
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [switch]$AllowFailure
    )

    $prevErrorAction = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $output = & az @Arguments 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $prevErrorAction
    }

    if ($exitCode -ne 0 -and -not $AllowFailure) {
        throw ("az {0} failed (exit {1}): {2}" -f ($Arguments -join " "), $exitCode, ($output | Out-String))
    }

    return ,@($output)
}

function ConvertTo-HashtableFromJson {
    param([string]$Json)
    if ([string]::IsNullOrWhiteSpace($Json)) { return $null }
    return ($Json | ConvertFrom-Json)
}

function Test-HttpPing {
    param(
        [string]$Url,
        [int]$TimeoutSec = 120
    )

    $result = [ordered]@{
        Url = $Url
        StatusCode = $null
        ElapsedMs = $null
        Error = $null
        Location = $null
    }

    if (Get-Command curl.exe -ErrorAction SilentlyContinue) {
        $format = "%{http_code}|%{time_total}|%{redirect_url}"
        $raw = & curl.exe -s -o NUL -w $format --max-redirs 0 --max-time $TimeoutSec $Url 2>&1
        if ($LASTEXITCODE -ne 0 -and -not $raw) {
            $result.Error = "curl exited with code $LASTEXITCODE"
            return [pscustomobject]$result
        }

        $parts = ("$raw").Split("|", 3)
        if ($parts.Count -ge 1 -and $parts[0] -match "^\d+$") {
            $result.StatusCode = [int]$parts[0]
        }

        if ($parts.Count -ge 2 -and $parts[1]) {
            $seconds = 0.0
            if ([double]::TryParse($parts[1], [ref]$seconds)) {
                $result.ElapsedMs = [math]::Round($seconds * 1000, 0)
            }
        }

        if ($parts.Count -ge 3 -and $parts[2]) {
            $result.Location = $parts[2]
        }

        return [pscustomobject]$result
    }

    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        if ($PSVersionTable.PSVersion.Major -ge 7) {
            $response = Invoke-WebRequest -Uri $Url -Method Get -MaximumRedirection 0 -TimeoutSec $TimeoutSec -SkipHttpErrorCheck
            $result.StatusCode = [int]$response.StatusCode
            if ($response.Headers.Location) {
                $result.Location = [string]$response.Headers.Location
            }
        }
        else {
            try {
                $response = Invoke-WebRequest -Uri $Url -Method Get -MaximumRedirection 0 -TimeoutSec $TimeoutSec -UseBasicParsing
                $result.StatusCode = [int]$response.StatusCode
                if ($response.Headers["Location"]) {
                    $result.Location = [string]$response.Headers["Location"]
                }
            }
            catch [System.Net.WebException] {
                $webResponse = $_.Exception.Response
                if ($webResponse) {
                    $result.StatusCode = [int]$webResponse.StatusCode
                    $locationHeader = $webResponse.Headers["Location"]
                    if ($locationHeader) {
                        $result.Location = [string]$locationHeader
                    }
                }
                else {
                    $result.Error = $_.Exception.Message
                }
            }
        }
    }
    catch {
        $result.Error = $_.Exception.Message
    }
    finally {
        $sw.Stop()
        if (-not $result.ElapsedMs) {
            $result.ElapsedMs = [math]::Round($sw.Elapsed.TotalMilliseconds, 0)
        }
    }

    return [pscustomobject]$result
}

function Get-DeploymentStatusLabel {
    param([object]$Status)
    switch ([string]$Status) {
        "0" { return "Pending" }
        "1" { return "Building" }
        "2" { return "Deploying" }
        "3" { return "Failed" }
        "4" { return "Success" }
        default { return [string]$Status }
    }
}

function Get-SwapRelevantAppSettings {
    param($Settings)

    $keys = @(
        "WEBSITE_SWAP_WARMUP_PING_PATH",
        "WEBSITE_SWAP_WARMUP_PING_STATUSES",
        "WEBSITE_WARMUP_PATH",
        "WEBSITE_PORT",
        "WEBSITES_PORT",
        "ASPNETCORE_ENVIRONMENT",
        "APPINSIGHTS_INSTRUMENTATIONKEY",
        "APPLICATIONINSIGHTS_CONNECTION_STRING",
        "WEBSITE_HEALTHCHECK_MAXPINGFAILURES",
        "WEBSITE_HEALTHCHECK_MAXUNHEALTHYWORKERPERCENT",
        "WEBSITE_ADD_SITENAME_BINDINGS_IN_APPHOST_CONFIG"
    )

    $found = @{}
    foreach ($key in $keys) {
        $value = $null
        if ($Settings -is [System.Collections.IDictionary]) {
            if ($Settings.Contains($key)) { $value = $Settings[$key] }
        }
        elseif ($Settings.PSObject.Properties.Name -contains $key) {
            $value = $Settings.$key
        }

        if ($null -ne $value -and "$value".Length -gt 0) {
            $found[$key] = $value
        }
    }

    return $found
}

function Search-LogFiles {
    param(
        [string]$Root,
        [datetime]$Since,
        [string[]]$Patterns
    )

    $logHits = New-Object System.Collections.Generic.List[object]
    if (-not (Test-Path $Root)) { return $logHits }

    $files = Get-ChildItem -Path $Root -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object {
            $_.Extension -in ".log", ".txt", ".xml", ".json", ".htm", ".html" -or
            $_.Name -match "eventlog|application|stdout|stderr|http|deploy|docker"
        }

    foreach ($file in $files) {
        if ($file.LastWriteTimeUtc -lt $Since.AddHours(-1)) {
            continue
        }

        try {
            $lineNumber = 0
            foreach ($line in [System.IO.File]::ReadLines($file.FullName)) {
                $lineNumber++
                foreach ($pattern in $Patterns) {
                    if ($line -match $pattern) {
                        $logHits.Add([pscustomobject]@{
                            File = $file.FullName.Substring($Root.Length).TrimStart('\', '/')
                            Line = $lineNumber
                            Pattern = $pattern
                            Text = if ($line.Length -gt 400) { $line.Substring(0, 400) + "..." } else { $line }
                            FileTimeUtc = $file.LastWriteTimeUtc
                        })
                        break
                    }
                }
            }
        }
        catch {
            Write-Finding "WARN" ("Could not read {0}: {1}" -f $file.FullName, $_.Exception.Message)
        }
    }

    return $logHits
}

function Get-AppInsightsComponentName {
    param(
        [string]$ResourceGroupName,
        [string]$AppName,
        [string]$Slot
    )

    $slotArgs = @("webapp", "config", "appsettings", "list", "-g", $ResourceGroupName, "-n", $AppName, "--slot", $Slot, "-o", "json")
    $settingsJson = Invoke-AzCli -Arguments $slotArgs
    $settings = ConvertTo-HashtableFromJson $settingsJson
    if (-not $settings) { return $null }

    $connectionString = $settings["APPLICATIONINSIGHTS_CONNECTION_STRING"]
    if ($connectionString -match "InstrumentationKey=([^;]+)") {
        return @{ InstrumentationKey = $Matches[1] }
    }

    if ($settings["APPINSIGHTS_INSTRUMENTATIONKEY"]) {
        return @{ InstrumentationKey = $settings["APPINSIGHTS_INSTRUMENTATIONKEY"] }
    }

    return $null
}

function Invoke-AppInsightsQueries {
    param(
        [string]$AppInsightsName,
        [string]$ResourceGroupName,
        [datetime]$Since
    )

    $sinceIso = $Since.ToUniversalTime().ToString("o")
    $queries = @{
        Exceptions = @"
exceptions
| where timestamp >= datetime($sinceIso)
| project timestamp, type, outerMessage, problemId, operation_Name, cloud_RoleName, cloud_RoleInstance, customDimensions
| order by timestamp desc
| take 50
"@
        FailedRequests = @"
requests
| where timestamp >= datetime($sinceIso)
| where success == false or resultCode !in ('200', '201', '202', '204', '301', '302', '304', '307')
| project timestamp, name, url, resultCode, duration, success, operation_Name, cloud_RoleInstance
| order by timestamp desc
| take 100
"@
        SwapWarmupCandidates = @"
requests
| where timestamp >= datetime($sinceIso)
| where url has '/status' or url has 'warmup' or url == '/' or name has 'warmup'
| project timestamp, name, url, resultCode, duration, cloud_RoleInstance
| order by timestamp desc
| take 100
"@
        TracesErrors = @"
traces
| where timestamp >= datetime($sinceIso)
| where severityLevel >= 3
| project timestamp, message, severityLevel, operation_Name, cloud_RoleInstance
| order by timestamp desc
| take 100
"@
        StartupEvents = @"
traces
| where timestamp >= datetime($sinceIso)
| where message has_any ('Application started', 'Application is shutting down', 'Hosting started', 'Now listening', 'KeyVault', 'Migration', '503', 'swap', 'warmup')
| project timestamp, message, severityLevel, cloud_RoleInstance
| order by timestamp desc
| take 100
"@
    }

    $results = @{}
    foreach ($entry in $queries.GetEnumerator()) {
        $queryArgs = @(
            "monitor", "app-insights", "query",
            "--app", $AppInsightsName,
            "--resource-group", $ResourceGroupName,
            "--analytics-query", $entry.Value,
            "-o", "json"
        )
        $raw = Invoke-AzCli -Arguments $queryArgs -AllowFailure
        if ($LASTEXITCODE -ne 0) {
            $results[$entry.Key] = @{ Error = ($raw | Out-String) }
            continue
        }

        $parsed = ConvertTo-HashtableFromJson ($raw | Out-String)
        $results[$entry.Key] = $parsed
    }

    return $results
}

function Format-AppInsightsTable {
    param($QueryResult)

    if ($null -eq $QueryResult) { return @() }
    if ($QueryResult -is [hashtable] -and $QueryResult.Error) {
        return @([pscustomobject]@{ Error = ($QueryResult.Error | Out-String).Trim() })
    }

    if (-not $QueryResult.tables) { return @() }

    $table = $QueryResult.tables[0]
    if (-not $table.rows -or $table.rows.Count -eq 0) { return @() }

    $columns = $table.columns | ForEach-Object { $_.name }
    $rows = foreach ($row in $table.rows) {
        $item = [ordered]@{}
        for ($i = 0; $i -lt $columns.Count; $i++) {
            $item[$columns[$i]] = $row[$i]
        }
        [pscustomobject]$item
    }

    return $rows
}

# --- Main ---

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "Azure CLI (az) is not installed or not on PATH."
}

$account = Invoke-AzCli -Arguments @("account", "show", "-o", "json")
$accountObj = ConvertTo-HashtableFromJson ($account | Out-String)
if (-not $accountObj) {
    throw "Not logged in to Azure. Run 'az login' first."
}

if ($SubscriptionId) {
    Invoke-AzCli -Arguments @("account", "set", "--subscription", $SubscriptionId) | Out-Null
}

if (-not $SinceUtc) {
    $SinceUtc = (Get-Date).ToUniversalTime().AddHours(-1 * $HoursBack)
}

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $OutputDir = Join-Path (Get-Location) ("slot-swap-analysis-{0}" -f $stamp)
}

New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
$reportPath = Join-Path $OutputDir "slot-swap-report.txt"
$report = New-Object System.Collections.Generic.List[string]

function Add-ReportLine {
    param([string]$Line = "")
    $report.Add($Line)
    Write-Host $Line
}

Add-ReportLine "Slot swap analysis report"
Add-ReportLine ("Generated (UTC): {0}" -f (Get-Date).ToUniversalTime().ToString("o"))
Add-ReportLine ("Window start (UTC): {0}" -f $SinceUtc.ToUniversalTime().ToString("o"))
Add-ReportLine ("Subscription: {0}" -f $accountObj.name)
Add-ReportLine ("Web app: {0} / slot: {1} / RG: {2}" -f $WebAppName, $SlotName, $ResourceGroup)
Add-ReportLine ("Output: {0}" -f $OutputDir)

Write-Section "1. Live HTTP warmup ping (what Azure uses internally)"
$baseHttp = "http://{0}-{1}.azurewebsites.net" -f $WebAppName, $SlotName
$baseHttps = "https://{0}-{1}.azurewebsites.net" -f $WebAppName, $SlotName
$pingUrls = @(
    "$baseHttp/health/swap",
    "$baseHttp/",
    "$baseHttp/status",
    "$baseHttps/health/swap",
    "$baseHttps/",
    "$baseHttps/status"
)

$pingResults = foreach ($url in $pingUrls) { Test-HttpPing -Url $url }
$pingResults | Format-Table -AutoSize | Out-String | ForEach-Object { Add-ReportLine $_ }

foreach ($ping in $pingResults) {
    if ($ping.Error) {
        Write-Finding "ERROR" ("Ping failed: {0} -> {1}" -f $ping.Url, $ping.Error)
    }
    elseif ($ping.StatusCode -ge 500) {
        Write-Finding "ERROR" ("Ping {0} returned HTTP {1} in {2} ms" -f $ping.Url, $ping.StatusCode, $ping.ElapsedMs)
    }
    elseif ($ping.StatusCode -in 301, 302, 307, 308) {
        Write-Finding "WARN" ("Ping {0} returned HTTP {1} redirect to {2} ({3} ms). Strict swap ping (200-only) will fail on HTTP." -f $ping.Url, $ping.StatusCode, $ping.Location, $ping.ElapsedMs)
    }
    elseif ($ping.ElapsedMs -gt 90000) {
        Write-Finding "WARN" ("Ping {0} took {1} ms (> Azure 90s swap ping timeout per attempt)" -f $ping.Url, $ping.ElapsedMs)
    }
    else {
        Write-Finding "OK" ("Ping {0} -> HTTP {1} ({2} ms)" -f $ping.Url, $ping.StatusCode, $ping.ElapsedMs)
    }
}

Write-Section "2. Slot swap related app settings ($SlotName)"
$settingsJson = Invoke-AzCli -Arguments @("webapp", "config", "appsettings", "list", "-g", $ResourceGroup, "-n", $WebAppName, "--slot", $SlotName, "-o", "json")
$allSettings = ConvertTo-HashtableFromJson ($settingsJson | Out-String)
$swapSettings = Get-SwapRelevantAppSettings -Settings $allSettings
$swapSettings.GetEnumerator() | Sort-Object Name | ForEach-Object {
    $line = "{0} = {1}" -f $_.Key, $_.Value
    Add-ReportLine $line
    if ($_.Key -eq "WEBSITE_SWAP_WARMUP_PING_STATUSES" -and $_.Value -match "200" -and $_.Value -notmatch "301|302|307") {
        Write-Finding "ERROR" "WEBSITE_SWAP_WARMUP_PING_STATUSES accepts only 200 but HTTP ping returns 301. This commonly causes swap failure even when HTTPS works."
    }
}

if (-not $swapSettings.ContainsKey("WEBSITE_SWAP_WARMUP_PING_PATH")) {
    Write-Finding "INFO" "WEBSITE_SWAP_WARMUP_PING_PATH not set (default ping path is / over HTTP). After deploy, set it to /health/swap on preprod + production slots."
}

Write-Section "3. Web app / slot state"
$siteJson = Invoke-AzCli -Arguments @("webapp", "show", "-g", $ResourceGroup, "-n", $WebAppName, "--slot", $SlotName, "-o", "json")
$site = ConvertTo-HashtableFromJson ($siteJson | Out-String)
Add-ReportLine ("State: {0}" -f $site.state)
Add-ReportLine ("HTTPS Only: {0}" -f $site.httpsOnly)
Add-ReportLine ("Kind: {0}" -f $site.kind)
Add-ReportLine ("Last modified: {0}" -f $site.lastModifiedTimeUtc)

$configJson = Invoke-AzCli -Arguments @("webapp", "config", "show", "-g", $ResourceGroup, "-n", $WebAppName, "--slot", $SlotName, "-o", "json")
$config = ConvertTo-HashtableFromJson ($configJson | Out-String)
Add-ReportLine ("Always On: {0}" -f $config.alwaysOn)
Add-ReportLine ("Health check path: {0}" -f $config.healthCheckPath)
Add-ReportLine ("HTTP 2.0 enabled: {0}" -f $config.http20Enabled)
Add-ReportLine ("Min TLS: {0}" -f $config.minTlsVersion)

if (-not $config.alwaysOn) {
    Write-Finding "WARN" "Always On is disabled. Cold starts during swap warmup can exceed ping timeouts."
}

if ($site.httpsOnly) {
    Write-Finding "WARN" "HTTPS Only is enabled on the slot. Azure slot swap warmup uses internal HTTP; combined with app-level redirects this often causes swap failures."
}

Write-Section "4. Recent deployments (slot)"
$deployJson = Invoke-AzCli -Arguments @("webapp", "log", "deployment", "list", "-g", $ResourceGroup, "-n", $WebAppName, "--slot", $SlotName, "-o", "json") -AllowFailure
if ($LASTEXITCODE -eq 0) {
    $deployments = ConvertTo-HashtableFromJson (($deployJson | Out-String).Trim())
    if ($deployments -and @($deployments).Count -gt 0) {
        $deploymentRows = foreach ($deployment in @($deployments)) {
            $messageObj = $null
            try { $messageObj = $deployment.message | ConvertFrom-Json } catch { }
            [pscustomobject]@{
                id = $deployment.id
                status = Get-DeploymentStatusLabel $deployment.status
                type = if ($messageObj) { $messageObj.type } else { "" }
                buildId = if ($messageObj) { $messageObj.buildId } else { "" }
                start = $deployment.start_time
                end = $deployment.end_time
                active = $deployment.active
            }
        }

        $deploymentRows | Select-Object -First 15 | Format-Table -AutoSize | Out-String | ForEach-Object { Add-ReportLine $_ }

        $failedSwaps = $deploymentRows | Where-Object { $_.type -eq "SlotSwap" -and $_.status -eq "Failed" }
        if ($failedSwaps) {
            Write-Finding "ERROR" ("Found {0} failed SlotSwap deployment(s) in recent history." -f @($failedSwaps).Count)
        }
    }
    else {
        Add-ReportLine "No deployment history returned."
    }
}
else {
    Write-Finding "WARN" "Could not list deployments: $($deployJson | Out-String)"
}

$logRoot = Join-Path $OutputDir "logs"
if (-not $SkipLogDownload) {
    Write-Section "5. Download App Service logs"
    New-Item -ItemType Directory -Path $logRoot -Force | Out-Null
    $zipPath = Join-Path $OutputDir "webapp-logs.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

    $downloadOutput = Invoke-AzCli -Arguments @(
        "webapp", "log", "download",
        "-g", $ResourceGroup,
        "-n", $WebAppName,
        "--slot", $SlotName,
        "--log-file", $zipPath
    ) -AllowFailure

    if ($LASTEXITCODE -eq 0 -and (Test-Path $zipPath)) {
        Expand-Archive -Path $zipPath -DestinationPath $logRoot -Force
        Write-Finding "OK" ("Logs extracted to {0}" -f $logRoot)
    }
    else {
        Write-Finding "WARN" ("Log download failed. Enable App Service logging (Application Logging + Failed Request Tracing) and retry. Details: {0}" -f ($downloadOutput | Out-String))
    }
}

Write-Section "6. Scan downloaded logs for swap / startup issues"
$patterns = @(
    "Cannot swap",
    "http ping",
    "warmup",
    "417",
    "ExpectationFailed",
    "503",
    "502",
    "500",
    "Application is shutting down",
    "Application started",
    "Unhandled exception",
    "fail:",
    "error",
    "KeyVault",
    "Key Vault",
    "TenantConfiguration",
    "Can't find a valid tenant",
    "Migration",
    "pending migrations",
    "SqlException",
    "timeout",
    "ANCM",
    "HTTP Error 50",
    "Process was terminated",
    "OutOfMemory"
)

if (Test-Path $logRoot) {
    $logMatches = Search-LogFiles -Root $logRoot -Since $SinceUtc -Patterns $patterns
    if ($logMatches.Count -eq 0) {
        Write-Finding "INFO" "No pattern matches in downloaded logs (logs may be empty, rotated, or logging not enabled)."
    }
    else {
        $matchReport = Join-Path $OutputDir "log-pattern-matches.csv"
        $logMatches | Sort-Object FileTimeUtc -Descending | Export-Csv -Path $matchReport -NoTypeInformation
        Write-Finding "OK" ("Found {0} matching log lines -> {1}" -f $logMatches.Count, $matchReport)

        $logMatches |
            Group-Object Pattern |
            Sort-Object Count -Descending |
            Select-Object Count, Name |
            Format-Table -AutoSize |
            Out-String |
            ForEach-Object { Add-ReportLine $_ }

        Add-ReportLine ""
        Add-ReportLine "Most recent matches:"
        $logMatches |
            Sort-Object FileTimeUtc -Descending |
            Select-Object -First 25 File, Line, Pattern, FileTimeUtc, Text |
            Format-Table -Wrap -AutoSize |
            Out-String |
            ForEach-Object { Add-ReportLine $_ }
    }
}
else {
    Write-Finding "WARN" "No log directory to scan."
}

$httpLogDir = Join-Path $logRoot "LogFiles\http\RawLogs"
if (Test-Path $httpLogDir) {
    Write-Section "6b. HTTP raw logs (IIS) status summary"
    $httpRows = New-Object System.Collections.Generic.List[object]
    Get-ChildItem $httpLogDir -Filter "*.log" -File | ForEach-Object {
        foreach ($line in [System.IO.File]::ReadLines($_.FullName)) {
            if ($line -notmatch "^\#") {
                $fields = $line -split " "
                if ($fields.Count -ge 9) {
                    $status = $fields[8]
                    if ($status -match "^\d{3}$") {
                        $httpRows.Add([pscustomobject]@{
                            File = $_.Name
                            Status = [int]$status
                            Uri = $fields[4]
                        })
                    }
                }
            }
        }
    }

    if ($httpRows.Count -gt 0) {
        $httpRows |
            Group-Object Status |
            Sort-Object Name |
            Select-Object @{ n = "Status"; e = { $_.Name } }, Count |
            Format-Table -AutoSize |
            Out-String |
            ForEach-Object { Add-ReportLine $_ }

        $badHttp = $httpRows | Where-Object { $_.Status -ge 500 -or $_.Status -eq 417 -or $_.Status -eq 503 -or $_.Status -eq 502 }
        if ($badHttp) {
            Write-Finding "ERROR" ("Found {0} HTTP log entries with 5xx/417/502/503 status codes." -f @($badHttp).Count)
            $badHttp | Select-Object -First 20 | Format-Table -AutoSize | Out-String | ForEach-Object { Add-ReportLine $_ }
            $badHttp | Export-Csv -Path (Join-Path $OutputDir "http-errors.csv") -NoTypeInformation
        }
        else {
            Write-Finding "OK" "No 5xx/417 entries in downloaded HTTP raw logs."
        }
    }
}

if (-not $SkipAppInsights) {
    Write-Section "7. Application Insights (exceptions / failed requests)"
    $aiComponents = Invoke-AzCli -Arguments @(
        "resource", "list",
        "-g", $ResourceGroup,
        "--resource-type", "Microsoft.Insights/components",
        "-o", "json"
    ) -AllowFailure
    $aiName = $null
    if ($LASTEXITCODE -eq 0) {
        $components = ConvertTo-HashtableFromJson (($aiComponents | Out-String).Trim())
        if ($components -and @($components).Count -gt 0) {
            $preferred = @($components) | Where-Object { $_.name -match "alimentaestaideia|alimentestaideia" } | Select-Object -First 1
            if (-not $preferred) { $preferred = @($components)[0] }
            $aiName = $preferred.name
            Add-ReportLine ("Using App Insights component: {0}" -f $aiName)
        }
    }
    else {
        Write-Finding "WARN" "Could not list Application Insights components. Install extension: az extension add --name application-insights"
    }

    if ($aiName) {
        $aiResults = Invoke-AppInsightsQueries -AppInsightsName $aiName -ResourceGroupName $ResourceGroup -Since $SinceUtc
        foreach ($entry in $aiResults.GetEnumerator()) {
            Add-ReportLine ""
            Add-ReportLine ("--- App Insights: {0} ---" -f $entry.Key)
            if ($entry.Value -is [hashtable] -and $entry.Value.Error) {
                Write-Finding "WARN" ($entry.Value.Error | Out-String).Trim()
                continue
            }

            $rows = @(Format-AppInsightsTable -QueryResult $entry.Value)
            if ($rows.Count -eq 0) {
                Add-ReportLine "(no rows)"
            }
            elseif ($rows[0].PSObject.Properties.Name -contains "Error") {
                Write-Finding "WARN" $rows[0].Error
            }
            else {
                $csvPath = Join-Path $OutputDir ("appinsights-{0}.csv" -f ($entry.Key.ToLower()))
                $rows | Export-Csv -Path $csvPath -NoTypeInformation
                $rows | Select-Object -First 15 | Format-Table -Wrap -AutoSize | Out-String | ForEach-Object { Add-ReportLine $_ }
                Write-Finding "OK" ("Full results: {0}" -f $csvPath)
            }
        }
    }
    else {
        Write-Finding "WARN" "No Application Insights component found in resource group $ResourceGroup."
    }
}

Write-Section "8. Summary / recommended actions"
$recommendations = @(
    "If HTTP ping returns 301/307 and WEBSITE_SWAP_WARMUP_PING_STATUSES=200, remove that setting or add 301,307, or set WEBSITE_SWAP_WARMUP_PING_PATH=/health/swap on the App Service (returns 200 over HTTP, no tenant/DB).",
    "If logs show KeyVault / tenant 503 errors during swap window, fix slot managed identity / app settings before swapping.",
    "If logs show pending migrations or SqlException on first request, run migrations before swap or add a pipeline warmup step that hits https://<slot>/status until Healthy.",
    "Enable Always On on the preprod slot to reduce cold-start ping timeouts.",
    "Add a release pipeline step after PrePROD deploy: poll https://<slot>/status until HTTP 200 before the Swap Slots task."
)

foreach ($rec in $recommendations) {
    Write-Finding "INFO" $rec
    Add-ReportLine ("RECOMMENDATION: {0}" -f $rec)
}

$report | Set-Content -Path $reportPath -Encoding UTF8
Write-Host ""
Write-Finding "OK" ("Report saved to {0}" -f $reportPath)
