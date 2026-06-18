# Investigate ASP.NET Core HTTP 500.30 (app failed to start) on Azure App Service.
#
# Usage:
#   az login
#   .\scripts\analyze-app-service-startup-failure.ps1
#   .\scripts\analyze-app-service-startup-failure.ps1 -SlotName preprod -HoursBack 6
#   .\scripts\analyze-app-service-startup-failure.ps1 -SiteUrl "https://alimentaestaideia-preprod.azurewebsites.net/"
#
# Requires: Azure CLI (az). Optional: curl.exe for HTTP probes.

[CmdletBinding()]
param(
    [string]$SubscriptionId,
    [string]$ResourceGroup = "AlimenteEstaIdeia",
    [string]$WebAppName = "alimentaestaideia",
    [string]$SlotName = "preprod",
    [string]$SiteUrl = "",
    [int]$HoursBack = 24,
    [datetime]$SinceUtc,
    [string]$OutputDir = "",
    [switch]$SkipLogDownload,
    [switch]$SkipAppInsights,
    [switch]$EnableStdoutLoggingHintOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ExpectedTargetFramework = "net9.0"

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

function Get-HttpResponseSummary {
    param(
        [string]$Url,
        [int]$TimeoutSec = 60
    )

    $result = [ordered]@{
        Url = $Url
        StatusCode = $null
        ElapsedMs = $null
        BodySnippet = $null
        Is50030 = $false
        Error = $null
    }

    $bodyFile = [System.IO.Path]::GetTempFileName()
    try {
        if (Get-Command curl.exe -ErrorAction SilentlyContinue) {
            $format = "%{http_code}|%{time_total}"
            $raw = & curl.exe -s -L --max-time $TimeoutSec -o $bodyFile -w $format $Url 2>&1
            $parts = ("$raw").Split("|", 2)
            if ($parts.Count -ge 1 -and $parts[0] -match "^\d+$") {
                $result.StatusCode = [int]$parts[0]
            }

            if ($parts.Count -ge 2 -and $parts[1]) {
                $seconds = 0.0
                if ([double]::TryParse($parts[1], [ref]$seconds)) {
                    $result.ElapsedMs = [math]::Round($seconds * 1000, 0)
                }
            }
        }
        else {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec $TimeoutSec
            $result.StatusCode = [int]$response.StatusCode
            Set-Content -Path $bodyFile -Value $response.Content -Encoding UTF8
        }

        if (Test-Path $bodyFile) {
            $body = Get-Content $bodyFile -Raw -ErrorAction SilentlyContinue
            if ($body) {
                $snippet = $body -replace "\s+", " "
                if ($snippet.Length -gt 500) { $snippet = $snippet.Substring(0, 500) + "..." }
                $result.BodySnippet = $snippet
                $result.Is50030 = $body -match "500\.30" -or $body -match "ASP\.NET Core app failed to start"
            }
        }
    }
    catch {
        $result.Error = $_.Exception.Message
    }
    finally {
        if (Test-Path $bodyFile) { Remove-Item $bodyFile -Force -ErrorAction SilentlyContinue }
    }

    return [pscustomobject]$result
}

function Search-LogContent {
    param(
        [string]$Root,
        [datetime]$Since,
        [string[]]$Patterns
    )

    $logHits = New-Object System.Collections.Generic.List[object]
    if (-not (Test-Path $Root)) { return $logHits }

    $files = Get-ChildItem -Path $Root -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object {
            $_.Extension -in ".log", ".txt", ".xml", ".htm", ".html", ".json" -or
            $_.Name -match "stdout|stderr|eventlog|aspnetcore|application|docker|stderr"
        }

    foreach ($file in $files) {
        if ($file.LastWriteTimeUtc -lt $Since.AddHours(-2)) {
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
                            Text = if ($line.Length -gt 600) { $line.Substring(0, 600) + "..." } else { $line }
                            FileTimeUtc = $file.LastWriteTimeUtc
                        })
                        break
                    }
                }
            }
        }
        catch {
            Write-Finding "WARN" ("Could not read {0}: {1}" -f $file.Name, $_.Exception.Message)
        }
    }

    return $logHits
}

function Get-StartupRelevantSettings {
    param($Settings)

    $keys = @(
        "ASPNETCORE_ENVIRONMENT",
        "ASPNETCORE_DETAILEDERRORS",
        "WEBSITE_SITE_NAME",
        "WEBSITE_SLOT_NAME",
        "WEBSITE_INSTANCE_ID",
        "WEBSITE_USE_PLACEHOLDER_DOTNETISOLATED",
        "FUNCTIONS_EXTENSION_VERSION",
        "DataProtectionKeyVaultKey",
        "AzureStorage:ConnectionString",
        "APPINSIGHTS_INSTRUMENTATIONKEY",
        "APPLICATIONINSIGHTS_CONNECTION_STRING"
    )

    $connectionStringKeys = @(
        "ConnectionStrings:DefaultConnection",
        "ConnectionStrings:Infrastructure",
        "DefaultConnection",
        "Infrastructure"
    )

    $found = @{}
    foreach ($key in ($keys + $connectionStringKeys)) {
        $value = $null
        if ($Settings -is [System.Collections.IDictionary]) {
            if ($Settings.Contains($key)) { $value = $Settings[$key] }
        }
        elseif ($Settings.PSObject.Properties.Name -contains $key) {
            $value = $Settings.$key
        }

        if ($null -ne $value -and "$value".Length -gt 0) {
            if ($key -match "Connection|Secret|Key|Password|Storage") {
                $found[$key] = "(set, length $($value.ToString().Length))"
            }
            else {
                $found[$key] = $value
            }
        }
    }

    return $found
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

function Add-ReportLine {
    param([string]$Line = "")
    $script:ReportLines.Add($Line)
    Write-Host $Line
}

function Get-PropertySafe {
    param(
        $Object,
        [string]$Name,
        [string]$Default = ""
    )

    if ($null -eq $Object) { return $Default }
    if ($Object.PSObject.Properties.Name -contains $Name) {
        $value = $Object.$Name
        if ($null -eq $value) { return $Default }
        return $value
    }

    return $Default
}

# --- Main ---

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "Azure CLI (az) is not installed or not on PATH."
}

$account = Invoke-AzCli -Arguments @("account", "show", "-o", "json")
$accountObj = ConvertTo-HashtableFromJson (($account | Out-String).Trim())
if (-not $accountObj) {
    throw "Not logged in to Azure. Run 'az login' first."
}

if ($SubscriptionId) {
    Invoke-AzCli -Arguments @("account", "set", "--subscription", $SubscriptionId) | Out-Null
}

if (-not $SinceUtc) {
    $SinceUtc = (Get-Date).ToUniversalTime().AddHours(-1 * $HoursBack)
}

if ([string]::IsNullOrWhiteSpace($SiteUrl)) {
    if ($SlotName -and $SlotName -ne "production") {
        $SiteUrl = "https://{0}-{1}.azurewebsites.net/" -f $WebAppName, $SlotName
    }
    else {
        $SiteUrl = "https://{0}.azurewebsites.net/" -f $WebAppName
    }
}

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $OutputDir = Join-Path (Get-Location) ("startup-failure-analysis-{0}" -f $stamp)
}

New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
$reportPath = Join-Path $OutputDir "startup-failure-report.txt"
$script:ReportLines = New-Object System.Collections.Generic.List[string]

Add-ReportLine "ASP.NET Core startup failure analysis (HTTP 500.30)"
Add-ReportLine ("Generated (UTC): {0}" -f (Get-Date).ToUniversalTime().ToString("o"))
Add-ReportLine ("Window start (UTC): {0}" -f $SinceUtc.ToUniversalTime().ToString("o"))
Add-ReportLine ("Subscription: {0}" -f $accountObj.name)
Add-ReportLine ("Web app: {0} / slot: {1} / RG: {2}" -f $WebAppName, $SlotName, $ResourceGroup)
Add-ReportLine ("Site URL: {0}" -f $SiteUrl)
Add-ReportLine ("Output: {0}" -f $OutputDir)
Add-ReportLine ("Expected target framework (repo): {0}" -f $ExpectedTargetFramework)

Write-Section "1. Live HTTP probe"
$probe = Get-HttpResponseSummary -Url $SiteUrl
$probe | Format-List | Out-String | ForEach-Object { Add-ReportLine $_ }

if ($probe.Is50030) {
    Write-Finding "ERROR" "Site returns HTTP 500.30 (ASP.NET Core app failed to start)."
}
elseif ($probe.StatusCode -eq 200) {
    Write-Finding "OK" "Site returned HTTP 200 - startup may have recovered since the failure."
}
elseif ($probe.StatusCode -ge 500) {
    Write-Finding "ERROR" ("Site returned HTTP {0}" -f $probe.StatusCode)
}
else {
    Write-Finding "WARN" ("Site returned HTTP {0}" -f $probe.StatusCode)
}

Write-Section "2. App Service state and runtime"
$slotArgs = @("webapp", "show", "-g", $ResourceGroup, "-n", $WebAppName, "-o", "json")
if ($SlotName -and $SlotName -ne "production") {
    $slotArgs += @("--slot", $SlotName)
}

$siteJson = Invoke-AzCli -Arguments $slotArgs
$site = ConvertTo-HashtableFromJson (($siteJson | Out-String).Trim())
Add-ReportLine ("State: {0}" -f $site.state)
Add-ReportLine ("Kind: {0}" -f $site.kind)
Add-ReportLine ("HTTPS Only: {0}" -f $site.httpsOnly)
Add-ReportLine ("Last modified: {0}" -f $site.lastModifiedTimeUtc)

$configArgs = @("webapp", "config", "show", "-g", $ResourceGroup, "-n", $WebAppName, "-o", "json")
if ($SlotName -and $SlotName -ne "production") {
    $configArgs += @("--slot", $SlotName)
}

$configJson = Invoke-AzCli -Arguments $configArgs
$config = ConvertTo-HashtableFromJson (($configJson | Out-String).Trim())
Add-ReportLine ("Always On: {0}" -f (Get-PropertySafe $config "alwaysOn"))
Add-ReportLine ("Linux FX: {0}" -f (Get-PropertySafe $config "linuxFxVersion"))
Add-ReportLine ("Windows FX (.NET): {0}" -f (Get-PropertySafe $config "netFrameworkVersion"))
Add-ReportLine ("Current stack: {0}" -f (Get-PropertySafe $config "currentStack"))
Add-ReportLine ("Use32BitWorker: {0}" -f (Get-PropertySafe $config "use32BitWorkerProcess"))
Add-ReportLine ("Health check path: {0}" -f (Get-PropertySafe $config "healthCheckPath"))

$linuxFx = Get-PropertySafe $config "linuxFxVersion"
if ($linuxFx -and $linuxFx -notmatch "DOTNET|9") {
    Write-Finding "WARN" ("linuxFxVersion is '{0}' - verify .NET 9 runtime is installed." -f $linuxFx)
}

if ($site.state -ne "Running") {
    Write-Finding "ERROR" ("App Service state is '{0}', not Running." -f $site.state)
}

Write-Section "3. App settings required for startup"
$settingsArgs = @("webapp", "config", "appsettings", "list", "-g", $ResourceGroup, "-n", $WebAppName, "-o", "json")
if ($SlotName -and $SlotName -ne "production") {
    $settingsArgs += @("--slot", $SlotName)
}

$settingsJson = Invoke-AzCli -Arguments $settingsArgs
$allSettings = ConvertTo-HashtableFromJson (($settingsJson | Out-String).Trim())

$connArgs = @("webapp", "config", "connection-string", "list", "-g", $ResourceGroup, "-n", $WebAppName, "-o", "json")
if ($SlotName -and $SlotName -ne "production") {
    $connArgs += @("--slot", $SlotName)
}

$connJson = Invoke-AzCli -Arguments $connArgs -AllowFailure
$connectionStrings = @()
if ($LASTEXITCODE -eq 0) {
    $connParsed = ConvertTo-HashtableFromJson (($connJson | Out-String).Trim())
    if ($connParsed) {
        $connectionStrings = @($connParsed | ForEach-Object { $_.name })
    }
}

$startupSettings = Get-StartupRelevantSettings -Settings $allSettings
$startupSettings.GetEnumerator() | Sort-Object Name | ForEach-Object {
    Add-ReportLine ("{0} = {1}" -f $_.Key, $_.Value)
}

if ($connectionStrings.Count -gt 0) {
    Add-ReportLine ("Connection strings (names only): {0}" -f ($connectionStrings -join ", "))
}

$envName = $startupSettings["ASPNETCORE_ENVIRONMENT"]
if (-not $envName) {
    Write-Finding "WARN" "ASPNETCORE_ENVIRONMENT is not set (defaults to Production on Azure)."
}
else {
    Add-ReportLine ("ASPNETCORE_ENVIRONMENT resolved: {0}" -f $envName)
}

$hasDefaultConn = $startupSettings.ContainsKey("ConnectionStrings:DefaultConnection") -or
    $startupSettings.ContainsKey("DefaultConnection") -or
    ($connectionStrings -contains "DefaultConnection")
$hasInfraConn = $startupSettings.ContainsKey("ConnectionStrings:Infrastructure") -or
    $startupSettings.ContainsKey("Infrastructure") -or
    ($connectionStrings -contains "Infrastructure")
if (-not $hasDefaultConn) {
    Write-Finding "ERROR" "DefaultConnection is missing from app settings - startup/DI may fail."
}
if (-not $hasInfraConn) {
    Write-Finding "WARN" "Infrastructure connection string missing - multitenancy/tenant lookup may fail at runtime (not always at host start)."
}

if ($envName -eq "Production" -or [string]::IsNullOrWhiteSpace($envName)) {
    if (-not $startupSettings.ContainsKey("DataProtectionKeyVaultKey")) {
        Write-Finding "WARN" "Production uses Key Vault data protection (Startup.cs) - DataProtectionKeyVaultKey should be set."
    }

    if (-not $startupSettings.ContainsKey("AzureStorage:ConnectionString")) {
        Write-Finding "WARN" "AzureStorage:ConnectionString missing - data protection blob persistence may fail on startup."
    }
}

Write-Section "4. Logging configuration"
$logConfigArgs = @("webapp", "log", "config", "show", "-g", $ResourceGroup, "-n", $WebAppName, "-o", "json")
if ($SlotName -and $SlotName -ne "production") {
    $logConfigArgs += @("--slot", $SlotName)
}

$logConfigJson = Invoke-AzCli -Arguments $logConfigArgs -AllowFailure
if ($LASTEXITCODE -eq 0) {
    $logConfig = ConvertTo-HashtableFromJson (($logConfigJson | Out-String).Trim())
    $appLogs = $logConfig.applicationLogs
    if ($appLogs) {
        $fsLevel = Get-PropertySafe $appLogs.fileSystem "level" "unknown"
        Add-ReportLine ("Application logging: fileSystem level = {0}" -f $fsLevel)
        Add-ReportLine ("HTTP logging: {0}" -f (Get-PropertySafe $logConfig.httpLogs.fileSystem "enabled"))
        Add-ReportLine ("Detailed errors: {0}" -f (Get-PropertySafe $logConfig.detailedErrorMessages "enabled"))
        Add-ReportLine ("Failed request tracing: {0}" -f (Get-PropertySafe $logConfig.failedRequestsTracing "enabled"))

        if ($fsLevel -eq "Off") {
            Write-Finding "WARN" "Application logging is Off - enable at least Error to capture stdout/startup failures in future."
            Add-ReportLine "Enable: az webapp log config --application-logging filesystem --level error -g $ResourceGroup -n $WebAppName --slot $SlotName"
        }
    }
}
else {
    Write-Finding "WARN" "Could not read log configuration."
}

Write-Section "5. Recent deployments"
$deployArgs = @("webapp", "log", "deployment", "list", "-g", $ResourceGroup, "-n", $WebAppName, "-o", "json")
if ($SlotName -and $SlotName -ne "production") {
    $deployArgs += @("--slot", $SlotName)
}

$deployJson = Invoke-AzCli -Arguments $deployArgs -AllowFailure
if ($LASTEXITCODE -eq 0) {
    $deployments = ConvertTo-HashtableFromJson (($deployJson | Out-String).Trim())
    $deploymentRows = foreach ($deployment in @($deployments)) {
        $messageObj = $null
        try { $messageObj = $deployment.message | ConvertFrom-Json } catch { }
        [pscustomobject]@{
            id = $deployment.id
            status = Get-DeploymentStatusLabel $deployment.status
            type = if ($messageObj) { $messageObj.type } else { "" }
            buildId = if ($messageObj) { $messageObj.buildId } else { "" }
            buildNumber = if ($messageObj) { $messageObj.buildNumber } else { "" }
            commitId = if ($messageObj) { $messageObj.commitId } else { "" }
            active = $deployment.active
            start = $deployment.start_time
        }
    }

    $deploymentRows | Select-Object -First 8 | Format-Table -AutoSize | Out-String | ForEach-Object { Add-ReportLine $_ }

    $activeDeploy = $deploymentRows | Where-Object { $_.active -eq $true } | Select-Object -First 1
    if ($activeDeploy) {
        Add-ReportLine ("Active deployment: id={0} build={1} commit={2}" -f $activeDeploy.id, $activeDeploy.buildNumber, $activeDeploy.commitId)
        if ($activeDeploy.status -eq "Failed") {
            Write-Finding "ERROR" "The active deployment on this slot is marked Failed - site files may be inconsistent."
        }
    }

    $recentFailed = $deploymentRows | Where-Object { $_.status -eq "Failed" } | Select-Object -First 1
    if ($recentFailed) {
        Write-Finding "WARN" ("Recent failed deployment id={0} - log: https://{1}-{2}.scm.azurewebsites.net/api/deployments/{0}/log" -f $recentFailed.id, $WebAppName, $SlotName)
    }
}

$logRoot = Join-Path $OutputDir "logs"
if (-not $SkipLogDownload) {
    Write-Section "6. Download App Service logs"
    New-Item -ItemType Directory -Path $logRoot -Force | Out-Null
    $zipPath = Join-Path $OutputDir "webapp-logs.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

    $downloadArgs = @("webapp", "log", "download", "-g", $ResourceGroup, "-n", $WebAppName, "--log-file", $zipPath)
    if ($SlotName -and $SlotName -ne "production") {
        $downloadArgs += @("--slot", $SlotName)
    }

    $downloadOutput = Invoke-AzCli -Arguments $downloadArgs -AllowFailure
    if ($LASTEXITCODE -eq 0 -and (Test-Path $zipPath)) {
        Expand-Archive -Path $zipPath -DestinationPath $logRoot -Force
        Write-Finding "OK" ("Logs extracted to {0}" -f $logRoot)
    }
    else {
        Write-Finding "WARN" ("Log download failed (common when app is down with 500.30): {0}" -f (($downloadOutput | Out-String).Trim()))
        Write-Finding "INFO" "Use Portal > preprod slot > Log stream, or Diagnose and solve problems > Application Crashes."
    }
}

Write-Section "7. Scan logs for startup / ANCM / 500.30 causes"
$patterns = @(
    "500\.30",
    "failed to start",
    "Application startup exception",
    "Hosting failed to start",
    "Unhandled exception",
    "fail:",
    "Fatal",
    "Could not load file or assembly",
    "BadImageFormatException",
    "FileNotFoundException",
    "TypeLoadException",
    "ReflectionTypeLoadException",
    "You must install or update \.NET",
    "It was not possible to find any compatible framework version",
    "No frameworks were found",
    "ANCM",
    "aspnetcorev2_inprocess",
    "stdout",
    "KeyVault",
    "Key Vault",
    "ManagedIdentityCredential",
    "Azure\.Identity",
    "DataProtection",
    "SqlException",
    "connection string",
    "Migration",
    "InvalidOperationException",
    "ArgumentException"
)

if (Test-Path $logRoot) {
    $logHits = @(Search-LogContent -Root $logRoot -Since $SinceUtc -Patterns $patterns)
    if ($logHits.Count -eq 0) {
        Write-Finding "INFO" "No startup-related patterns in downloaded logs (logging may be off or logs rotated)."
    }
    else {
        $matchReport = Join-Path $OutputDir "startup-log-matches.csv"
        $logHits | Sort-Object FileTimeUtc -Descending | Export-Csv -Path $matchReport -NoTypeInformation
        Write-Finding "OK" ("Found {0} matching lines -> {1}" -f $logHits.Count, $matchReport)

        $logHits |
            Group-Object Pattern |
            Sort-Object Count -Descending |
            Select-Object Count, Name |
            Format-Table -AutoSize |
            Out-String |
            ForEach-Object { Add-ReportLine $_ }

        Add-ReportLine ""
        Add-ReportLine "Most relevant matches (newest first):"
        $priority = $logHits | Sort-Object @{
            Expression = {
                switch -Regex ($_.Pattern) {
                    "500\.30|failed to start|startup exception|Hosting failed|Could not load|framework version|No frameworks" { 0 }
                    "Unhandled exception|Fatal|SqlException|KeyVault|DataProtection" { 1 }
                    default { 2 }
                }
            }
        }, @{ Expression = { $_.FileTimeUtc }; Descending = $true }

        $priority |
            Select-Object -First 30 File, Line, Pattern, FileTimeUtc, Text |
            Format-Table -Wrap -AutoSize |
            Out-String |
            ForEach-Object { Add-ReportLine $_ }
    }

    $stdoutFiles = Get-ChildItem -Path $logRoot -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -match "stdout|aspnetcore" }
    if ($stdoutFiles) {
        Add-ReportLine ""
        Add-ReportLine "Stdout / ASP.NET Core log files:"
        foreach ($f in ($stdoutFiles | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 5)) {
            Add-ReportLine ("  {0} (modified {1})" -f $f.FullName.Substring($logRoot.Length), $f.LastWriteTimeUtc.ToString("o"))
            $tail = Get-Content $f.FullName -Tail 40 -ErrorAction SilentlyContinue
            if ($tail) {
                Add-ReportLine "--- tail ---"
                $tail | ForEach-Object { Add-ReportLine $_ }
                Add-ReportLine "--- end tail ---"
            }
        }
    }
    else {
        Write-Finding "WARN" "No stdout/aspnetcore log files in download - enable application logging (Error) and restart the app."
    }

    $eventLog = Get-ChildItem -Path $logRoot -Recurse -Filter "eventlog*.xml" -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1
    if ($eventLog) {
        Add-ReportLine ""
        Add-ReportLine ("Event log: {0}" -f $eventLog.Name)
        $eventContent = Get-Content $eventLog.FullName -Raw -ErrorAction SilentlyContinue
        if ($eventContent -match "Application startup exception") {
            Write-Finding "ERROR" "eventlog contains 'Application startup exception' - see startup-log-matches.csv"
        }
    }
}
else {
    Write-Finding "WARN" "No log directory to scan."
}

if (-not $SkipAppInsights) {
    Write-Section "8. Application Insights (startup exceptions)"
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
            Add-ReportLine ("Using App Insights: {0}" -f $aiName)
        }
    }

    if ($aiName) {
        $sinceIso = $SinceUtc.ToUniversalTime().ToString("o")
        $queries = @{
            StartupExceptions = @"
exceptions
| where timestamp >= datetime($sinceIso)
| where cloud_RoleInstance has "$SlotName" or cloud_RoleName has "$WebAppName"
| where outerMessage has_any ("startup", "hosting", "framework", "assembly", "KeyVault", "DataProtection", "Sql", "500.30")
   or type has_any ("FileNotFoundException", "BadImageFormatException", "InvalidOperationException")
| project timestamp, type, outerMessage, problemId, operation_Name, cloud_RoleInstance
| order by timestamp desc
| take 30
"@
            RecentExceptions = @"
exceptions
| where timestamp >= datetime($sinceIso)
| where cloud_RoleInstance has "$SlotName" or cloud_RoleName has "$WebAppName"
| project timestamp, type, outerMessage, problemId, cloud_RoleInstance
| order by timestamp desc
| take 20
"@
            StartupTraces = @"
traces
| where timestamp >= datetime($sinceIso)
| where cloud_RoleInstance has "$SlotName" or cloud_RoleName has "$WebAppName"
| where message has_any ("Application startup", "Hosting failed", "stopped", "shutting down", "Now listening", "fail", "KeyVault", "Migration")
| project timestamp, message, severityLevel, cloud_RoleInstance
| order by timestamp desc
| take 50
"@
        }

        foreach ($entry in $queries.GetEnumerator()) {
            $queryArgs = @(
                "monitor", "app-insights", "query",
                "--app", $aiName,
                "--resource-group", $ResourceGroup,
                "--analytics-query", $entry.Value,
                "-o", "json"
            )
            $raw = Invoke-AzCli -Arguments $queryArgs -AllowFailure
            Add-ReportLine ""
            Add-ReportLine ("--- App Insights: {0} ---" -f $entry.Key)
            if ($LASTEXITCODE -ne 0) {
                Write-Finding "WARN" ($raw | Out-String)
                continue
            }

            $parsed = ConvertTo-HashtableFromJson (($raw | Out-String).Trim())
            $rows = @(Format-AppInsightsTable -QueryResult $parsed)
            if ($rows.Count -eq 0) {
                Add-ReportLine "(no rows)"
            }
            elseif ($rows[0].PSObject.Properties.Name -contains "Error") {
                Write-Finding "WARN" $rows[0].Error
            }
            else {
                $csvPath = Join-Path $OutputDir ("appinsights-{0}.csv" -f ($entry.Key.ToLower()))
                $rows | Export-Csv -Path $csvPath -NoTypeInformation
                $rows | Select-Object -First 12 | Format-Table -Wrap -AutoSize | Out-String | ForEach-Object { Add-ReportLine $_ }
                Write-Finding "OK" ("Full results: {0}" -f $csvPath)
            }
        }
    }
}

Write-Section "9. Common 500.30 causes for this app"
$hints = @(
    "Missing .NET 9 runtime on App Service (project targets net9.0) - check Site extensions / stack in Portal.",
    "Unhandled exception in ConfigureServices (e.g. DataProtection + Key Vault + managed identity in Production).",
    "Corrupt or partial deploy after failed MSDeploy - restart slot and redeploy.",
    "Missing ConnectionStrings:DefaultConnection or Key Vault / storage settings for Production.",
    "Enable application logging (Error) and restart to capture stdout on next failure.",
    "Kudu: https://$WebAppName-$SlotName.scm.azurewebsites.net/api/logs/docker (if container) or Log stream in Portal."
)

foreach ($hint in $hints) {
    Write-Finding "INFO" $hint
    Add-ReportLine ("HINT: {0}" -f $hint)
}

$script:ReportLines | Set-Content -Path $reportPath -Encoding UTF8
Write-Host ""
Write-Finding "OK" ("Report saved to {0}" -f $reportPath)
