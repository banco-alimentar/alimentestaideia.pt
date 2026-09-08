// -----------------------------------------------------------------------
// <copyright file="Details.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Subscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Services;
    using Easypay.Rest.Client.Api;
    using Easypay.Rest.Client.Client;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using EasyPaySubscriptionResponse = Easypay.Rest.Client.Model.SubscriptionIdGet200Response;
    using EasyPayTransaction = Easypay.Rest.Client.Model.SubscriptionIdGet200ResponseTransactionsInner;

    /// <summary>
    /// Subscription details model.
    /// </summary>
    public class DetailsModel : PageModel
    {
        private readonly IUnitOfWork context;
        private readonly EasyPayBuilder easyPayBuilder;
        private readonly IConfiguration configuration;
        private readonly IStringLocalizer<AdminSharedResources> localizer;
        private readonly ILogger<DetailsModel> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DetailsModel"/> class.
        /// </summary>
        /// <param name="context">Unit of work.</param>
        /// <param name="easyPayBuilder">Easypay API builder.</param>
        /// <param name="configuration">Application configuration.</param>
        /// <param name="localizer">Admin resource localizer.</param>
        /// <param name="logger">Page logger.</param>
        public DetailsModel(
            IUnitOfWork context,
            EasyPayBuilder easyPayBuilder,
            IConfiguration configuration,
            IStringLocalizer<AdminSharedResources> localizer,
            ILogger<DetailsModel> logger)
        {
            this.context = context;
            this.easyPayBuilder = easyPayBuilder;
            this.configuration = configuration;
            this.localizer = localizer;
            this.logger = logger;
        }

        /// <summary>
        /// Gets or sets the subscription.
        /// </summary>
        public Subscription Subscription { get; set; }

        /// <summary>
        /// Gets the donations linked to the subscription.
        /// </summary>
        public IList<Donation> Donations { get; private set; } = new List<Donation>();

        /// <summary>
        /// Gets the total amount of donations marked as paid for the subscription.
        /// </summary>
        public double PaidDonationTotal => this.Donations
            .Where(donation => donation.PaymentStatus == PaymentStatus.Payed)
            .Sum(donation => donation.DonationAmount);

        /// <summary>
        /// Gets donation totals grouped by payment status.
        /// </summary>
        public IList<DonationPaymentStatusSummary> DonationsByPaymentStatus { get; private set; } =
            new List<DonationPaymentStatusSummary>();

        /// <summary>
        /// Gets the local and Easypay donation totals grouped by payment status.
        /// </summary>
        public IList<PaymentStatusTotalsComparison> PaymentStatusTotalsComparisons { get; private set; } =
            new List<PaymentStatusTotalsComparison>();

        /// <summary>
        /// Gets the date of the most recent linked donation, if any.
        /// </summary>
        public DateTime? LastDonationDate { get; private set; }

        /// <summary>
        /// Gets the estimated date of the next recurring donation.
        /// </summary>
        public DateTime? NextExpectedDonationDate { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the next donation date could be estimated.
        /// </summary>
        public bool HasNextExpectedDonationDate => NextExpectedDonationDate.HasValue;

        /// <summary>
        /// Gets the subscription returned by Easypay.
        /// </summary>
        public EasyPaySubscriptionResponse EasyPaySubscription { get; private set; }

        /// <summary>
        /// Gets the payments returned by Easypay for this subscription.
        /// </summary>
        public IList<EasyPayPaymentDetails> EasyPayPayments { get; private set; } =
            new List<EasyPayPaymentDetails>();

        /// <summary>
        /// Gets the number of transactions returned by Easypay.
        /// </summary>
        public int EasyPayTransactionCount => this.EasyPayPayments.Count;

        /// <summary>
        /// Gets the number of donations linked to the local subscription.
        /// </summary>
        public int LocalDonationCount => this.Donations.Count;

        /// <summary>
        /// Gets the total amount marked as paid by Easypay.
        /// </summary>
        public decimal EasyPayPaidTotal => this.EasyPayPayments.Sum(payment => payment.Transaction?.Values?.Paid ?? 0m);

        /// <summary>
        /// Gets a value indicating whether the Easypay transaction count differs from the local donation count.
        /// </summary>
        public bool HasEasyPayDonationCountMismatch => this.EasyPayTransactionCount != this.LocalDonationCount;

        /// <summary>
        /// Gets the Easypay lookup error, if the provider data could not be loaded.
        /// </summary>
        public string EasyPayError { get; private set; }

        /// <summary>
        /// Gets a value indicating whether Easypay credentials are available for this request.
        /// </summary>
        public bool IsEasyPayConfigured { get; private set; }

        /// <summary>
        /// Gets the Easypay payment IDs associated with a donation.
        /// </summary>
        /// <param name="donation">The donation.</param>
        /// <returns>A comma-separated list of Easypay payment IDs.</returns>
        public string GetEasyPayPaymentIds(Donation donation)
        {
            return string.Join(", ", this.GetEasyPayPaymentIdList(donation));
        }

        /// <summary>
        /// Gets the Easypay payment IDs associated with a donation.
        /// </summary>
        /// <param name="donation">The donation.</param>
        /// <returns>The Easypay payment IDs.</returns>
        public IList<string> GetEasyPayPaymentIdList(Donation donation)
        {
            return donation?.PaymentList?
                .OfType<EasyPayBaseClass>()
                .Select(payment => payment.EasyPayPaymentId)
                .Where(paymentId => !string.IsNullOrWhiteSpace(paymentId))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
                ?? new List<string>();
        }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <param name="id">The subscription id.</param>
        /// <returns>A <see cref="IActionResult"/> representing the result of the operation.</returns>
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Subscription = this.context.SubscriptionRepository.GetSubscriptionById(id.Value);
            if (Subscription == null)
            {
                return NotFound();
            }

            Donations = this.context.SubscriptionRepository.GetDonationsForSubscription(id.Value);

            DonationsByPaymentStatus = Donations
                .GroupBy(donation => donation.PaymentStatus)
                .Select(group => new DonationPaymentStatusSummary
                {
                    PaymentStatus = group.Key,
                    DonationCount = group.Count(),
                    TotalAmount = group.Sum(donation => donation.DonationAmount),
                })
                .OrderBy(summary => summary.PaymentStatus)
                .ToList();

            this.ComputeNextExpectedDonation();
            await this.LoadEasyPayDetailsAsync();
            this.BuildPaymentStatusTotalsComparisons();

            return Page();
        }

        private PaymentStatus GetEasyPayPaymentStatus(EasyPayTransaction transaction)
        {
            if (transaction?.Values == null)
            {
                return PaymentStatus.WaitingPayment;
            }

            bool isPaid = transaction.Values.Paid > 0
                && PaymentAmountReconciliation.ProviderValueMatchesDonation(
                    (double)transaction.Values.Requested,
                    (double)transaction.Values.Paid);
            return isPaid ? PaymentStatus.Payed : PaymentStatus.WaitingPayment;
        }

        private async Task LoadEasyPayDetailsAsync()
        {
            if (string.IsNullOrWhiteSpace(this.Subscription.EasyPaySubscriptionId))
            {
                this.EasyPayError = this.localizer["EasyPayNoSubscriptionId"].Value;
                return;
            }

            if (!Guid.TryParse(this.Subscription.EasyPaySubscriptionId, out Guid easyPaySubscriptionId))
            {
                this.EasyPayError = this.localizer[
                    "EasyPayInvalidSubscriptionId",
                    this.Subscription.EasyPaySubscriptionId].Value;
                return;
            }

            if (!this.HasEasyPayConfiguration())
            {
                this.EasyPayError = this.localizer["EasyPayNotConfigured"].Value;
                return;
            }

            this.IsEasyPayConfigured = true;

            try
            {
                ISubscriptionPaymentApi subscriptionApi = this.easyPayBuilder.GetSubscriptionPaymentApi();
                this.EasyPaySubscription = await subscriptionApi.SubscriptionIdGetAsync(easyPaySubscriptionId);

                if (this.EasyPaySubscription == null)
                {
                    this.EasyPayError = this.localizer["EasyPayEmptySubscriptionResponse"].Value;
                    return;
                }

                this.LoadEasyPayPayments();
            }
            catch (ApiException exception)
            {
                this.SetEasyPayError(exception);
            }
            catch (Exception exception)
            {
                this.logger.LogError(
                    exception,
                    "Failed to load Easypay details for local subscription {SubscriptionId} and Easypay subscription {EasyPaySubscriptionId}.",
                    this.Subscription.Id,
                    this.Subscription.EasyPaySubscriptionId);
                this.EasyPayError = this.localizer["EasyPayLookupFailed", exception.Message].Value;
            }
        }

        private void LoadEasyPayPayments()
        {
            if (this.EasyPaySubscription.Transactions == null
                || this.EasyPaySubscription.Transactions.Count == 0)
            {
                return;
            }

            foreach (EasyPayTransaction transaction in this.EasyPaySubscription.Transactions)
            {
                this.EasyPayPayments.Add(new EasyPayPaymentDetails
                {
                    Transaction = transaction,
                });
            }
        }

        private bool HasEasyPayConfiguration()
        {
            string baseUrl = this.configuration["Easypay:BaseUrl"];
            string accountId = this.configuration["Easypay:AccountId"];
            string apiKey = this.configuration["Easypay:ApiKey"];

            bool hasSharedCredentials = IsConfiguredValue(accountId) && IsConfiguredValue(apiKey);
            bool hasFoodBankCredentials = this.configuration.AsEnumerable().Any(setting =>
                setting.Key.StartsWith("Easypay:AccountId-", StringComparison.OrdinalIgnoreCase)
                && IsConfiguredValue(setting.Value))
                && this.configuration.AsEnumerable().Any(setting =>
                    setting.Key.StartsWith("Easypay:ApiKey-", StringComparison.OrdinalIgnoreCase)
                    && IsConfiguredValue(setting.Value));

            return IsConfiguredValue(baseUrl) && (hasSharedCredentials || hasFoodBankCredentials);
        }

        private void SetEasyPayError(ApiException exception)
        {
            this.logger.LogError(
                exception,
                "Easypay API lookup failed for local subscription {SubscriptionId} and Easypay subscription {EasyPaySubscriptionId}.",
                this.Subscription.Id,
                this.Subscription.EasyPaySubscriptionId);
            this.EasyPayError = this.localizer["EasyPayLookupFailed", exception.Message].Value;
        }

        private bool IsConfiguredValue(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && !value.StartsWith("#{", StringComparison.Ordinal);
        }

        private void ComputeNextExpectedDonation()
        {
            if (Subscription.IsDeleted
                || Subscription.Status != SubscriptionStatus.Active
                || string.IsNullOrWhiteSpace(Subscription.Frequency))
            {
                return;
            }

            Donation lastDonation = Donations
                .OrderByDescending(donation => donation.DonationDate)
                .FirstOrDefault();

            DateTime referenceDate = lastDonation?.DonationDate ?? Subscription.StartTime;
            LastDonationDate = lastDonation?.DonationDate;

            DateTime? nextDate = SubscriptionFrequencyHelper.AddFrequency(referenceDate, Subscription.Frequency);
            if (!nextDate.HasValue || nextDate.Value > Subscription.ExpirationTime)
            {
                return;
            }

            NextExpectedDonationDate = nextDate;
        }

        private void BuildPaymentStatusTotalsComparisons()
        {
            if (this.EasyPaySubscription == null)
            {
                return;
            }

            Dictionary<PaymentStatus, DonationPaymentStatusSummary> localSummaries = this.DonationsByPaymentStatus
                .ToDictionary(summary => summary.PaymentStatus);
            Dictionary<PaymentStatus, EasyPayPaymentStatusSummary> easyPaySummaries = this.EasyPayPayments
                .Where(payment => payment.Transaction != null)
                .GroupBy(payment => this.GetEasyPayPaymentStatus(payment.Transaction))
                .ToDictionary(
                    group => group.Key,
                    group => new EasyPayPaymentStatusSummary
                    {
                        PaymentStatus = group.Key,
                        PaymentCount = group.Count(),
                        TotalPaidAmount = group.Sum(payment => (double)(payment.Transaction.Values?.Paid ?? 0m)),
                    });

            this.PaymentStatusTotalsComparisons = Enum.GetValues<PaymentStatus>()
                .Where(status => localSummaries.ContainsKey(status) || easyPaySummaries.ContainsKey(status))
                .Select(status => new PaymentStatusTotalsComparison
                {
                    PaymentStatus = status,
                    LocalDonationCount = localSummaries.TryGetValue(status, out DonationPaymentStatusSummary localSummary)
                        ? localSummary.DonationCount
                        : 0,
                    LocalTotalAmount = localSummaries.TryGetValue(status, out localSummary)
                        ? localSummary.TotalAmount
                        : 0,
                    EasyPayPaymentCount = easyPaySummaries.TryGetValue(status, out EasyPayPaymentStatusSummary easyPaySummary)
                        ? easyPaySummary.PaymentCount
                        : 0,
                    EasyPayTotalPaidAmount = easyPaySummaries.TryGetValue(status, out easyPaySummary)
                        ? easyPaySummary.TotalPaidAmount
                        : 0,
                })
                .ToList();
        }

        /// <summary>
        /// Donation totals for a payment status.
        /// </summary>
        public sealed class DonationPaymentStatusSummary
        {
            /// <summary>
            /// Gets or sets the payment status.
            /// </summary>
            public PaymentStatus PaymentStatus { get; set; }

            /// <summary>
            /// Gets or sets the number of donations.
            /// </summary>
            public int DonationCount { get; set; }

            /// <summary>
            /// Gets or sets the total donation amount.
            /// </summary>
            public double TotalAmount { get; set; }
        }

        /// <summary>
        /// Easypay payment totals for a payment status.
        /// </summary>
        public sealed class EasyPayPaymentStatusSummary
        {
            /// <summary>
            /// Gets or sets the payment status.
            /// </summary>
            public PaymentStatus PaymentStatus { get; set; }

            /// <summary>
            /// Gets or sets the number of Easypay payments.
            /// </summary>
            public int PaymentCount { get; set; }

            /// <summary>
            /// Gets or sets the amount paid according to Easypay.
            /// </summary>
            public double TotalPaidAmount { get; set; }
        }

        /// <summary>
        /// Compares local donation totals with Easypay payment totals for a payment status.
        /// </summary>
        public sealed class PaymentStatusTotalsComparison
        {
            /// <summary>
            /// Gets or sets the payment status.
            /// </summary>
            public PaymentStatus PaymentStatus { get; set; }

            /// <summary>
            /// Gets or sets the number of local donations.
            /// </summary>
            public int LocalDonationCount { get; set; }

            /// <summary>
            /// Gets or sets the local donation total.
            /// </summary>
            public double LocalTotalAmount { get; set; }

            /// <summary>
            /// Gets or sets the number of Easypay payments.
            /// </summary>
            public int EasyPayPaymentCount { get; set; }

            /// <summary>
            /// Gets or sets the Easypay paid total.
            /// </summary>
            public double EasyPayTotalPaidAmount { get; set; }

            /// <summary>
            /// Gets a value indicating whether the local and Easypay totals differ.
            /// </summary>
            public bool HasMismatch => this.LocalDonationCount != this.EasyPayPaymentCount
                || Math.Abs(this.LocalTotalAmount - this.EasyPayTotalPaidAmount)
                    > PaymentAmountReconciliation.DefaultTolerance;
        }

        /// <summary>
        /// Easypay payment summary and detail response.
        /// </summary>
        public sealed class EasyPayPaymentDetails
        {
            /// <summary>
            /// Gets or sets the transaction returned by the Easypay subscription endpoint.
            /// </summary>
            public EasyPayTransaction Transaction { get; set; }
        }
    }
}
