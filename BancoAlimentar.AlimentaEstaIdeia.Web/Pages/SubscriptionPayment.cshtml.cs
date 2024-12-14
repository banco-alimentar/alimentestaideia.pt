// -----------------------------------------------------------------------
// <copyright file="SubscriptionPayment.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages
{
    using System;
    using System.Collections.Generic;
    using System.Security.Claims;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Features;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using Easypay.Rest.Client.Client;
    using Easypay.Rest.Client.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Configuration;
    using Microsoft.FeatureManagement.Mvc;
    using Newtonsoft.Json;
    using static Easypay.Rest.Client.Model.SubscriptionPostRequest;

    /// <summary>
    /// Subscription payment model class.
    /// </summary>
    [FeatureGate(DevelopingFeatureFlags.SubscriptionPayements)]
    public class SubscriptionPaymentModel : PageModel
    {
        private readonly IConfiguration configuration;
        private readonly IUnitOfWork context;
        private readonly UserManager<WebUser> userManager;
        private readonly EasyPayBuilder easyPayBuilder;
        private readonly TelemetryClient telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionPaymentModel"/> class.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="context">A reference to the <see cref="IUnitOfWork"/>.</param>
        /// <param name="userManager">User manager.</param>
        /// <param name="easyPayBuilder">A referece to the EasyPay builder.</param>
        /// <param name="telemetryClient">Telemetry Client.</param>
        public SubscriptionPaymentModel(
            IConfiguration configuration,
            IUnitOfWork context,
            UserManager<WebUser> userManager,
            EasyPayBuilder easyPayBuilder,
            TelemetryClient telemetryClient)
        {
            this.configuration = configuration;
            this.context = context;
            this.userManager = userManager;
            this.easyPayBuilder = easyPayBuilder;
            this.telemetryClient = telemetryClient;
        }

        /// <summary>
        /// Gets or sets the current donation.
        /// </summary>
        public Donation Donation { get; set; }

        /// <summary>
        /// Gets or sets the donation id.
        /// </summary>
        [BindProperty]
        public int DonationId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the current error in the payment system.
        /// </summary>
        [BindProperty]
        public bool PaymentStatusError { get; set; }

        /// <summary>
        /// Gets or sets the Frequency for the subscription payment.
        /// </summary>
        [BindProperty]
        public string FrequencyStringValue { get; set; }

        /// <summary>
        /// Gets or sets the subscription frequency.
        /// </summary>
        public FrequencyEnum Frequency { get; set; }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <param name="publicId">Public donation id.</param>
        /// <param name="frequency">Frequency.</param>
        /// <returns>Page.</returns>
        public IActionResult OnGet(Guid publicId, string frequency)
        {
            int donationId = 0;
            if (publicId != default(Guid))
            {
                donationId = this.context.Donation.GetDonationIdFromPublicId(publicId);
            }
            else
            {
                int? targetDonationId = HttpContext.Session.GetDonationId();
                if (targetDonationId.HasValue)
                {
                    donationId = targetDonationId.Value;
                }
            }

            FrequencyStringValue = frequency;
            Donation = this.context.Donation.GetFullDonationById(donationId);

            if (Donation == null)
            {
                return RedirectToPage("./Error", new { errorMsg = "Doação não encontrada" });
            }
            else
            {
                return Page();
            }
        }

        /// <summary>
        /// Execetue the credit card post payment operation.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostCreditCard()
        {
            var user = await userManager.GetUserAsync(new ClaimsPrincipal(User.Identity));

            AlimentaEstaIdeia.Model.Subscription existingSubscription = this.context.SubscriptionRepository.GetSubscriptionFromDonationId(DonationId);
            if (existingSubscription != null && existingSubscription.Status == SubscriptionStatus.Created)
            {
                // retry the subscription if for the current flow we already have donation id
                // and the status of the subscription is created.
                return this.Redirect(existingSubscription.Url);
            }
            else
            {
                Donation = this.context.Donation.GetFullDonationById(DonationId);
                string transactionKey = Guid.NewGuid().ToString();
                var easyPaySubcription = CreateEasyPaySubscriptionPaymentAsync(transactionKey);

                if (easyPaySubcription.InlineResponse != null)
                {
                    string url = string.Empty;

                    // string url = easyPaySubcription.inlineResponse.Method.Url;
                    this.context.SubscriptionRepository.CreateSubscription(
                        Donation,
                        transactionKey,
                        easyPaySubcription.InlineResponse.Id.ToString(),
                        url,
                        user,
                        easyPaySubcription.Request,
                        Frequency);

                    this.context.Donation.CreateCreditCardPaymnet(
                        Donation,
                        easyPaySubcription.InlineResponse.Id.ToString(),
                        transactionKey,
                        url,
                        DateTime.UtcNow);
                    return this.Redirect(url);
                }
                else
                {
                    PaymentStatusError = true;
                    return Page();
                }
            }
        }

        private DateTime ConvertFrequencyToDateTime(FrequencyEnum frequency)
        {
            DateTime result = DateTime.MinValue;
            string value = frequency.ToString().TrimStart('_');
            int count = int.Parse(value.Substring(0, 1));
            string modifier = value.Substring(1, 1);
            switch (modifier)
            {
                case "D":
                    {
                        result = DateTime.Now.GetPortugalDateTime().AddDays(count);
                        break;
                    }

                case "W":
                    {
                        result = DateTime.Now.GetPortugalDateTime().AddDays(7 * count);
                        break;
                    }

                case "M":
                    {
                        result = DateTime.Now.GetPortugalDateTime().AddMonths(count);
                        break;
                    }

                case "Y":
                    {
                        result = DateTime.Now.GetPortugalDateTime().AddYears(count);
                        break;
                    }
            }

            return result;
        }

        private (SubscriptionPost201Response InlineResponse, SubscriptionPostRequest Request) CreateEasyPaySubscriptionPaymentAsync(string transactionKey)
        {
            this.telemetryClient.TrackEvent("CreateEasyPaySubscriptionPaymentAsync", new Dictionary<string, string>()
                {
                    { "transactionKey", transactionKey },
                    { "FrequencyStringValue", this.FrequencyStringValue },
                });

            Frequency = Enum.Parse<FrequencyEnum>(string.Concat("_", FrequencyStringValue));
            SubscriptionPostRequest request = new SubscriptionPostRequest(
                 capture: new SubscriptionPostRequestCapture(
                    transactionKey,
                    new CaptureIdPostRequestAccount(Guid.Parse(transactionKey)),
                    "Alimente esta ideia Donation subscription"),
                 startTime: DateTime.UtcNow.GetEasyPayDateTimeString())
            {
                Key = transactionKey,
                ExpirationTime = DateTime.UtcNow.AddYears(value: 13).GetEasyPayDateTimeString(),
                Currency = Currency.EUR,
                Customer = new SubscriptionPostRequestCustomer()
                {
                    Email = Donation.User.Email,
                    Name = Donation.User.UserName,
                    Phone = Donation.User.PhoneNumber,
                    FiscalNumber = Donation.User.Nif,
                    Language = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName,
                    Key = Donation.User.Id,
                },
                Value = Donation.DonationAmount,
                Frequency = Frequency,
                StartTime = ConvertFrequencyToDateTime(Frequency).GetEasyPayDateTimeString(),
                CaptureNow = true,
                Method = MethodEnum.Cc,
                Failover = false,
                MaxCaptures = 0,
                Retries = 0,
                SddMandate = null,
            };

            string json1 = JsonConvert.SerializeObject(request);

            SubscriptionPost201Response response = null;
            try
            {
                response = this.easyPayBuilder.GetSubscriptionPaymentApi().SubscriptionPost(request);
            }
            catch (ApiException ex)
            {
                this.telemetryClient.TrackException(ex, new Dictionary<string, string>()
                {
                    { "TransactionKey", transactionKey },
                    { "DonationId", Donation.Id.ToString() },
                    { "json", json1 },
                });
            }

            return (response, request);
        }
    }
}