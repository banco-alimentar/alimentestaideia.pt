// -----------------------------------------------------------------------
// <copyright file="SubscriptionPayment.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using Easypay.Rest.Client.Client;
    using Easypay.Rest.Client.Model;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Subscription payment model class.
    /// </summary>
    public class SubscriptionPaymentModel : PageModel
    {
        private readonly IConfiguration configuration;
        private readonly IUnitOfWork context;
        private readonly EasyPayBuilder easyPayBuilder;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionPaymentModel"/> class.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="context">A reference to the <see cref="IUnitOfWork"/>.</param>
        /// <param name="easyPayBuilder">A referece to the EasyPay builder.</param>
        public SubscriptionPaymentModel(
            IConfiguration configuration,
            IUnitOfWork context,
            EasyPayBuilder easyPayBuilder)
        {
            this.configuration = configuration;
            this.context = context;
            this.easyPayBuilder = easyPayBuilder;
        }

        /// <summary>
        /// Gets or sets the current donation.
        /// </summary>
        public Donation Donation { get; set; }

        [BindProperty]
        public int DonationId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the current error in the payment system.
        /// </summary>
        [BindProperty]
        public bool PaymentStatusError { get; set; }

        [BindProperty]
        public string FrequencyStringValue { get; set; }

        /// <summary>
        /// Gets or sets the subscription frequency.
        /// </summary>
        public PaymentSubscription.FrequencyEnum Frequency { get; set; }

        public IActionResult OnGet(int donationId = 0, Guid publicDonationId = default(Guid))
        {
            if (TempData["Donation"] != null)
            {
                donationId = (int)TempData["Donation"];
            }
            else
            {
                if (publicDonationId != default(Guid))
                {
                    donationId = this.context.Donation.GetDonationIdFromPublicId(publicDonationId);
                }
                else
                {
                    var targetDonationId = HttpContext.Session.GetInt32(DonationModel.DonationIdKey);
                    if (targetDonationId.HasValue)
                    {
                        donationId = targetDonationId.Value;
                    }
                }
            }

            if (TempData["SubscriptionFrequencySelected"] != null)
            {
                FrequencyStringValue = TempData["SubscriptionFrequencySelected"] as string;
            }

            Donation = this.context.Donation.GetFullDonationById(donationId);

            return Page();
        }

        public IActionResult OnPostCreditCard()
        {
            Donation = this.context.Donation.GetFullDonationById(DonationId);
            string transactionKey = Guid.NewGuid().ToString();
            InlineResponse2015 targetPayment = CreateEasyPaySubscriptionPaymentAsync(transactionKey);
            if (targetPayment != null)
            {
                string url = targetPayment.Method.Url;

                this.context.SubscriptionRepository.CreateSubscription(
                    Donation,
                    transactionKey,
                    url);

                return this.Redirect(url);
            }
            else
            {
                PaymentStatusError = true;
                return Page();
            }
        }

        private InlineResponse2015 CreateEasyPaySubscriptionPaymentAsync(string transactionKey)
        {
            Frequency = Enum.Parse<PaymentSubscription.FrequencyEnum>(FrequencyStringValue);
            PaymentSubscription request = new PaymentSubscription(
                 capture: new SubscriptionCapture(
                    "Alimente esta ideia Donation subscription",
                    transactionKey,
                    new SinglePaymentRequestCapture("Alimente esta ideia Donation subscription", transactionKey)))
            {
                Id = Guid.NewGuid(),
                Key = Donation.PublicId.ToString(),
                ExpirationTime = DateTime.UtcNow.GetEasyPayDateTimeString(),
                Currency = PaymentSubscription.CurrencyEnum.EUR,
                Customer = new Customer()
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
                StartTime = DateTime.UtcNow.AddDays(1).GetEasyPayDateTimeString(),
                CaptureNow = true,
                Method = PaymentSubscriptionMethodAvailable.Cc,
                CreatedAt = DateTime.UtcNow.GetEasyPayDateTimeString(),
                Failover = false,
                MaxCaptures = 0,
                Retries = 0,
                SddMandate = null,
            };

            string json1 = JsonConvert.SerializeObject(request);

            InlineResponse2015 response = null;
            try
            {
                response = this.easyPayBuilder.GetSubscriptionPaymentApi().SubscriptionPost(request);
            }
            catch (ApiException ex)
            {
                if (ex.ErrorContent is string)
                {
                    string json = (string)ex.ErrorContent;
                    JObject obj = JObject.Parse(json);
                    JArray errorList = (JArray)obj["message"];
                    StringBuilder stringBuilder = new StringBuilder();
                    foreach (var item in errorList.Children())
                    {
                        stringBuilder.Append(item.Value<string>());
                        stringBuilder.Append(Environment.NewLine);
                    }

                }
            }

            if (response != null)
            {
                return response;
            }
            else
            {
                return null;
            }
        }
    }
}
