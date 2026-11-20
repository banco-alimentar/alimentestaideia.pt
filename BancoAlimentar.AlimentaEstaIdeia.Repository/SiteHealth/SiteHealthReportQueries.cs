// -----------------------------------------------------------------------
// <copyright file="SiteHealthReportQueries.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.SiteHealth
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Kusto queries for site health reporting against Log Analytics App* tables.
    /// </summary>
    public static class SiteHealthReportQueries
    {
        /// <summary>
        /// Warning and error custom event names monitored in production.
        /// </summary>
        public static readonly string[] MonitoredEventNames =
        {
            "EasypayWebhookRejected",
            "EasypayApiLookupFailed",
            "EasypaySubscriptionLookupFailed",
            "PublicDonationIdNotFound",
            "PayedDonation-To-Failed-Payment-Try",
            "DonationNotFound",
            "DonationIsNull",
            "SecretNotFound",
            "ServicePrincipal-Secret-NotFound",
            "WebhookAmountMismatch",
            "ExistingSinglePayment-NotFound",
        };

        /// <summary>
        /// Builds a query counting monitored AppEvents for a role filter.
        /// </summary>
        /// <param name="window">Kusto ago window (24h or 7d).</param>
        /// <param name="appRoleName">App role name or empty for CI/unscoped.</param>
        /// <param name="asOfUtc">Optional fixed report timestamp.</param>
        /// <returns>Kusto query.</returns>
        public static string EventCountsByName(string window, string appRoleName, DateTime? asOfUtc = null)
        {
            string roleFilter = string.IsNullOrWhiteSpace(appRoleName)
                ? "| where isempty(AppRoleName) or AppRoleName !contains \"alimentaestaideia\""
                : $"| where AppRoleName == \"{Escape(appRoleName)}\"";

            return $@"
AppEvents
{TimeRangeFilter(window, asOfUtc)}
| where Name in ({EventNameList()})
{roleFilter}
| summarize count() by Name
| order by count_ desc";
        }

        /// <summary>
        /// Builds a query for EasypayWebhookRejected reasons.
        /// </summary>
        /// <param name="window">Kusto ago window.</param>
        /// <param name="appRoleName">Production app role name.</param>
        /// <param name="asOfUtc">Optional fixed report timestamp.</param>
        /// <returns>Kusto query.</returns>
        public static string EasypayRejectionReasons(string window, string appRoleName, DateTime? asOfUtc = null)
        {
            return $@"
AppEvents
{TimeRangeFilter(window, asOfUtc)}
| where Name == ""EasypayWebhookRejected""
| where AppRoleName == ""{Escape(appRoleName)}""
| summarize count() by Reason = tostring(Properties.Reason)
| order by count_ desc";
        }

        /// <summary>
        /// Builds a query for distinct EasyPay transaction keys with lookup failures.
        /// </summary>
        /// <param name="window">Kusto ago window.</param>
        /// <param name="appRoleName">Production app role name.</param>
        /// <param name="asOfUtc">Optional fixed report timestamp.</param>
        /// <returns>Kusto query.</returns>
        public static string EasypayLookupDistinctKeys(string window, string appRoleName, DateTime? asOfUtc = null)
        {
            return $@"
AppEvents
{TimeRangeFilter(window, asOfUtc)}
| where Name == ""EasypayApiLookupFailed""
| where AppRoleName == ""{Escape(appRoleName)}""
| summarize DistinctKeys = dcount(tostring(Properties.TransactionKey))";
        }

        /// <summary>
        /// Builds a query counting production HTTP requests.
        /// </summary>
        /// <param name="window">Kusto ago window.</param>
        /// <param name="appRoleName">Production app role name.</param>
        /// <param name="asOfUtc">Optional fixed report timestamp.</param>
        /// <returns>Kusto query.</returns>
        public static string RequestCount(string window, string appRoleName, DateTime? asOfUtc = null)
        {
            return $@"
AppRequests
{TimeRangeFilter(window, asOfUtc)}
| where AppRoleName == ""{Escape(appRoleName)}""
| summarize RequestCount = count()";
        }

        /// <summary>
        /// Builds a query counting failed production HTTP requests (excluding 404).
        /// </summary>
        /// <param name="window">Kusto ago window.</param>
        /// <param name="appRoleName">Production app role name.</param>
        /// <param name="asOfUtc">Optional fixed report timestamp.</param>
        /// <returns>Kusto query.</returns>
        public static string FailedRequestCount(string window, string appRoleName, DateTime? asOfUtc = null)
        {
            return $@"
AppRequests
{TimeRangeFilter(window, asOfUtc)}
| where AppRoleName == ""{Escape(appRoleName)}""
| where Success == false and ResultCode !in (""404"")
| summarize FailedRequestCount = count()";
        }

        /// <summary>
        /// Builds a query counting production exceptions.
        /// </summary>
        /// <param name="window">Kusto ago window.</param>
        /// <param name="appRoleName">Production app role name.</param>
        /// <param name="asOfUtc">Optional fixed report timestamp.</param>
        /// <returns>Kusto query.</returns>
        public static string ExceptionCount(string window, string appRoleName, DateTime? asOfUtc = null)
        {
            return $@"
AppExceptions
{TimeRangeFilter(window, asOfUtc)}
| where AppRoleName == ""{Escape(appRoleName)}""
| summarize ExceptionCount = count()";
        }

        /// <summary>
        /// Builds a time range relative to a fixed report timestamp, or a rolling window when omitted.
        /// </summary>
        /// <param name="window">Kusto ago window (24h or 7d).</param>
        /// <param name="asOfUtc">Optional fixed end time in UTC.</param>
        /// <returns>Kusto time filter.</returns>
        public static string TimeRangeFilter(string window, DateTime? asOfUtc)
        {
            if (!asOfUtc.HasValue)
            {
                return $"| where TimeGenerated > ago({window})";
            }

            TimeSpan duration = string.Equals(window, "7d", StringComparison.OrdinalIgnoreCase)
                ? TimeSpan.FromDays(7)
                : TimeSpan.FromHours(24);
            DateTime endUtc = asOfUtc.Value.Kind == DateTimeKind.Utc
                ? asOfUtc.Value
                : asOfUtc.Value.ToUniversalTime();
            DateTime startUtc = endUtc - duration;
            string start = startUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture);
            string end = endUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture);
            return $"| where TimeGenerated > datetime({start}) and TimeGenerated <= datetime({end})";
        }

        private static string EventNameList()
        {
            return string.Join(", ", System.Array.ConvertAll(MonitoredEventNames, name => $"\"{name}\""));
        }

        private static string Escape(string value)
        {
            return value?.Replace("\"", "\\\"", System.StringComparison.Ordinal) ?? string.Empty;
        }
    }
}
