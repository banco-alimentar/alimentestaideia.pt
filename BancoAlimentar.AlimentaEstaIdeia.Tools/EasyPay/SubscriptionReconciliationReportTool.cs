// -----------------------------------------------------------------------
// <copyright file="SubscriptionReconciliationReportTool.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.EasyPay
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using BancoAlimentar.AlimentaEstaIdeia.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Easypay.Rest.Client.Api;
    using Easypay.Rest.Client.Client;
    using Easypay.Rest.Client.Model;
    using Microsoft.Data.SqlClient;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using Configuration = Easypay.Rest.Client.Client.Configuration;
    using Subscription = BancoAlimentar.AlimentaEstaIdeia.Model.Subscription;

    /// <summary>
    /// Audits the developer and production subscription records against their matching Easypay environments.
    /// The tool performs database and Easypay read operations only.
    /// </summary>
    internal sealed class SubscriptionReconciliationReportTool
    {
        private const int TargetRetries = 3;
        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionReconciliationReportTool"/> class.
        /// </summary>
        /// <param name="configuration">Tool configuration.</param>
        public SubscriptionReconciliationReportTool(IConfiguration configuration)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Creates a report for both the developer and production database/provider pairs.
        /// </summary>
        /// <param name="outputPath">Path for the JSON report.</param>
        public void Execute(string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("A report output path is required.", nameof(outputPath));
            }

            var report = new SubscriptionReconciliationReport
            {
                GeneratedAtUtc = DateTime.UtcNow,
                ReadOnly = true,
                DatabaseWrites = 0,
                EasypayWriteOperations = 0,
                SchemaChangesRequired = 0,
                Environments = new List<SubscriptionEnvironmentReport>
                {
                    this.ProcessEnvironment("Developer"),
                    this.ProcessEnvironment("Production"),
                },
                DatabaseImpact = new DatabaseImpactSummary
                {
                    RetryPolicy = "Eligible active credit-card subscriptions with fewer than three Easypay retries are candidates for a provider PATCH; this report only identifies them.",
                    ExpirationPolicy = "The local model stores a concrete expiration time. The generated Easypay response model does not expose unlimited_payments; compare local/provider expiration fields and decide the policy separately.",
                    FailedCapture = "A verified failed subscription capture may update or create a recurring donation, payment and subscription-donation link. A failure email uses a separate PaymentNotifications row and delivery-state updates. The report counts existing related records; it does not predict unreceived webhooks.",
                    RetrySetting = "Changing Easypay retries does not require a local database write or schema migration.",
                },
            };

            string fullOutputPath = Path.GetFullPath(outputPath);
            string outputDirectory = Path.GetDirectoryName(fullOutputPath);
            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            File.WriteAllText(
                fullOutputPath,
                JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));

            Console.WriteLine($"Subscription reconciliation report: {fullOutputPath}");
            foreach (SubscriptionEnvironmentReport environment in report.Environments)
            {
                Console.WriteLine(
                    $"{environment.Environment}: {environment.Status}; local subscriptions={environment.Summary.LocalSubscriptions}; "
                    + $"provider lookups={environment.Summary.ProviderSubscriptionsRead}; retry updates recommended={environment.Summary.RetryUpdatesRecommended}; "
                    + $"lookup failures={environment.Summary.ProviderLookupFailures}");
            }

            Console.WriteLine("Database writes: 0; Easypay write operations: 0; schema changes required: 0.");
            if (report.Environments.Any(environment => environment.Status != "Succeeded"))
            {
                Environment.ExitCode = 1;
            }
        }

        private SubscriptionEnvironmentReport ProcessEnvironment(string environment)
        {
            var targetConfiguration = this.configuration.GetSection($"SubscriptionReconciliation:{environment}");
            string connectionString = targetConfiguration["ConnectionString"];
            string baseUrl = targetConfiguration["EasyPay:BaseUrl"];
            var report = new SubscriptionEnvironmentReport
            {
                Environment = environment,
                Status = "Failed",
                ProviderEndpoint = environment == "Developer"
                    ? "https://api.test.easypay.pt"
                    : "https://api.prod.easypay.pt",
                Subscriptions = new List<SubscriptionReconciliationRow>(),
                Summary = new SubscriptionEnvironmentSummary(),
            };

            try
            {
                SubscriptionReconciliationEndpointPolicy.Validate(environment, baseUrl);
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException($"SubscriptionReconciliation:{environment}:ConnectionString is not configured.");
                }

                SqlConnectionStringBuilder sqlConnection = new SqlConnectionStringBuilder(connectionString);
                report.DatabaseServer = sqlConnection.DataSource;
                report.DatabaseName = sqlConnection.InitialCatalog;

                var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseSqlServer(connectionString, builder => builder.MigrationsAssembly("BancoAlimentar.AlimentaEstaIdeia.Web"))
                    .Options;
                using var context = new ApplicationDbContext(options);
                List<Subscription> subscriptions = context.Subscriptions
                    .AsNoTracking()
                    .Include(subscription => subscription.InitialDonation)
                        .ThenInclude(donation => donation.PaymentList)
                    .Include(subscription => subscription.InitialDonation)
                        .ThenInclude(donation => donation.ConfirmedPayment)
                    .Include(subscription => subscription.InitialDonation)
                        .ThenInclude(donation => donation.FoodBank)
                    .Include(subscription => subscription.Donations)
                        .ThenInclude(link => link.Donation)
                            .ThenInclude(donation => donation.PaymentList)
                    .Include(subscription => subscription.Donations)
                        .ThenInclude(link => link.Donation)
                            .ThenInclude(donation => donation.ConfirmedPayment)
                    .OrderBy(subscription => subscription.Id)
                    .ToList();
                List<PaymentNotifications> failureNotifications = context.PaymentNotifications
                    .AsNoTracking()
                    .Include(notification => notification.Payment)
                    .Where(notification => notification.NotificationType == NotificationType.SubscriptionPaymentFailureEmail)
                    .ToList();

                report.Summary.LocalSubscriptions = subscriptions.Count;
                report.Summary.ActiveSubscriptions = subscriptions.Count(subscription =>
                    subscription.Status == SubscriptionStatus.Active && !subscription.IsDeleted);
                report.Summary.LocalExpiredSubscriptions = subscriptions.Count(subscription =>
                    subscription.ExpirationTime <= DateTime.UtcNow);
                report.Summary.FailureEmailNotifications = failureNotifications.Count;
                report.Summary.FailureEmailNotificationsSent = failureNotifications.Count(notification =>
                    notification.DeliveryStatus == PaymentNotificationDeliveryStatus.Sent);
                report.Summary.FailureEmailNotificationsFailed = failureNotifications.Count(notification =>
                    notification.DeliveryStatus == PaymentNotificationDeliveryStatus.Failed);
                report.Summary.FailureEmailNotificationsPending = failureNotifications.Count(notification =>
                    notification.DeliveryStatus == PaymentNotificationDeliveryStatus.Pending);

                var paymentNotificationsById = failureNotifications
                    .Where(notification => notification.Payment != null)
                    .GroupBy(notification => notification.Payment.Id)
                    .ToDictionary(group => group.Key, group => group.Count());

                foreach (Subscription subscription in subscriptions)
                {
                    report.Subscriptions.Add(this.ReconcileSubscription(
                        subscription,
                        targetConfiguration,
                        paymentNotificationsById,
                        report.Summary));
                }

                report.Summary.LocalDonations = subscriptions
                    .SelectMany(GetSubscriptionDonations)
                    .Select(donation => donation.Id)
                    .Distinct()
                    .Count();
                report.Status = "Succeeded";
            }
            catch (Exception exception)
            {
                report.Error = exception is InvalidOperationException
                    ? exception.Message
                    : exception.GetType().Name;
                Console.Error.WriteLine($"{environment} reconciliation failed: {report.Error}");
            }

            return report;
        }

        private SubscriptionReconciliationRow ReconcileSubscription(
            Subscription subscription,
            IConfiguration targetConfiguration,
            IReadOnlyDictionary<int, int> failureNotificationsByPaymentId,
            SubscriptionEnvironmentSummary summary)
        {
            List<Donation> donations = GetSubscriptionDonations(subscription)
                .GroupBy(donation => donation.Id)
                .Select(group => group.First())
                .ToList();
            List<BasePayment> payments = donations
                .SelectMany(donation => donation.PaymentList ?? new List<BasePayment>())
                .Concat(donations
                    .Where(donation => donation.ConfirmedPayment != null)
                    .Select(donation => donation.ConfirmedPayment))
                .GroupBy(payment => payment.Id)
                .Select(group => group.First())
                .ToList();
            var row = new SubscriptionReconciliationRow
            {
                LocalSubscriptionId = subscription.Id,
                EasypaySubscriptionId = subscription.EasyPaySubscriptionId,
                LocalStatus = subscription.Status.ToString(),
                IsDeleted = subscription.IsDeleted,
                PaymentType = subscription.SubscriptionType.ToString(),
                Frequency = subscription.Frequency,
                LocalAmount = subscription.InitialDonation?.DonationAmount,
                LocalExpirationTime = subscription.ExpirationTime,
                LocalDonationCount = donations.Count,
                LocalPaidDonationCount = donations.Count(donation => donation.PaymentStatus == PaymentStatus.Payed),
                LocalFailedDonationCount = donations.Count(donation => donation.PaymentStatus == PaymentStatus.ErrorPayment),
                LocalWaitingDonationCount = donations.Count(donation => donation.PaymentStatus == PaymentStatus.WaitingPayment),
                LocalPaymentCount = payments.Count,
                LocalFailedPaymentCount = payments.Count(payment => string.Equals(payment.Status, "Failed", StringComparison.OrdinalIgnoreCase)),
                FailureEmailNotificationCount = payments.Sum(payment =>
                    failureNotificationsByPaymentId.TryGetValue(payment.Id, out int count) ? count : 0),
                ProviderLookupStatus = "NotAttempted",
            };

            if (!Guid.TryParse(subscription.EasyPaySubscriptionId, out Guid easypaySubscriptionId))
            {
                row.ProviderLookupStatus = "InvalidSubscriptionId";
                row.ReconciliationNotes.Add("No valid Easypay subscription UUID is stored locally.");
                summary.ProviderLookupFailures++;
                return row;
            }

            if (subscription.InitialDonation == null)
            {
                row.ProviderLookupStatus = "MissingInitialDonation";
                row.ReconciliationNotes.Add("The local subscription has no initial donation; provider amount comparison and payment-method credential selection are unavailable.");
                summary.ProviderLookupFailures++;
                return row;
            }

            try
            {
                ISubscriptionPaymentApi api = CreateSubscriptionApi(subscription, targetConfiguration);
                SubscriptionIdGet200Response provider = api.SubscriptionIdGet(easypaySubscriptionId);
                if (provider == null)
                {
                    throw new InvalidOperationException("Easypay returned an empty subscription response.");
                }

                row.ProviderLookupStatus = "Succeeded";
                row.ProviderIdMatches = provider.Id == easypaySubscriptionId;
                row.ProviderKeyMatches = !string.IsNullOrWhiteSpace(subscription.TransactionKey)
                    && string.Equals(provider.Key, subscription.TransactionKey, StringComparison.Ordinal);
                row.ProviderAmount = (double)provider.Value;
                row.ProviderRetries = provider.Retries;
                row.ProviderFailover = provider.Failover;
                row.ProviderMethodStatus = provider.Method?.Status?.ToString();
                row.ProviderMethodType = provider.Method?.Type;
                row.ProviderExpirationTime = provider.ExpirationTime;
                row.ProviderMaxCaptures = provider.MaxCaptures;
                row.RetryUpdateRecommended = subscription.Status == SubscriptionStatus.Active
                    && !subscription.IsDeleted
                    && subscription.SubscriptionType == SubscriptionPaymentType.CC
                    && subscription.ExpirationTime > DateTime.UtcNow
                    && provider.Method?.Status == SubscriptionIdGet200ResponseMethod.StatusEnum.Active
                    && provider.Retries < TargetRetries;

                if (row.ProviderIdMatches != true)
                {
                    row.ReconciliationNotes.Add("The returned provider subscription ID does not match the requested ID.");
                }

                if (row.ProviderKeyMatches != true)
                {
                    row.ReconciliationNotes.Add("The provider merchant key differs from the local subscription transaction key.");
                }

                if (row.LocalAmount.HasValue
                    && !PaymentAmountReconciliation.ProviderValueMatchesDonation(row.LocalAmount.Value, (double)provider.Value))
                {
                    row.ReconciliationNotes.Add("The provider subscription amount differs from the local initial donation amount.");
                }

                if (row.RetryUpdateRecommended)
                {
                    summary.RetryUpdatesRecommended++;
                    row.ReconciliationNotes.Add($"The scheduled function would request retries={TargetRetries}; it would preserve failover={provider.Failover}.");
                }

                if (provider.Failover)
                {
                    summary.FailoverEnabledSubscriptions++;
                    row.ReconciliationNotes.Add("Failover is enabled at Easypay; the site does not currently have a verified donor-facing failover recovery flow.");
                }

                if (subscription.ExpirationTime > DateTime.UtcNow)
                {
                    summary.NonExpiredSubscriptions++;
                }

                summary.ProviderSubscriptionsRead++;
            }
            catch (ApiException exception)
            {
                row.ProviderLookupStatus = "Failed";
                row.ProviderError = $"Easypay API error (HTTP {exception.ErrorCode}).";
                row.ReconciliationNotes.Add("Provider state could not be verified; no retry or expiration conclusion is safe.");
                summary.ProviderLookupFailures++;
            }
            catch (Exception exception)
            {
                row.ProviderLookupStatus = "Failed";
                row.ProviderError = exception.GetType().Name;
                row.ReconciliationNotes.Add("Provider state could not be verified; no retry or expiration conclusion is safe.");
                summary.ProviderLookupFailures++;
            }

            return row;
        }

        private static IEnumerable<Donation> GetSubscriptionDonations(Subscription subscription)
        {
            if (subscription.InitialDonation != null)
            {
                yield return subscription.InitialDonation;
            }

            foreach (SubscriptionDonations link in subscription.Donations ?? new List<SubscriptionDonations>())
            {
                if (link.Donation != null)
                {
                    yield return link.Donation;
                }
            }
        }

        private static ISubscriptionPaymentApi CreateSubscriptionApi(
            Subscription subscription,
            IConfiguration targetConfiguration)
        {
            string paymentStrategyValue = targetConfiguration["Tenant:PaymentStrategy"];
            if (!Enum.TryParse(paymentStrategyValue, true, out PaymentStrategy paymentStrategy))
            {
                throw new InvalidOperationException("PaymentStrategy must be configured as SharedPaymentProcessor or IndividualPaymentProcessorPerFoodBank.");
            }

            string accountId;
            string apiKey;
            if (paymentStrategy == PaymentStrategy.SharedPaymentProcessor)
            {
                accountId = targetConfiguration["EasyPay:AccountId"];
                apiKey = targetConfiguration["EasyPay:ApiKey"];
            }
            else if (paymentStrategy == PaymentStrategy.IndividualPaymentProcessorPerFoodBank
                && subscription.InitialDonation?.FoodBank != null)
            {
                int foodBankId = subscription.InitialDonation.FoodBank.Id;
                accountId = targetConfiguration[$"EasyPay:AccountId-{foodBankId}"];
                apiKey = targetConfiguration[$"EasyPay:ApiKey-{foodBankId}"];
            }
            else
            {
                throw new InvalidOperationException("A food bank is required to select the per-food-bank Easypay credentials.");
            }

            if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("Read-only Easypay account credentials are not configured for this subscription.");
            }

            var apiConfiguration = new Configuration
            {
                BasePath = targetConfiguration["EasyPay:BaseUrl"].TrimEnd('/') + "/2.0",
            };
            apiConfiguration.ApiKey.Add("AccountId", accountId);
            apiConfiguration.ApiKey.Add("ApiKey", apiKey);
            apiConfiguration.DefaultHeaders.Add("Content-Type", "application/json");
            return new SubscriptionPaymentApi(apiConfiguration);
        }
    }

    /// <summary>
    /// Validates that a report's provider endpoint belongs to the provider environment paired with the database.
    /// </summary>
    public static class SubscriptionReconciliationEndpointPolicy
    {
        /// <summary>
        /// Ensures a developer database uses Easypay test and production uses Easypay production.
        /// </summary>
        /// <param name="environment">Developer or Production.</param>
        /// <param name="baseUrl">Configured Easypay API base URL.</param>
        public static void Validate(string environment, string baseUrl)
        {
            string expectedHost = environment switch
            {
                "Developer" => "api.test.easypay.pt",
                "Production" => "api.prod.easypay.pt",
                _ => throw new ArgumentException("Environment must be Developer or Production.", nameof(environment)),
            };

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri endpoint)
                || !string.Equals(endpoint.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(endpoint.Host, expectedHost, StringComparison.OrdinalIgnoreCase)
                || !endpoint.IsDefaultPort
                || !string.IsNullOrEmpty(endpoint.UserInfo)
                || !string.IsNullOrEmpty(endpoint.Query)
                || !string.IsNullOrEmpty(endpoint.Fragment)
                || (endpoint.AbsolutePath != "/" && endpoint.AbsolutePath != string.Empty))
            {
                throw new InvalidOperationException(
                    $"{environment} must use the HTTPS Easypay endpoint https://{expectedHost} with no path.");
            }
        }
    }

    internal sealed class SubscriptionReconciliationReport
    {
        public DateTime GeneratedAtUtc { get; set; }

        public bool ReadOnly { get; set; }

        public int DatabaseWrites { get; set; }

        public int EasypayWriteOperations { get; set; }

        public int SchemaChangesRequired { get; set; }

        public List<SubscriptionEnvironmentReport> Environments { get; set; }

        public DatabaseImpactSummary DatabaseImpact { get; set; }
    }

    internal sealed class DatabaseImpactSummary
    {
        public string RetryPolicy { get; set; }

        public string ExpirationPolicy { get; set; }

        public string FailedCapture { get; set; }

        public string RetrySetting { get; set; }
    }

    internal sealed class SubscriptionEnvironmentReport
    {
        public string Environment { get; set; }

        public string Status { get; set; }

        public string DatabaseServer { get; set; }

        public string DatabaseName { get; set; }

        public string ProviderEndpoint { get; set; }

        public string Error { get; set; }

        public SubscriptionEnvironmentSummary Summary { get; set; }

        public List<SubscriptionReconciliationRow> Subscriptions { get; set; }
    }

    internal sealed class SubscriptionEnvironmentSummary
    {
        public int LocalSubscriptions { get; set; }

        public int ActiveSubscriptions { get; set; }

        public int LocalExpiredSubscriptions { get; set; }

        public int NonExpiredSubscriptions { get; set; }

        public int LocalDonations { get; set; }

        public int ProviderSubscriptionsRead { get; set; }

        public int ProviderLookupFailures { get; set; }

        public int RetryUpdatesRecommended { get; set; }

        public int FailoverEnabledSubscriptions { get; set; }

        public int FailureEmailNotifications { get; set; }

        public int FailureEmailNotificationsSent { get; set; }

        public int FailureEmailNotificationsFailed { get; set; }

        public int FailureEmailNotificationsPending { get; set; }
    }

    internal sealed class SubscriptionReconciliationRow
    {
        public int LocalSubscriptionId { get; set; }

        public string EasypaySubscriptionId { get; set; }

        public string LocalStatus { get; set; }

        public bool IsDeleted { get; set; }

        public string PaymentType { get; set; }

        public string Frequency { get; set; }

        public double? LocalAmount { get; set; }

        public DateTime LocalExpirationTime { get; set; }

        public string ProviderLookupStatus { get; set; }

        public bool? ProviderIdMatches { get; set; }

        public bool? ProviderKeyMatches { get; set; }

        public double? ProviderAmount { get; set; }

        public int? ProviderRetries { get; set; }

        public bool? ProviderFailover { get; set; }

        public string ProviderMethodStatus { get; set; }

        public string ProviderMethodType { get; set; }

        public string ProviderExpirationTime { get; set; }

        public int? ProviderMaxCaptures { get; set; }

        public bool RetryUpdateRecommended { get; set; }

        public int LocalDonationCount { get; set; }

        public int LocalPaidDonationCount { get; set; }

        public int LocalFailedDonationCount { get; set; }

        public int LocalWaitingDonationCount { get; set; }

        public int LocalPaymentCount { get; set; }

        public int LocalFailedPaymentCount { get; set; }

        public int FailureEmailNotificationCount { get; set; }

        public string ProviderError { get; set; }

        public List<string> ReconciliationNotes { get; } = new List<string>();
    }
}
