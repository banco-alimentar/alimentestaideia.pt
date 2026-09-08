// -----------------------------------------------------------------------
// <copyright file="Details.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage.Subscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Services;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Features;
    using Easypay.Rest.Client.Api;
    using Easypay.Rest.Client.Client;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.FeatureManagement.Mvc;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using EasyPayTransaction = Easypay.Rest.Client.Model.SubscriptionIdGet200ResponseTransactionsInner;

    /// <summary>
    /// Display the detail for a subscription.
    /// </summary>
    [FeatureGate(DevelopingFeatureFlags.SubscriptionAdmin)]
    public class DetailsModel : PageModel
    {
        private readonly IUnitOfWork context;
        private readonly UserManager<WebUser> userManager;
        private readonly TelemetryClient telemetryClient;
        private readonly EasyPayBuilder easyPayBuilder;
        private readonly IConfiguration configuration;
        private readonly ILogger<DetailsModel> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DetailsModel"/> class.
        /// <param name="context">UnitOfWork.</param>
        /// <param name="userManager">User Manager.</param>
        /// <param name="telemetryClient">TelemetryClient.</param>
        /// <param name="easyPayBuilder">Easypay API builder.</param>
        /// <param name="configuration">Application configuration.</param>
        /// <param name="logger">Page logger.</param>
        /// </summary>
        public DetailsModel(
            IUnitOfWork context,
            UserManager<WebUser> userManager,
            TelemetryClient telemetryClient,
            EasyPayBuilder easyPayBuilder,
            IConfiguration configuration,
            ILogger<DetailsModel> logger)
        {
            this.context = context;
            this.userManager = userManager;
            this.telemetryClient = telemetryClient;
            this.easyPayBuilder = easyPayBuilder;
            this.configuration = configuration;
            this.logger = logger;
        }

        /// <summary>
        /// Gets or sets the actual Subscription.
        /// </summary>
        [BindProperty]
        public Subscription Subscription { get; set; }

        /// <summary>
        /// Gets or sets the list of donations part of the Subscription.
        /// </summary>
        [BindProperty]
        public List<Donation> Donations { get; set; }

        /// <summary>
        /// Gets the total amount of donations marked as paid for the subscription.
        /// </summary>
        public double PaidDonationTotal { get; private set; }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <param name="publicId">Subscription public id.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnGetAsync(Guid? publicId)
        {
            if (publicId == null)
            {
                return NotFound();
            }

            Subscription = context.SubscriptionRepository.GetSubscriptionByPublicId(publicId.Value);

            if (Subscription == null)
            {
                return NotFound();
            }

            var user = await userManager.GetUserAsync(User);
            if (!(user != null && userManager != null && user.Id == Subscription.User?.Id))
            {
                this.telemetryClient.TrackEvent(
                    "WhenDeletingSubscripionUserIsNotValidGetDetails",
                    new Dictionary<string, string>()
                    {
                                        { "CurrentLoggedUser", user?.Id },
                                        { "SubcriptionId", Subscription?.Id.ToString() },
                                        { "SubscriptionUser", Subscription.User?.Id },
                    });
                return NotFound();
            }

            PaidDonationTotal = context.SubscriptionRepository
                .GetDonationsForSubscription(Subscription.Id)
                .Where(donation => donation.PaymentStatus == PaymentStatus.Payed)
                .Sum(donation => donation.DonationAmount);

            return Page();
        }

        /// <summary>
        /// Return the donations associated to the subscription.
        /// </summary>
        /// <param name="id">Subscription id.</param>
        /// <returns>Json.</returns>
        public async Task<IActionResult> OnGetDataTableDataAsync(int id)
        {
            var user = await userManager.GetUserAsync(User);
            var subscription = context.SubscriptionRepository.GetSubscriptionById(id);
            if (user == null)
            {
                return Unauthorized();
            }

            if (subscription == null || subscription.User?.Id != user.Id)
            {
                return NotFound();
            }

            var donations = context.SubscriptionRepository.GetDonationsForSubscription(id);
            var providerResult = await LoadEasyPayTransactionsAsync(subscription);

            JArray list = new JArray();
            int count = 1;
            foreach (var item in donations)
            {
                EasyPayTransaction transaction = FindMatchingTransaction(item, subscription, providerResult.Transactions);
                string easyPayStatus = transaction == null
                    ? null
                    : GetEasyPayPaymentStatus(transaction).ToString();
                string consistency = providerResult.IsAvailable
                    ? transaction == null
                        ? "NotFound"
                        : item.PaymentStatus.ToString() == easyPayStatus ? "Match" : "Mismatch"
                    : "Unavailable";

                JObject obj = new JObject();
                obj.Add("Id", count);
                obj.Add("Created", FormatSubscriptionDateForJson(item.DonationDate));
                obj.Add("Amount", item.DonationAmount);
                obj.Add("FoodBank", item.FoodBank != null ? item.FoodBank.Name : string.Empty);
                obj.Add("Payment", item.PaymentStatus.ToString());
                obj.Add("EasyPayPaymentStatus", easyPayStatus);
                obj.Add("Consistency", consistency);
                obj.Add("PublicId", item.PublicId);
                list.Add(obj);
                count++;
            }

            JObject response = new JObject
            {
                ["Donations"] = list,
                ["Summary"] = new JObject
                {
                    ["ProviderAvailable"] = providerResult.IsAvailable,
                    ["ProviderTransactionCount"] = providerResult.Transactions.Count,
                    ["MatchedCount"] = list.Count(item => (string)item["Consistency"] == "Match"),
                    ["MismatchCount"] = list.Count(item => (string)item["Consistency"] == "Mismatch"),
                    ["NotFoundCount"] = list.Count(item => (string)item["Consistency"] == "NotFound"),
                },
            };

            return new ContentResult()
            {
                Content = JsonConvert.SerializeObject(response),
                ContentType = "application/json",
                StatusCode = 200,
            };
        }

        private static EasyPayTransaction FindMatchingTransaction(
            Donation donation,
            Subscription subscription,
            IReadOnlyList<EasyPayTransaction> transactions)
        {
            var localPaymentIds = donation.PaymentList?
                .OfType<EasyPayBaseClass>()
                .Select(payment => payment.EasyPayPaymentId)
                .Where(paymentId => !string.IsNullOrWhiteSpace(paymentId))
                .Where(paymentId => !string.Equals(
                    paymentId,
                    subscription.EasyPaySubscriptionId,
                    StringComparison.OrdinalIgnoreCase))
                .ToList() ?? new List<string>();

            EasyPayTransaction match = transactions.FirstOrDefault(transaction =>
                localPaymentIds.Any(paymentId => string.Equals(paymentId, transaction.Id, StringComparison.OrdinalIgnoreCase)));
            if (match != null)
            {
                return match;
            }

            var candidates = transactions
                .Where(transaction => ValuesMatchDonation(transaction, donation))
                .ToList();
            return candidates.Count == 1 ? candidates[0] : null;
        }

        private static bool ValuesMatchDonation(EasyPayTransaction transaction, Donation donation)
        {
            if (transaction?.Values == null
                || !PaymentAmountReconciliation.ProviderValueMatchesDonation(
                    (double)transaction.Values.Requested,
                    donation.DonationAmount))
            {
                return false;
            }

            if (!DateTime.TryParse(
                transaction.Date ?? transaction.CreatedAt,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out DateTime providerDate))
            {
                return false;
            }

            return providerDate.Date == donation.DonationDate.ToUniversalTime().Date;
        }

        private static PaymentStatus GetEasyPayPaymentStatus(EasyPayTransaction transaction)
        {
            bool isPaid = transaction?.Values != null
                && transaction.Values.Paid > 0
                && PaymentAmountReconciliation.ProviderValueMatchesDonation(
                    (double)transaction.Values.Requested,
                    (double)transaction.Values.Paid);
            return isPaid ? PaymentStatus.Payed : PaymentStatus.WaitingPayment;
        }

        private static string FormatSubscriptionDateForJson(DateTime value)
        {
            if (value.Year < 1900)
            {
                return null;
            }

            if (value.Kind == DateTimeKind.Unspecified)
            {
                value = DateTime.SpecifyKind(value, DateTimeKind.Utc);
            }

            return value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
        }

        private static bool IsConfiguredValue(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && !value.StartsWith("#{", StringComparison.Ordinal);
        }

        private async Task<EasyPayLookupResult> LoadEasyPayTransactionsAsync(Subscription subscription)
        {
            if (!Guid.TryParse(subscription.EasyPaySubscriptionId, out Guid easyPaySubscriptionId)
                || !HasEasyPayConfiguration())
            {
                return EasyPayLookupResult.Unavailable();
            }

            try
            {
                var response = await easyPayBuilder.GetSubscriptionPaymentApi()
                    .SubscriptionIdGetAsync(easyPaySubscriptionId);
                return EasyPayLookupResult.Available(response?.Transactions?.ToList() ?? new List<EasyPayTransaction>());
            }
            catch (ApiException exception)
            {
                this.logger.LogWarning(
                    exception,
                    "Easypay donation status lookup failed for local subscription {SubscriptionId} and Easypay subscription {EasyPaySubscriptionId}.",
                    subscription.Id,
                    subscription.EasyPaySubscriptionId);
                return EasyPayLookupResult.Unavailable();
            }
            catch (Exception exception)
            {
                this.logger.LogError(
                    exception,
                    "Unexpected Easypay donation status lookup failure for local subscription {SubscriptionId} and Easypay subscription {EasyPaySubscriptionId}.",
                    subscription.Id,
                    subscription.EasyPaySubscriptionId);
                return EasyPayLookupResult.Unavailable();
            }
        }

        private bool HasEasyPayConfiguration()
        {
            bool hasSharedCredentials = IsConfiguredValue(this.configuration["Easypay:AccountId"])
                && IsConfiguredValue(this.configuration["Easypay:ApiKey"]);
            bool hasFoodBankCredentials = this.configuration.AsEnumerable().Any(setting =>
                        setting.Key.StartsWith("Easypay:AccountId-", StringComparison.OrdinalIgnoreCase)
                        && IsConfiguredValue(setting.Value))
                && this.configuration.AsEnumerable().Any(setting =>
                        setting.Key.StartsWith("Easypay:ApiKey-", StringComparison.OrdinalIgnoreCase)
                        && IsConfiguredValue(setting.Value));

            return IsConfiguredValue(this.configuration["Easypay:BaseUrl"])
                && (hasSharedCredentials || hasFoodBankCredentials);
        }

        private sealed class EasyPayLookupResult
        {
            private EasyPayLookupResult(bool isAvailable, IReadOnlyList<EasyPayTransaction> transactions)
            {
                this.IsAvailable = isAvailable;
                this.Transactions = transactions;
            }

            public bool IsAvailable { get; }

            public IReadOnlyList<EasyPayTransaction> Transactions { get; }

            public static EasyPayLookupResult Available(IReadOnlyList<EasyPayTransaction> transactions)
            {
                return new EasyPayLookupResult(true, transactions);
            }

            public static EasyPayLookupResult Unavailable()
            {
                return new EasyPayLookupResult(false, Array.Empty<EasyPayTransaction>());
            }
        }
    }
}
