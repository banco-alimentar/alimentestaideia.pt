// -----------------------------------------------------------------------
// <copyright file="SiteHealthIssueCatalog.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.SiteHealth
{
    using System.Collections.Generic;

    /// <summary>
    /// Static impact descriptions for monitored Application Insights events.
    /// </summary>
    public static class SiteHealthIssueCatalog
    {
        private static readonly Dictionary<string, IssueTemplate> Templates = new Dictionary<string, IssueTemplate>(System.StringComparer.OrdinalIgnoreCase)
        {
            ["EasypayWebhookRejected:easypay_api_lookup_failed"] = new IssueTemplate(
                "EasyPay webhook — API lookup failed",
                SiteHealthReportSeverity.Critical,
                "Production webhooks were rejected because EasyPay could not confirm the payment. Donations may remain unpaid until the Multibanco timer or manual intervention catches up.",
                "Check EasyPay credentials per food bank and verify the payment id exists in the merchant account. See Admin → Search Easypay for affected transaction keys."),
            ["EasypayWebhookRejected:unknown_transaction_key"] = new IssueTemplate(
                "EasyPay webhook — unknown transaction key",
                SiteHealthReportSeverity.Warning,
                "EasyPay sent a webhook for a transaction key that is not in the database. Usually test traffic, replays, or wrong environment.",
                "Confirm EasyPay callback URLs point to the correct slot. Ignore if limited to the developer slot or CI."),
            ["EasypayWebhookRejected:amount_mismatch"] = new IssueTemplate(
                "EasyPay webhook — amount mismatch",
                SiteHealthReportSeverity.Critical,
                "A paid amount from EasyPay did not match the donation amount. Payment was rejected to prevent incorrect settlement.",
                "Investigate immediately in Admin → Search Easypay and EasyPay backoffice."),
            ["EasypayWebhookRejected:merchant_key_mismatch"] = new IssueTemplate(
                "EasyPay webhook — merchant key mismatch",
                SiteHealthReportSeverity.Critical,
                "EasyPay payment key did not match the donation public id. Possible misconfiguration or fraud attempt.",
                "Verify food bank EasyPay configuration and affected donations."),
            ["EasypayWebhookRejected:easypay_payment_not_paid"] = new IssueTemplate(
                "EasyPay webhook — payment not paid yet",
                SiteHealthReportSeverity.Info,
                "Webhook arrived before EasyPay marked the payment as paid. Often timing; EasyPay may retry.",
                "Monitor only if volume is high or donors report stuck payments."),
            ["EasypayApiLookupFailed"] = new IssueTemplate(
                "EasyPay API lookup failed",
                SiteHealthReportSeverity.Critical,
                "Verification called EasyPay and received an error (often “Payment not found”). Each distinct transaction key may represent a donor payment at risk.",
                "Cross-check transaction keys in Admin → Search Easypay and EasyPay merchant portal."),
            ["PublicDonationIdNotFound"] = new IssueTemplate(
                "Public donation id not found",
                SiteHealthReportSeverity.Info,
                "Invoice or claim flow looked up a public donation GUID that does not exist. Often expired links, typos, or automated probes.",
                "Investigate only if real users report broken invoice links."),
            ["PayedDonation-To-Failed-Payment-Try"] = new IssueTemplate(
                "Paid donation received failed webhook",
                SiteHealthReportSeverity.Info,
                "EasyPay sent a failure notification for a donation already marked paid. Ignored by design.",
                "No action unless counts spike (stale EasyPay notifications)."),
            ["DonationNotFound"] = new IssueTemplate(
                "Donation not found",
                SiteHealthReportSeverity.Warning,
                "Application code referenced a donation id that does not exist.",
                "Review recent admin or payment flows if frequent."),
            ["DonationIsNull"] = new IssueTemplate(
                "Donation is null",
                SiteHealthReportSeverity.Warning,
                "Payment page or webhook handling ran without a resolved donation.",
                "Check for broken payment entry links or session loss."),
            ["SecretNotFound"] = new IssueTemplate(
                "Key Vault secret not found",
                SiteHealthReportSeverity.Critical,
                "Required configuration secret is missing from Key Vault.",
                "Restore or rename the secret and reload tenant settings."),
            ["ServicePrincipal-Secret-NotFound"] = new IssueTemplate(
                "Service principal secret not found",
                SiteHealthReportSeverity.Critical,
                "Azure AD service principal credentials are missing.",
                "Update Key Vault with valid service principal secrets."),
            ["WebhookAmountMismatch"] = new IssueTemplate(
                "Webhook amount mismatch",
                SiteHealthReportSeverity.Critical,
                "Webhook amounts did not match the expected donation total.",
                "Investigate affected payments immediately."),
            ["FailedHttpRequests"] = new IssueTemplate(
                "Failed HTTP requests",
                SiteHealthReportSeverity.Warning,
                "Server returned errors (5xx or other failures excluding 404) on production traffic.",
                "Open Application Insights failed requests for stack traces and affected URLs."),
            ["UnhandledExceptions"] = new IssueTemplate(
                "Unhandled exceptions",
                SiteHealthReportSeverity.Critical,
                "Unhandled errors occurred on production instances.",
                "Review exception details in Application Insights and deploy fixes."),
            ["ExistingSinglePayment-NotFound"] = new IssueTemplate(
                "Existing EasyPay payment not matched",
                SiteHealthReportSeverity.Info,
                "Checkout found stale EasyPay payments that did not match the stored id.",
                "Usually harmless unless Multibanco checkout success rate drops."),
        };

        /// <summary>
        /// Builds a report issue from catalog metadata.
        /// </summary>
        /// <param name="code">Issue code.</param>
        /// <param name="count">Event count.</param>
        /// <param name="distinctIncidents">Optional distinct incident estimate.</param>
        /// <param name="detailLines">Optional breakdown lines.</param>
        /// <returns>Populated issue or generic fallback.</returns>
        public static SiteHealthReportIssue CreateIssue(
            string code,
            long count,
            long? distinctIncidents = null,
            IList<string> detailLines = null)
        {
            if (!Templates.TryGetValue(code, out IssueTemplate template))
            {
                template = new IssueTemplate(
                    code,
                    SiteHealthReportSeverity.Warning,
                    "Review this signal in Application Insights.",
                    "See Documentation/Application-Insights.md for query examples.");
            }

            return new SiteHealthReportIssue
            {
                Code = code,
                Title = template.Title,
                Severity = template.Severity,
                Count = count,
                DistinctIncidents = distinctIncidents,
                ImpactOverview = template.ImpactOverview,
                RecommendedAction = template.RecommendedAction,
                DetailLines = detailLines ?? new List<string>(),
            };
        }

        /// <summary>
        /// Computes overall status from production issues.
        /// </summary>
        /// <param name="issues">Production issues.</param>
        /// <param name="exceptionCount">Exception count.</param>
        /// <param name="failedRequestCount">Failed request count.</param>
        /// <returns>Overall status.</returns>
        public static SiteHealthOverallStatus ComputeOverallStatus(
            IEnumerable<SiteHealthReportIssue> issues,
            long exceptionCount,
            long failedRequestCount)
        {
            foreach (SiteHealthReportIssue issue in issues)
            {
                if (issue.Count > 0 && issue.Severity == SiteHealthReportSeverity.Critical)
                {
                    return SiteHealthOverallStatus.Critical;
                }
            }

            if (exceptionCount > 0)
            {
                return SiteHealthOverallStatus.Critical;
            }

            foreach (SiteHealthReportIssue issue in issues)
            {
                if (issue.Count > 0 && issue.Severity == SiteHealthReportSeverity.Warning)
                {
                    return SiteHealthOverallStatus.Attention;
                }
            }

            if (failedRequestCount > 10)
            {
                return SiteHealthOverallStatus.Attention;
            }

            return SiteHealthOverallStatus.Healthy;
        }

        /// <summary>
        /// Builds a short summary sentence.
        /// </summary>
        /// <param name="status">Overall status.</param>
        /// <param name="criticalCount">Number of critical issue types with events.</param>
        /// <param name="warningCount">Number of warning issue types with events.</param>
        /// <returns>Summary text.</returns>
        public static string BuildSummary(SiteHealthOverallStatus status, int criticalCount, int warningCount)
        {
            return status switch
            {
                SiteHealthOverallStatus.Critical =>
                    criticalCount > 0
                        ? $"Critical: {criticalCount} production signal(s) need immediate attention."
                        : "Critical: unhandled errors detected on production.",
                SiteHealthOverallStatus.Attention =>
                    warningCount > 0
                        ? $"Attention: {warningCount} warning signal(s) should be reviewed."
                        : "Attention: elevated failed requests on production.",
                _ => "Healthy: no significant production issues in this window.",
            };
        }

        private sealed class IssueTemplate
        {
            public IssueTemplate(string title, SiteHealthReportSeverity severity, string impactOverview, string recommendedAction)
            {
                this.Title = title;
                this.Severity = severity;
                this.ImpactOverview = impactOverview;
                this.RecommendedAction = recommendedAction;
            }

            public string Title { get; }

            public SiteHealthReportSeverity Severity { get; }

            public string ImpactOverview { get; }

            public string RecommendedAction { get; }
        }
    }
}
