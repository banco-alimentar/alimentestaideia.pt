# Diagnose HTTP 5xx on a specific Razor page using Azure App Service + Application Insights.
#
# Usage:
#   az login
#   .\scripts\analyze-page-failure-app-insights.ps1
#   .\scripts\analyze-page-failure-app-insights.ps1 -PageUrl "https://dev.alimentestaideia.pt/Identity/Account/Manage/CampaignsHistory"
#   .\scripts\analyze-page-failure-app-insights.ps1 -PagePathFilter "CampaignsHistory" -HoursBack 6
#   .\scripts\analyze-page-failure-app-insights.ps1 -SlotName preprod -SkipHttpProbe
#
# Notes:
#   - Unauthenticated probes often return 302/200 (login) instead of 500. The 500 usually appears
#     only for signed-in users; App Insights queries below still capture server-side failures.
#   - Requires: Azure CLI (az) with read access to the web app and Application Insights resource.
#
# Related pages:
#   scripts/analyze-app-service-startup-failure.ps1
#   scripts/analyze-app-service-slot-swap-logs.ps1

[CmdletBinding()]
param(
    [string]$SubscriptionId,
    [string]$ResourceGroup = "AlimenteEstaIdeia",
    [string]$WebAppName = "alimentaestaideia",
    [string]$SlotName = "preprod",
    [string]$PageUrl = "https://dev.alimentestaideia.pt/Identity/Account/Manage/CampaignsHistory",
    [string]$PagePathFilter = "CampaignsHistory",
    [string]$AppInsightsName = "",
    [int]$HoursBack = 24,
    [datetime]$SinceUtc,
    [string]$OutputDir = "",
    [switch]$SkipHttpProbe,
    [switch]$SkipAppInsights,
    [switch]$SkipLogDownload
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

function Test-HttpProbe {
    param(
        [string]$Url,
        [int]$TimeoutSec = 60
    )

    $result = [ordered]@{
        Url = $Url
        StatusCode = $null
        ElapsedMs = $null
        Location = $null
        BodySnippet = $null
        Error = $null
    }

    $bodyFile = [System.IO.Path]::GetTempFileName()
    try {
        if (Get-Command curl.exe -ErrorAction SilentlyContinue) {
            $format = "%{http_code}|%{time_total}|%{redirect_url}"
            $raw = & curl.exe -s -L --max-time $TimeoutSec -o $bodyFile -w $format $Url 2>&1
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
        }
        else {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec $TimeoutSec -MaximumRedirection 5
            $result.StatusCode = [int]$response.StatusCode
            Set-Content -Path $bodyFile -Value $response.Content -Encoding UTF8
        }

        if (Test-Path $bodyFile) {
            $snippet = Get-Content $bodyFile -Raw -ErrorAction SilentlyContinue
            if ($snippet) {
                $result.BodySnippet = ($snippet.Substring(0, [Math]::Min(400, $snippet.Length)) -replace "\s+", " ").Trim()
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

function Format-AppInsightsTable {
    param($QueryResult)

    if ($null -eq $QueryResult) { return @() }
    if ($QueryResult -is [hashtable] -and $QueryResult.Error) {
        return @([pscustomobject]@{ Error = ($QueryResult.Error | Out-String).Trim() })
    }

    if (-not $QueryResult.tables) { return @() }

    $table = $QueryResult.tables[0]
    if (-not $table.rows -or $table.rows.Count -eq 0) { return @() }

    $columns = @($table.columns | ForEach-Object { $_.name })
    $rows = foreach ($row in $table.rows) {
        $item = [ordered]@{}
        for ($i = 0; $i -lt $columns.Count; $i++) {
            $item[$columns[$i]] = $row[$i]
        }

        [pscustomobject]$item
    }

    return @($rows)
}

function Get-AppInsightsComponentName {
    param(
        [string]$ResourceGroupName,
        [string]$PreferredName
    )

    if (-not [string]::IsNullOrWhiteSpace($PreferredName)) {
        return $PreferredName
    }

    $aiComponents = Invoke-AzCli -Arguments @(
        "resource", "list",
        "-g", $ResourceGroupName,
        "--resource-type", "Microsoft.Insights/components",
        "-o", "json"
    ) -AllowFailure

    if ($LASTEXITCODE -ne 0) { return $null }

    $components = ConvertTo-HashtableFromJson (($aiComponents | Out-String).Trim())
    if (-not $components -or @($components).Count -eq 0) { return $null }

    $preferred = @($components) | Where-Object { $_.name -match "alimentaestaideia|alimentestaideia" } | Select-Object -First 1
    if (-not $preferred) { $preferred = @($components)[0] }
    return $preferred.name
}

function Invoke-AppInsightsQuery {
    param(
        [string]$AppName,
        [string]$ResourceGroupName,
        [string]$Query
    )

    $queryArgs = @(
        "monitor", "app-insights", "query",
        "--app", $AppName,
        "--resource-group", $ResourceGroupName,
        "--analytics-query", $Query,
        "-o", "json"
    )

    $raw = Invoke-AzCli -Arguments $queryArgs -AllowFailure
    if ($LASTEXITCODE -ne 0) {
        return @{ Error = ($raw | Out-String) }
    }

    return (ConvertTo-HashtableFromJson (($raw | Out-String).Trim()))
}

function Write-QueryResults {
    param(
        [string]$Title,
        [string]$CsvPath,
        $QueryResult,
        [int]$PreviewCount = 15
    )

    Add-ReportLine ""
    Add-ReportLine ("--- {0} ---" -f $Title)
    $rows = @(Format-AppInsightsTable -QueryResult $QueryResult)
    if ($rows.Count -eq 0) {
        Add-ReportLine "(no rows)"
        return @()
    }

    if ($rows[0].PSObject.Properties.Name -contains "Error") {
        Write-Finding "WARN" $rows[0].Error
        return @()
    }

    $rows | Export-Csv -Path $CsvPath -NoTypeInformation
    $rows | Select-Object -First $PreviewCount | Format-Table -Wrap -AutoSize | Out-String | ForEach-Object { Add-ReportLine $_ }
    Write-Finding "OK" ("Saved {0} rows -> {1}" -f $rows.Count, $CsvPath)
    return $rows
}

function Get-PageFilterKql {
    param([string]$PathFilter)

    $escaped = $PathFilter.Replace("'", "''")
    return "(url contains ""/$escaped"" or name contains ""$escaped"" or operation_Name contains ""$escaped"")"
}

function Add-ReportLine {
    param([string]$Line)
    $script:ReportLines.Add($Line)
    Write-Host $Line
}

if (-not $SinceUtc) {
    $SinceUtc = (Get-Date).ToUniversalTime().AddHours(-1 * $HoursBack)
}

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $safeName = ($PagePathFilter -replace "[^A-Za-z0-9_-]", "-").ToLowerInvariant()
    $OutputDir = Join-Path (Split-Path -Parent $PSCommandPath) ("output/{0}-analysis-{1:yyyyMMdd-HHmmss}" -f $safeName, (Get-Date))
}

New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
$ReportPath = Join-Path $OutputDir "report.txt"
$script:ReportLines = New-Object System.Collections.Generic.List[string]

Write-Section "Page failure analysis"
Add-ReportLine ("Page URL: {0}" -f $PageUrl)
Add-ReportLine ("Page filter: {0}" -f $PagePathFilter)
Add-ReportLine ("Resource group: {0}" -f $ResourceGroup)
Add-ReportLine ("Web app: {0} (slot: {1})" -f $WebAppName, $SlotName)
Add-ReportLine ("Since (UTC): {0}" -f $SinceUtc.ToString("o"))

if ($SubscriptionId) {
    Invoke-AzCli -Arguments @("account", "set", "--subscription", $SubscriptionId) | Out-Null
    Add-ReportLine ("Subscription: {0}" -f $SubscriptionId)
}
else {
    $subJson = Invoke-AzCli -Arguments @("account", "show", "-o", "json")
    $sub = ConvertTo-HashtableFromJson (($subJson | Out-String).Trim())
    Add-ReportLine ("Subscription: {0} ({1})" -f $sub.name, $sub.id)
}

if (-not $SkipHttpProbe) {
    Write-Section "1. HTTP probe (anonymous)"
    $probe = Test-HttpProbe -Url $PageUrl
    Add-ReportLine ("Status: {0}  Elapsed: {1} ms" -f $probe.StatusCode, $probe.ElapsedMs)
    if ($probe.Location) { Add-ReportLine ("Redirect: {0}" -f $probe.Location) }
    if ($probe.Error) { Add-ReportLine ("Error: {0}" -f $probe.Error) }

    switch ($probe.StatusCode) {
        500 { Write-Finding "ERROR" "Anonymous probe returned HTTP 500." }
        302 { Write-Finding "INFO" "HTTP 302 - likely redirect to login. Server 500 may only occur when authenticated; rely on App Insights below." }
        200 {
            if ($probe.BodySnippet -match "Log in|Login|Entrar") {
                Write-Finding "INFO" "HTTP 200 with login content - page requires authentication for the real error path."
            }
            else {
                Write-Finding "OK" "HTTP 200 returned."
            }
        }
        default {
            if ($probe.StatusCode -ge 500) {
                Write-Finding "ERROR" ("HTTP {0}" -f $probe.StatusCode)
            }
            else {
                Write-Finding "WARN" ("HTTP {0}" -f $probe.StatusCode)
            }
        }
    }
}

Write-Section "2. App Service slot state"
$slotArgs = @("webapp", "show", "-g", $ResourceGroup, "-n", $WebAppName, "-o", "json")
if ($SlotName -and $SlotName -ne "production") {
    $slotArgs += @("--slot", $SlotName)
}

$siteJson = Invoke-AzCli -Arguments $slotArgs -AllowFailure
if ($LASTEXITCODE -eq 0) {
    $site = ConvertTo-HashtableFromJson (($siteJson | Out-String).Trim())
    Add-ReportLine ("State: {0}" -f $site.state)
    Add-ReportLine ("Host: {0}" -f $site.defaultHostName)
    Add-ReportLine ("Last modified: {0}" -f $site.lastModifiedTimeUtc)
    if ($site.state -ne "Running") {
        Write-Finding "ERROR" ("Slot state is '{0}'." -f $site.state)
    }
}
else {
    Write-Finding "WARN" "Could not read web app metadata."
}

if (-not $SkipAppInsights) {
    Write-Section "3. Application Insights queries"
    $aiName = Get-AppInsightsComponentName -ResourceGroupName $ResourceGroup -PreferredName $AppInsightsName
    if (-not $aiName) {
        Write-Finding "ERROR" "No Application Insights component found in resource group $ResourceGroup."
    }
    else {
        Add-ReportLine ("App Insights component: {0}" -f $aiName)
        $sinceIso = $SinceUtc.ToUniversalTime().ToString("o")
        $pageFilter = Get-PageFilterKql -PathFilter $PagePathFilter
        $slotFilter = if ($SlotName -and $SlotName -ne "production") {
            "| where cloud_RoleInstance has '$SlotName' or cloud_RoleName has '$WebAppName'"
        }
        else {
            ""
        }

        $queries = [ordered]@{
            FailedRequests = @"
requests
| where timestamp >= datetime($sinceIso)
$slotFilter
| where $pageFilter
| where success == false or toint(resultCode) >= 500
| project timestamp, name, url, resultCode, duration, success, operation_Id, user_AuthenticatedId, cloud_RoleInstance
| order by timestamp desc
| take 50
"@
            ServerExceptions = @"
exceptions
| where timestamp >= datetime($sinceIso)
$slotFilter
| where client_Type == ""PC""
| where operation_Name contains ""$($PagePathFilter.Replace('"', '\"'))""
   or tostring(customDimensions.RequestPath) contains ""$($PagePathFilter.Replace('"', '\"'))""
   or outerMessage contains ""$($PagePathFilter.Replace('"', '\"'))""
| project timestamp, type, outerMessage, innermostMessage, problemId, operation_Name, operation_Id, method, assembly
| order by timestamp desc
| take 30
"@
            RequestExceptionCorrelation = @"
let since = datetime($sinceIso);
let pageRequests = requests
| where timestamp >= since
$slotFilter
| where $pageFilter
| where success == false or toint(resultCode) >= 500
| project operation_Id, reqTime=timestamp, name, url, resultCode;
pageRequests
| join kind=inner (
    exceptions
    | where timestamp >= since
    | where client_Type == ""PC""
) on `$left.operation_Id == `$right.operation_Id
| project reqTime, resultCode, url, name, exTime=timestamp, type, outerMessage, innermostMessage, problemId, operation_Id
| order by reqTime desc
| take 30
"@
            PageExceptions = @"
exceptions
| where timestamp >= datetime($sinceIso)
$slotFilter
| where client_Type == ""PC""
| where operation_Name contains ""$($PagePathFilter.Replace('"', '\"'))""
   or tostring(customDimensions.RequestPath) contains ""$($PagePathFilter.Replace('"', '\"'))""
   or outerMessage contains ""$($PagePathFilter.Replace('"', '\"'))""
   or details contains ""$($PagePathFilter.Replace('"', '\"'))""
| project timestamp, type, outerMessage, innermostMessage, problemId, operation_Name, operation_Id, cloud_RoleInstance, details
| order by timestamp desc
| take 40
"@
            ExceptionSummary = @"
exceptions
| where timestamp >= datetime($sinceIso)
$slotFilter
| where client_Type == ""PC""
| where operation_Name contains ""$($PagePathFilter.Replace('"', '\"'))""
   or tostring(customDimensions.RequestPath) contains ""$($PagePathFilter.Replace('"', '\"'))""
| summarize count(), lastSeen=max(timestamp), sampleMessage=take_any(outerMessage) by problemId, type
| order by count_ desc, lastSeen desc
| take 20
"@
            DependencyFailures = @"
let since = datetime($sinceIso);
let ops = materialize(
    requests
    | where timestamp >= since
    $slotFilter
    | where $pageFilter
    | where success == false or toint(resultCode) >= 500
    | project operation_Id
);
dependencies
| where timestamp >= since
| where operation_Id in (ops)
| where success == false
| project timestamp, name, type, target, resultCode, duration, operation_Id, data
| order by timestamp desc
| take 40
"@
            ErrorTraces = @"
traces
| where timestamp >= datetime($sinceIso)
$slotFilter
| where severityLevel >= 3
| where message has '$($PagePathFilter.Replace("'", "''"))' or operation_Name has '$($PagePathFilter.Replace("'", "''"))'
| project timestamp, message, severityLevel, operation_Name, cloud_RoleInstance
| order by timestamp desc
| take 40
"@
        }

        $allRows = @{}
        foreach ($entry in $queries.GetEnumerator()) {
            $parsed = Invoke-AppInsightsQuery -AppName $aiName -ResourceGroupName $ResourceGroup -Query $entry.Value
            $csvPath = Join-Path $OutputDir ("appinsights-{0}.csv" -f ($entry.Key.ToLower()))
            $rows = Write-QueryResults -Title $entry.Key -CsvPath $csvPath -QueryResult $parsed
            $allRows[$entry.Key] = $rows
        }

        Write-Section "4. Diagnosis summary"
        $correlated = @($allRows["RequestExceptionCorrelation"])
        $failed = @($allRows["FailedRequests"])
        $summary = @($allRows["ExceptionSummary"])
        $serverExceptions = @($allRows["ServerExceptions"])

        if ($failed.Count -eq 0 -and $correlated.Count -eq 0 -and $serverExceptions.Count -eq 0) {
            Write-Finding "WARN" "No failed requests or server exceptions matched '$PagePathFilter' in the last $HoursBack hour(s)."
            Add-ReportLine "Try: increase -HoursBack, reproduce while logged in, or verify App Insights slot filter (-SlotName)."
        }
        else {
            if ($correlated.Count -gt 0) {
                $top = $correlated | Select-Object -First 1
                Write-Finding "ERROR" ("Most recent correlated failure: HTTP {0} -> {1}: {2}" -f $top.resultCode, $top.type, $top.outerMessage)
                if ($top.innermostMessage) {
                    Add-ReportLine ("Inner: {0}" -f $top.innermostMessage)
                }
            }
            elseif ($serverExceptions.Count -gt 0) {
                $topEx = $serverExceptions | Select-Object -First 1
                Write-Finding "ERROR" ("Recent server exception: {0}: {1}" -f $topEx.type, $topEx.outerMessage)
                if ($topEx.innermostMessage) {
                    Add-ReportLine ("Inner: {0}" -f $topEx.innermostMessage)
                }
                if ($topEx.method) {
                    Add-ReportLine ("At: {0}" -f $topEx.method)
                }
            }

            if ($summary.Count -gt 0) {
                Add-ReportLine ""
                Add-ReportLine "Top recurring problems:"
                $summary | Select-Object -First 5 | Format-Table count_, type, problemId, sampleMessage -Wrap -AutoSize | Out-String | ForEach-Object { Add-ReportLine $_ }
            }
        }

        if ($PagePathFilter -eq "CampaignsHistory") {
            Write-Section "5. Known CampaignsHistory failure patterns (check stack trace)"
            $hints = @(
                "SqlException Invalid column 'LinkOpenCount' / 'TagLine' -> EF migrations not applied on staging DB (20260620130000, 20260620140000, 20260621050000).",
                "NullReferenceException on user.Id when User is null (OnGetAsync/OnPostAsync) - missing null check after GetUserAsync.",
                "NullReferenceException on item.Donations in CampaignsHistory.cshtml when summing donated totals.",
                "ReferralImageService.ResolveUrl or blob storage misconfiguration for campaign images.",
                "QRCoder / ReferralQrCodeService failure when generating QR PNG data URIs for each campaign card.",
                "Heavy GetUserReferrals query timeout (includes all DonationItems) under load."
            )
            foreach ($hint in $hints) { Add-ReportLine ("  - {0}" -f $hint) }
        }
    }
}

if (-not $SkipLogDownload) {
    Write-Section "6. App Service logs (optional download)"
    $logRoot = Join-Path $OutputDir "logs"
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

        $patterns = @("CampaignsHistory", "NullReferenceException", "SqlException", "Unhandled exception", "fail:", "500")
        $matches = @()
        Get-ChildItem -Path $logRoot -Recurse -File -ErrorAction SilentlyContinue | ForEach-Object {
            $lineNo = 0
            foreach ($line in [System.IO.File]::ReadLines($_.FullName)) {
                $lineNo++
                foreach ($pattern in $patterns) {
                    if ($line -match $pattern) {
                        $matches += [pscustomobject]@{
                            File = $_.FullName.Substring($logRoot.Length)
                            Line = $lineNo
                            Pattern = $pattern
                            Text = $line.Trim()
                        }
                        break
                    }
                }
            }
        }

        if ($matches.Count -gt 0) {
            $matchCsv = Join-Path $OutputDir "log-matches.csv"
            $matches | Export-Csv -Path $matchCsv -NoTypeInformation
            Write-Finding "OK" ("Found {0} log lines -> {1}" -f $matches.Count, $matchCsv)
            $matches | Select-Object -First 20 | Format-Table -Wrap -AutoSize | Out-String | ForEach-Object { Add-ReportLine $_ }
        }
        else {
            Write-Finding "INFO" "No CampaignsHistory/exception patterns in downloaded logs."
        }
    }
    else {
        Write-Finding "WARN" ("Log download skipped or failed: {0}" -f (($downloadOutput | Out-String).Trim()))
    }
}

Write-Section "Done"
Add-ReportLine ("Report saved to: {0}" -f $ReportPath)
$script:ReportLines | Out-File -FilePath $ReportPath -Encoding UTF8
Write-Finding "OK" ("Analysis output directory: {0}" -f (Resolve-Path $OutputDir))
