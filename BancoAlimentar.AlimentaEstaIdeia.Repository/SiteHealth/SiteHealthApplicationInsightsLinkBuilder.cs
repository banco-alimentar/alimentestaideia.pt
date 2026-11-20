// -----------------------------------------------------------------------
// <copyright file="SiteHealthApplicationInsightsLinkBuilder.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.SiteHealth
{
    using System;

    /// <summary>
    /// Builds Azure portal Log Analytics links for site health issues.
    /// </summary>
    public static class SiteHealthApplicationInsightsLinkBuilder
    {
        /// <summary>
        /// Default Log Analytics workspace ARM resource id (see Documentation/Application-Insights.md).
        /// </summary>
        public const string DefaultWorkspaceResourceId =
            "/subscriptions/f1b937fb-ca82-4eb6-a452-77af7a531344/resourceGroups/defaultresourcegroup-weu/providers/Microsoft.OperationalInsights/workspaces/DefaultWorkspace-f1b937fb-ca82-4eb6-a452-77af7a531344-WEU";

        private const string PortalLogsBladeBase =
            "https://portal.azure.com/#view/Microsoft_Azure_Monitoring_Logs/LogsBlade/resourceId/";

        /// <summary>
        /// Builds a portal URL that opens Log Analytics with a pre-filled query for an issue.
        /// </summary>
        /// <param name="issueCode">Stable issue code from the report.</param>
        /// <param name="windowLabel">Report window label (24h or 7d).</param>
        /// <param name="appRoleName">Application Insights cloud role name.</param>
        /// <param name="options">Site health configuration.</param>
        /// <param name="asOfUtc">Optional fixed report timestamp.</param>
        /// <returns>Azure portal URL, or null when the issue has no drill-down query.</returns>
        public static string BuildInvestigateUrlForRole(
            string issueCode,
            string windowLabel,
            string appRoleName,
            SiteHealthReportOptions options,
            DateTime? asOfUtc = null)
        {
            if (string.IsNullOrWhiteSpace(issueCode) || string.IsNullOrWhiteSpace(appRoleName))
            {
                return null;
            }

            string query = BuildInvestigateQueryForRole(issueCode, windowLabel, appRoleName, asOfUtc);
            string workspaceResourceId = ResolveWorkspaceResourceId(options);
            return BuildPortalUrl(workspaceResourceId, query, MapTimespan(windowLabel));
        }

        /// <summary>
        /// Builds a portal URL that opens Log Analytics with a pre-filled query for an issue.
        /// </summary>
        /// <param name="issueCode">Stable issue code from the report.</param>
        /// <param name="windowLabel">Report window label (24h or 7d).</param>
        /// <param name="scope">Telemetry scope filter.</param>
        /// <param name="options">Site health configuration.</param>
        /// <param name="asOfUtc">Optional fixed report timestamp.</param>
        /// <returns>Azure portal URL, or null when the issue has no drill-down query.</returns>
        public static string BuildInvestigateUrl(
            string issueCode,
            string windowLabel,
            SiteHealthTelemetryScope scope,
            SiteHealthReportOptions options,
            DateTime? asOfUtc = null)
        {
            if (string.IsNullOrWhiteSpace(issueCode))
            {
                return null;
            }

            string roleName = scope == SiteHealthTelemetryScope.Developer
                ? options.DeveloperAppRoleName
                : options.ProductionAppRoleName;

            if (scope == SiteHealthTelemetryScope.CiUnscoped)
            {
                string query = BuildInvestigateQuery(issueCode, windowLabel, scope, options, asOfUtc);
                if (string.IsNullOrWhiteSpace(query))
                {
                    return null;
                }

                string workspaceResourceId = ResolveWorkspaceResourceId(options);
                return BuildPortalUrl(workspaceResourceId, query, MapTimespan(windowLabel));
            }

            return BuildInvestigateUrlForRole(issueCode, windowLabel, roleName, options, asOfUtc);
        }

        /// <summary>
        /// Builds a portal URL for failed HTTP requests in the reporting window.
        /// </summary>
        /// <param name="windowLabel">Report window label (24h or 7d).</param>
        /// <param name="productionAppRoleName">Production cloud role name.</param>
        /// <param name="options">Site health configuration.</param>
        /// <param name="asOfUtc">Optional fixed report timestamp.</param>
        /// <returns>Azure portal URL.</returns>
        public static string BuildFailedRequestsUrl(
            string windowLabel,
            string productionAppRoleName,
            SiteHealthReportOptions options,
            DateTime? asOfUtc = null)
        {
            string window = NormalizeWindow(windowLabel);
            string query = $@"
AppRequests
{SiteHealthReportQueries.TimeRangeFilter(window, asOfUtc)}
| where AppRoleName == ""{EscapeKusto(productionAppRoleName)}""
| where Success == false and ResultCode !in (""404"")
| project TimeGenerated, Name, Url, ResultCode, DurationMs, OperationId
| order by TimeGenerated desc
| take 500";

            string workspaceResourceId = ResolveWorkspaceResourceId(options);
            return BuildPortalUrl(workspaceResourceId, query, MapTimespan(windowLabel));
        }

        /// <summary>
        /// Builds a portal URL for unhandled exceptions in the reporting window.
        /// </summary>
        /// <param name="windowLabel">Report window label (24h or 7d).</param>
        /// <param name="productionAppRoleName">Production cloud role name.</param>
        /// <param name="options">Site health configuration.</param>
        /// <param name="asOfUtc">Optional fixed report timestamp.</param>
        /// <returns>Azure portal URL.</returns>
        public static string BuildExceptionsUrl(
            string windowLabel,
            string productionAppRoleName,
            SiteHealthReportOptions options,
            DateTime? asOfUtc = null)
        {
            string window = NormalizeWindow(windowLabel);
            string query = $@"
AppExceptions
{SiteHealthReportQueries.TimeRangeFilter(window, asOfUtc)}
| where AppRoleName == ""{EscapeKusto(productionAppRoleName)}""
| project TimeGenerated, Type, OuterMessage, Details, OperationId
| order by TimeGenerated desc
| take 500";

            string workspaceResourceId = ResolveWorkspaceResourceId(options);
            return BuildPortalUrl(workspaceResourceId, query, MapTimespan(windowLabel));
        }

        private static string BuildInvestigateQueryForRole(
            string issueCode,
            string windowLabel,
            string appRoleName,
            DateTime? asOfUtc)
        {
            string window = NormalizeWindow(windowLabel);
            string roleFilter = BuildRoleFilterForName(appRoleName);
            string timeFilter = SiteHealthReportQueries.TimeRangeFilter(window, asOfUtc);

            if (string.Equals(issueCode, "FailedHttpRequests", StringComparison.OrdinalIgnoreCase))
            {
                return $@"
AppRequests
{timeFilter}
{roleFilter}
| where Success == false and ResultCode !in (""404"")
| project TimeGenerated, Name, Url, ResultCode, DurationMs, OperationId
| order by TimeGenerated desc
| take 500";
            }

            if (string.Equals(issueCode, "UnhandledExceptions", StringComparison.OrdinalIgnoreCase))
            {
                return $@"
AppExceptions
{timeFilter}
{roleFilter}
| project TimeGenerated, Type, OuterMessage, Details, OperationId
| order by TimeGenerated desc
| take 500";
            }

            const string webhookPrefix = "EasypayWebhookRejected:";
            if (issueCode.StartsWith(webhookPrefix, StringComparison.OrdinalIgnoreCase))
            {
                string reason = issueCode.Substring(webhookPrefix.Length);
                return $@"
AppEvents
{timeFilter}
| where Name == ""EasypayWebhookRejected""
{roleFilter}
| where tostring(Properties.Reason) == ""{EscapeKusto(reason)}""
| project TimeGenerated, Name, AppRoleName, Properties
| order by TimeGenerated desc
| take 500";
            }

            return $@"
AppEvents
{timeFilter}
| where Name == ""{EscapeKusto(issueCode)}""
{roleFilter}
| project TimeGenerated, Name, AppRoleName, Properties
| order by TimeGenerated desc
| take 500";
        }

        private static string BuildRoleFilterForName(string appRoleName)
        {
            return $"| where AppRoleName == \"{EscapeKusto(appRoleName)}\"";
        }

        private static string BuildInvestigateQuery(
            string issueCode,
            string windowLabel,
            SiteHealthTelemetryScope scope,
            SiteHealthReportOptions options,
            DateTime? asOfUtc)
        {
            string window = NormalizeWindow(windowLabel);
            string roleFilter = BuildRoleFilter(scope, options);
            string timeFilter = SiteHealthReportQueries.TimeRangeFilter(window, asOfUtc);

            if (string.Equals(issueCode, "FailedHttpRequests", StringComparison.OrdinalIgnoreCase))
            {
                return $@"
AppRequests
{timeFilter}
{roleFilter}
| where Success == false and ResultCode !in (""404"")
| project TimeGenerated, Name, Url, ResultCode, DurationMs, OperationId
| order by TimeGenerated desc
| take 500";
            }

            if (string.Equals(issueCode, "UnhandledExceptions", StringComparison.OrdinalIgnoreCase))
            {
                return $@"
AppExceptions
{timeFilter}
{roleFilter}
| project TimeGenerated, Type, OuterMessage, Details, OperationId
| order by TimeGenerated desc
| take 500";
            }

            const string webhookPrefix = "EasypayWebhookRejected:";
            if (issueCode.StartsWith(webhookPrefix, StringComparison.OrdinalIgnoreCase))
            {
                string reason = issueCode.Substring(webhookPrefix.Length);
                return $@"
AppEvents
{timeFilter}
| where Name == ""EasypayWebhookRejected""
{roleFilter}
| where tostring(Properties.Reason) == ""{EscapeKusto(reason)}""
| project TimeGenerated, Name, AppRoleName, Properties
| order by TimeGenerated desc
| take 500";
            }

            return $@"
AppEvents
{timeFilter}
| where Name == ""{EscapeKusto(issueCode)}""
{roleFilter}
| project TimeGenerated, Name, AppRoleName, Properties
| order by TimeGenerated desc
| take 500";
        }

        private static string BuildRoleFilter(SiteHealthTelemetryScope scope, SiteHealthReportOptions options)
        {
            if (scope == SiteHealthTelemetryScope.CiUnscoped)
            {
                return "| where isempty(AppRoleName) or AppRoleName !contains \"alimentaestaideia\"";
            }

            string roleName = scope == SiteHealthTelemetryScope.Developer
                ? options.DeveloperAppRoleName
                : options.ProductionAppRoleName;

            return $"| where AppRoleName == \"{EscapeKusto(roleName)}\"";
        }

        private static string BuildPortalUrl(string workspaceResourceId, string query, string timespan)
        {
            string encodedResourceId = Uri.EscapeDataString(workspaceResourceId);
            string encodedQuery = Uri.EscapeDataString(query.Trim());
            return $"{PortalLogsBladeBase}{encodedResourceId}/source/LogsBlade.AnalyticsShareLinkToQuery/query/{encodedQuery}/timespan/{timespan}";
        }

        private static string ResolveWorkspaceResourceId(SiteHealthReportOptions options)
        {
            return string.IsNullOrWhiteSpace(options?.LogAnalyticsWorkspaceResourceId)
                ? DefaultWorkspaceResourceId
                : options.LogAnalyticsWorkspaceResourceId;
        }

        private static string NormalizeWindow(string windowLabel)
        {
            return string.Equals(windowLabel, "7d", StringComparison.OrdinalIgnoreCase) ? "7d" : "24h";
        }

        private static string MapTimespan(string windowLabel)
        {
            return string.Equals(windowLabel, "7d", StringComparison.OrdinalIgnoreCase) ? "P7D" : "P1D";
        }

        private static string EscapeKusto(string value)
        {
            return value?.Replace("\"", "\\\"", StringComparison.Ordinal) ?? string.Empty;
        }
    }
}
