// -----------------------------------------------------------------------
// <copyright file="Details.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Donations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Services;
    using Easypay.Rest.Client.Api;
    using Easypay.Rest.Client.Client;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using EasyPayPaymentResponse = Easypay.Rest.Client.Model.InlineObject9;

    /// <summary>
    /// Details on the donation.
    /// </summary>
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext context;
        private readonly EasyPayBuilder easyPayBuilder;
        private readonly IConfiguration configuration;
        private readonly IStringLocalizer<AdminSharedResources> localizer;
        private readonly ILogger<DetailsModel> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DetailsModel"/> class.
        /// </summary>
        /// <param name="context">Application Db Context.</param>
        /// <param name="easyPayBuilder">Easypay API builder.</param>
        /// <param name="configuration">Application configuration.</param>
        /// <param name="localizer">Admin resource localizer.</param>
        /// <param name="logger">Page logger.</param>
        public DetailsModel(
            ApplicationDbContext context,
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
        /// Gets or sets the current donation.
        /// </summary>
        public Donation Donation { get; set; }

        /// <summary>
        /// Gets the payments related to the donation.
        /// </summary>
        public IList<BasePayment> Payments { get; private set; } = new List<BasePayment>();

        /// <summary>
        /// Gets local Easypay payments together with their provider details.
        /// </summary>
        public IList<EasyPayPaymentDetails> EasyPayPaymentLookups { get; private set; } =
            new List<EasyPayPaymentDetails>();

        /// <summary>
        /// Gets the general Easypay lookup error, if configuration is unavailable.
        /// </summary>
        public string EasyPayError { get; private set; }

        /// <summary>
        /// Gets the invoices related to the donation.
        /// </summary>
        public IList<Invoice> Invoices { get; private set; } = new List<Invoice>();

        /// <summary>
        /// Gets the subscription linked to this donation, if any.
        /// </summary>
        public Subscription RelatedSubscription { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this donation is the initial donation of its subscription.
        /// </summary>
        public bool IsInitialSubscriptionDonation { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this donation belongs to a subscription.
        /// </summary>
        public bool IsSubscriptionDonation => RelatedSubscription != null;

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <param name="id">The donation id.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Donation = await context.Donations
                .AsNoTracking()
                .Include(donation => donation.ReferralEntity)
                .Include(donation => donation.Campaign)
                .Include(donation => donation.FoodBank)
                .Include(donation => donation.User)
                .Include(donation => donation.ConfirmedPayment)
                .Include(donation => donation.PaymentList)
                .FirstOrDefaultAsync(donation => donation.Id == id);

            if (Donation == null)
            {
                return NotFound();
            }

            Payments = Donation.PaymentList?
                .OrderByDescending(payment => payment.Created)
                .ThenByDescending(payment => payment.Id)
                .ToList() ?? new List<BasePayment>();

            await this.LoadEasyPayPaymentDetailsAsync();

            Invoices = await context.Invoices
                .AsNoTracking()
                .Where(invoice => EF.Property<int?>(invoice, "DonationId") == id)
                .OrderByDescending(invoice => invoice.Created)
                .ToListAsync();

            await this.LoadSubscriptionInfoAsync(id.Value);

            return Page();
        }

        /// <summary>
        /// Gets the payment type label for display.
        /// </summary>
        /// <param name="payment">The payment.</param>
        /// <returns>The payment type name.</returns>
        public string GetPaymentTypeName(BasePayment payment)
        {
            return payment switch
            {
                MultiBankPayment => "MultiBank",
                CreditCardPayment => "CreditCard",
                MBWayPayment => "MBWay",
                PayPalPayment => "PayPal",
                _ => payment.GetType().Name,
            };
        }

        /// <summary>
        /// Gets the Easypay payment identifier when available.
        /// </summary>
        /// <param name="payment">The payment.</param>
        /// <returns>The Easypay payment id, if any.</returns>
        public string GetEasyPayPaymentId(BasePayment payment)
        {
            if (payment is EasyPayBaseClass easyPayPayment)
            {
                return easyPayPayment.EasyPayPaymentId;
            }

            return null;
        }

        /// <summary>
        /// Gets a value indicating whether the payment is the confirmed payment for the donation.
        /// </summary>
        /// <param name="payment">The payment.</param>
        /// <returns>True when this is the confirmed payment.</returns>
        public bool IsConfirmedPayment(BasePayment payment)
        {
            return Donation?.ConfirmedPayment != null && Donation.ConfirmedPayment.Id == payment.Id;
        }

        private async Task LoadEasyPayPaymentDetailsAsync()
        {
            List<EasyPayBaseClass> localPayments = Payments
                .OfType<EasyPayBaseClass>()
                .ToList();
            if (localPayments.Count == 0)
            {
                return;
            }

            this.EasyPayPaymentLookups = localPayments
                .Select(payment => new EasyPayPaymentDetails { LocalPayment = payment })
                .ToList();

            if (!this.HasEasyPayConfiguration())
            {
                this.EasyPayError = this.localizer["EasyPayNotConfigured"].Value;
                return;
            }

            Dictionary<string, EasyPayPaymentLookupResult> lookupResults =
                new Dictionary<string, EasyPayPaymentLookupResult>(StringComparer.OrdinalIgnoreCase);
            ISinglePaymentApi paymentApi;
            try
            {
                paymentApi = this.easyPayBuilder.GetSinglePaymentApi();
            }
            catch (Exception exception)
            {
                this.logger.LogError(
                    exception,
                    "Failed to create the Easypay payment API for donation {DonationId}.",
                    this.Donation.Id);
                this.EasyPayError = this.localizer["EasyPayDonationProviderUnavailable"].Value;
                return;
            }

            foreach (EasyPayPaymentDetails paymentDetails in this.EasyPayPaymentLookups)
            {
                string paymentId = paymentDetails.LocalPayment.EasyPayPaymentId;
                if (string.IsNullOrWhiteSpace(paymentId))
                {
                    paymentDetails.LookupError = this.localizer["EasyPayNoPaymentId"].Value;
                    continue;
                }

                if (!Guid.TryParse(paymentId, out Guid easyPayPaymentId))
                {
                    paymentDetails.LookupError = this.localizer["EasyPayInvalidPaymentId", paymentId].Value;
                    continue;
                }

                if (!lookupResults.TryGetValue(paymentId, out EasyPayPaymentLookupResult lookupResult))
                {
                    lookupResult = new EasyPayPaymentLookupResult();
                    try
                    {
                        lookupResult.Payment = await paymentApi.SingleIdGetAsync(easyPayPaymentId);
                    }
                    catch (ApiException exception)
                    {
                        this.logger.LogError(
                            exception,
                            "Easypay payment lookup failed for donation {DonationId}, local payment {PaymentId}, and Easypay payment {EasyPayPaymentId}.",
                            this.Donation.Id,
                            paymentDetails.LocalPayment.Id,
                            paymentId);
                        lookupResult.Error = this.localizer["EasyPayPaymentLookupFailed", exception.Message].Value;
                    }
                    catch (Exception exception)
                    {
                        this.logger.LogError(
                            exception,
                            "Easypay payment lookup failed for donation {DonationId}, local payment {PaymentId}, and Easypay payment {EasyPayPaymentId}.",
                            this.Donation.Id,
                            paymentDetails.LocalPayment.Id,
                            paymentId);
                        lookupResult.Error = this.localizer["EasyPayPaymentLookupFailed", exception.Message].Value;
                    }

                    lookupResults[paymentId] = lookupResult;
                }

                paymentDetails.ProviderPayment = lookupResult.Payment;
                paymentDetails.LookupError = lookupResult.Error;
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

        private bool IsConfiguredValue(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && !value.StartsWith("#{", StringComparison.Ordinal);
        }

        private async Task LoadSubscriptionInfoAsync(int donationId)
        {
            int? subscriptionId = await context.SubscriptionDonations
                .AsNoTracking()
                .Where(link => link.Donation != null && link.Donation.Id == donationId)
                .Select(link => (int?)link.Subscription.Id)
                .FirstOrDefaultAsync();

            if (!subscriptionId.HasValue)
            {
                return;
            }

            RelatedSubscription = await context.Subscriptions
                .AsNoTracking()
                .Include(subscription => subscription.InitialDonation)
                .FirstOrDefaultAsync(subscription => subscription.Id == subscriptionId.Value);

            if (RelatedSubscription?.InitialDonation != null)
            {
                IsInitialSubscriptionDonation = RelatedSubscription.InitialDonation.Id == donationId;
            }
        }

        /// <summary>
        /// Contains a local Easypay payment and the result of its provider lookup.
        /// </summary>
        public sealed class EasyPayPaymentDetails
        {
            /// <summary>
            /// Gets or sets the local payment.
            /// </summary>
            public EasyPayBaseClass LocalPayment { get; set; }

            /// <summary>
            /// Gets or sets the provider payment response.
            /// </summary>
            public EasyPayPaymentResponse ProviderPayment { get; set; }

            /// <summary>
            /// Gets or sets the provider lookup error.
            /// </summary>
            public string LookupError { get; set; }
        }

        private sealed class EasyPayPaymentLookupResult
        {
            public EasyPayPaymentResponse Payment { get; set; }

            public string Error { get; set; }
        }
    }
}
