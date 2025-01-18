// -----------------------------------------------------------------------
// <copyright file="Thanks.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages.Tenants.BancoAlimentar.Pages
{
    using System;
    using System.Collections.Generic;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using global::BancoAlimentar.AlimentaEstaIdeia.Model;
    using global::BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using global::BancoAlimentar.AlimentaEstaIdeia.Repository;
    using global::BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using global::BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using global::BancoAlimentar.AlimentaEstaIdeia.Web.Model;
    using global::BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Thanks Model.
    /// </summary>
    public class ThanksModel : PageModel
    {
        private readonly UserManager<WebUser> userManager;
        private readonly IUnitOfWork context;
        private readonly IStringLocalizerFactory stringLocalizerFactory;
        private readonly IConfiguration configuration;
        private readonly TelemetryClient telemetryClient;
        private readonly IMail mail;
        private readonly IStringLocalizer localizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThanksModel"/> class.
        /// </summary>
        /// <param name="userManager">User manager.</param>
        /// <param name="context">Unit of work.</param>
        /// <param name="stringLocalizerFactory">Localizer factory.</param>
        /// <param name="telemetryClient">Telemetry client.</param>
        /// <param name="mail">Email service.</param>
        /// <param name="configuration">Configuration.</param>
        public ThanksModel(
            UserManager<WebUser> userManager,
            IUnitOfWork context,
            IStringLocalizerFactory stringLocalizerFactory,
            TelemetryClient telemetryClient,
            IMail mail,
            IConfiguration configuration)
        {
            this.userManager = userManager;
            this.context = context;
            this.stringLocalizerFactory = stringLocalizerFactory;
            this.telemetryClient = telemetryClient;
            this.mail = mail;
            this.localizer = stringLocalizerFactory.Create("Pages.Thanks", System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);
            this.configuration = configuration;
        }

        /// <summary>
        /// Gets or sets the current user.
        /// </summary>
        public WebUser CurrentUser { get; set; }

        /// <summary>
        /// Gets or sets the current donation.
        /// </summary>
        public Donation Donation { get; set; }

        /// <summary>
        /// Gets or sets the message for the Twitter handler.
        /// </summary>
        [BindProperty]
        public string TwittMessage { get; set; }

        /// <summary>
        /// Complete the donation flow.
        /// </summary>
        /// <param name="context">Current <see cref="HttpContext"/>.</param>
        /// <param name="userRepository">User respository.</param>
        public static void CompleteDonationFlow(HttpContext context, UserRepository userRepository)
        {
            if (context != null)
            {
                context.Items.Remove(KeyNames.DonationSessionKey);
                context.Session.Remove(KeyNames.DonationSessionKey);
                UserDataDonationFlowModel userData = context.Session.GetObjectFromJson<UserDataDonationFlowModel>(DonationModel.SaveAnonymousUserDataFlowKey);
                if (userData != null)
                {
                    userRepository.UpdateAnonymousUserData(userData.Email, userData.CompanyName, userData.Nif, userData.FullName, userData.Address);
                    context.Session.Remove(DonationModel.SaveAnonymousUserDataFlowKey);
                }
            }
        }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <param name="publicId">Donation Public Id.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnGet(Guid publicId)
        {
            int id = this.context.Donation.GetDonationIdFromPublicId(publicId);
            if (id == 0)
            {
                // There are super weird situations where the parameter publicId
                // is not the Donation.PublicID, but the Payement.TransactionKey
                // I don't know the reason why, but that leads to some
                // NullReferenceExceptions, so in case that I don't find the donation
                // I'm trying this new way to find the donation based on the PublicId.
                id = this.context.Donation.GetDonationByTransactionKey(publicId);
            }

            Subscription subscription = this.context.SubscriptionRepository.GetSubscriptionFromDonationId(id);
            if (subscription != null)
            {
                return this.RedirectToPage("./SubscriptionThanks", new { id = subscription.Id });
            }

            CurrentUser = await userManager.GetUserAsync(new ClaimsPrincipal(User.Identity));

            Donation = this.context.Donation.GetFullDonationById(id);
            if (Donation != null)
            {
                this.context.Donation.InvalidateTotalCache();
                string foodBank = "Lisbon";
                if (Donation.FoodBank != null && !string.IsNullOrEmpty(Donation.FoodBank.Name))
                {
                    foodBank = Donation.FoodBank.Name;
                }

                TwittMessage = string.Format(localizer.GetString("TwittMessage"), Donation.DonationAmount, foodBank);
                if (this.configuration.IsSendingEmailEnabled())
                {
                    await SendThanksEmailForPaypalPayment(Donation);
                }

                this.telemetryClient.TrackEvent("ThanksOnGetSuccess", new Dictionary<string, string> { { "DonationId", id.ToString() }, { "UserId", CurrentUser?.Id }, { "PublicId", Donation.PublicId.ToString() } });
            }
            else
            {
                this.telemetryClient.TrackEvent(
                    "DonationNotFound",
                    new Dictionary<string, string>
                    {
                        { "Page", this.GetType().Name },
                        { "UserId", CurrentUser?.Id },
                    });
            }

            CompleteDonationFlow(HttpContext, this.context.User);

            return Page();
        }

        /// <summary>
        /// Send the thanks email for the paypal payment.
        /// </summary>
        /// <param name="donation">Donation.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task SendThanksEmailForPaypalPayment(Donation donation)
        {
            if (donation.ConfirmedPayment is PayPalPayment)
            {
                await this.mail.GenerateInvoiceAndSendByEmail(donation, Request, this.context, this.HttpContext.GetTenant());
            }
        }
    }
}
