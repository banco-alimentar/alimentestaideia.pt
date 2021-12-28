// -----------------------------------------------------------------------
// <copyright file="SubscriptionThanks.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages
{
    using System;
    using System.Collections.Generic;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Represent the subscription thanks page model.
    /// </summary>
    public class SubscriptionThanksModel : PageModel
    {
        private readonly UserManager<WebUser> userManager;
        private readonly IUnitOfWork context;
        private readonly IStringLocalizerFactory stringLocalizerFactory;
        private readonly IConfiguration configuration;
        private readonly TelemetryClient telemetryClient;
        private readonly IMail mail;
        private readonly IStringLocalizer localizer;
        private readonly IStringLocalizer sharedIdentityLocalizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionThanksModel"/> class.
        /// </summary>
        /// <param name="userManager">User manager.</param>
        /// <param name="context">Unit of work.</param>
        /// <param name="stringLocalizerFactory">Localizer factory.</param>
        /// <param name="telemetryClient">Telemetry client.</param>
        /// <param name="mail">Email service.</param>
        /// <param name="configuration">Configuration.</param>
        public SubscriptionThanksModel(
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
            this.sharedIdentityLocalizer = stringLocalizerFactory.Create(
                typeof(IdentitySharedResources).Name,
                System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);
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
        /// Gets or sets the current subscription.
        /// </summary>
        public Subscription Subscription { get; set; }

        /// <summary>
        /// Gets or sets the message for the Twitter handler.
        /// </summary>
        [BindProperty]
        public string TwittMessage { get; set; }

        /// <summary>
        /// Gets or sets the subscription frequency.
        /// </summary>
        [BindProperty]
        public string Frecuency { get; set; }

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
        /// <param name="publicId">Donation Public Dd.</param>
        /// <param name="subscriptionPublicId">Subscription Public Id.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task OnGet(Guid publicId, Guid subscriptionPublicId)
        {
            int donationId = this.context.Donation.GetDonationIdFromPublicId(publicId);
            CurrentUser = await userManager.GetUserAsync(new ClaimsPrincipal(User.Identity));
            Donation = this.context.Donation.GetFullDonationById(donationId);
            Subscription = this.context.SubscriptionRepository.GetSubscriptionByPublicId(subscriptionPublicId);
            if (Donation != null && Subscription != null)
            {
                this.Frecuency = this.sharedIdentityLocalizer.GetString(Subscription.Frequency);
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

                this.telemetryClient.TrackEvent(
                    "ThanksOnGetSuccess",
                    new Dictionary<string, string>
                    {
                        { "DonationId", donationId.ToString() },
                        { "UserId", CurrentUser?.Id },
                        { "PublicId", Donation.PublicId.ToString() },
                    });
            }
            else
            {
                if (Donation == null)
                {
                    this.telemetryClient.TrackEvent(
                        "DonationNotFound",
                        new Dictionary<string, string>
                        {
                            { "Page", this.GetType().Name },
                            { "DonationId", donationId.ToString() },
                            { "UserId", CurrentUser?.Id },
                        });
                }

                if (Subscription == null)
                {
                    this.telemetryClient.TrackEvent(
                        "SubscriptionNotFound",
                        new Dictionary<string, string>
                        {
                            { "Page", this.GetType().Name },
                            { "SubscriptionId", subscriptionPublicId.ToString() },
                            { "UserId", CurrentUser?.Id },
                        });
                }
            }

            CompleteDonationFlow(HttpContext, this.context.User);
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
                await this.mail.GenerateInvoiceAndSendByEmail(donation, Request);
            }
        }
    }
}
