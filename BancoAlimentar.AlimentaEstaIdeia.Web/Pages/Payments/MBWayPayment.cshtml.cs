// -----------------------------------------------------------------------
// <copyright file="MBWayPayment.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages.Payments
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Common.EasyPay;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Services;
    using Easypay.Rest.Client.Api;
    using Easypay.Rest.Client.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Configuration;
    using InlineObject9 = Easypay.Rest.Client.Model.InlineObject9;

    /// <summary>
    /// MBWay payment model.
    /// </summary>
    public class MBWayPaymentModel : PageModel
    {
        /// <summary>
        /// Refresh of the page.
        /// </summary>
        public const int PageRefreshInSeconds = 5;

        private readonly IUnitOfWork context;
        private readonly IConfiguration configuration;
        private readonly TelemetryClient telemetryClient;
        private readonly ISinglePaymentApi easyPayApiClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="MBWayPaymentModel"/> class.
        /// </summary>
        /// <param name="context">Unit of work.</param>
        /// <param name="configuration">Configuration.</param>
        /// <param name="telemetryClient">Telemetry Client.</param>
        /// <param name="easyPayBuilder">Easypay API builder.</param>
        public MBWayPaymentModel(
            IUnitOfWork context,
            IConfiguration configuration,
            TelemetryClient telemetryClient,
            EasyPayBuilder easyPayBuilder)
        {
            this.context = context;
            this.configuration = configuration;
            this.telemetryClient = telemetryClient;
            this.easyPayApiClient = easyPayBuilder.GetSinglePaymentApi();
        }

        /// <summary>
        /// Gets or sets the donation.
        /// </summary>
        public Donation Donation { get; set; }

        /// <summary>
        /// Gets or sets the payment status.
        /// </summary>
        public PaymentStatus PaymentStatus { get; set; }

        /// <summary>
        /// Gets or sets the suggested other payment methods.
        /// </summary>
        public string SuggestOtherPaymentMethod { get; set; }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <param name="publicId">Donation public id.</param>
        /// <param name="paymentId">Payment id.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnGetAsync(Guid publicId, Guid paymentId)
        {
            int donationId = this.context.Donation.GetDonationIdFromPublicId(publicId);

            Donation = this.context.Donation.GetFullDonationById(donationId);
            if (Donation == null)
            {
                return RedirectToPage("/Payment");
            }

            if (this.TryRedirectToThanksForCompletedDonation(Donation.PublicId, out IActionResult thanksRedirect))
            {
                return thanksRedirect;
            }

            PaymentStatus = Donation.PaymentStatus;

            try
            {
                InlineObject9 response = await easyPayApiClient.SingleIdGetAsync(paymentId);
                if (response == null)
                {
                    PaymentStatus = PaymentStatus.WaitingPayment;
                    Response.Headers.Append("Refresh", PageRefreshInSeconds.ToString());
                    return Page();
                }

                SinglePaymentStatus paymentStatus = response.ResolvePaymentStatus();

                // Validate Payment status (EasyPay+Repository)
                if (paymentStatus == SinglePaymentStatus.Pending)
                {
                    PaymentStatus = PaymentStatus.WaitingPayment;
                    Response.Headers.Append("Refresh", PageRefreshInSeconds.ToString());
                }
                else if (response.IsSinglePaymentComplete())
                {
                    PaymentStatus = PaymentStatus.Payed;
                    this.context.Donation.UpdatePaymentStatus<MBWayPayment>(Donation.PublicId, paymentStatus);
                    ThanksModel.CompleteDonationFlow(HttpContext, this.context.User, Donation.PublicId);
                    return RedirectToPage("/Thanks", new { Donation.PublicId });
                }
                else if (this.TryRedirectToThanksForCompletedDonation(Donation.PublicId, out thanksRedirect))
                {
                    // Webhook may have completed the donation while Easypay still reports failed/expired.
                    return thanksRedirect;
                }
                else if (paymentStatus == SinglePaymentStatus.Failed || paymentStatus == SinglePaymentStatus.Deleted)
                {
                    PaymentStatus = PaymentStatus.ErrorPayment;
                    this.context.Donation.UpdatePaymentStatus<MBWayPayment>(Donation.PublicId, paymentStatus);
                    this.context.Complete();
                }
                else
                {
                    PaymentStatus = PaymentStatus.WaitingPayment;
                    Response.Headers.Append("Refresh", PageRefreshInSeconds.ToString());
                }
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(
                    ex,
                    new Dictionary<string, string>()
                    {
                        { "DonationId", donationId.ToString() },
                        { "PaymentId", paymentId.ToString() },
                    });

                if (this.TryRedirectToThanksForCompletedDonation(Donation.PublicId, out thanksRedirect))
                {
                    return thanksRedirect;
                }
            }

            return Page();
        }

        private bool TryRedirectToThanksForCompletedDonation(Guid publicId, out IActionResult redirectResult)
        {
            redirectResult = null;
            int donationId = this.context.Donation.GetDonationIdFromPublicId(publicId);
            Donation donation = this.context.Donation.GetFullDonationById(donationId);
            if (donation?.PaymentStatus != PaymentStatus.Payed)
            {
                return false;
            }

            ThanksModel.CompleteDonationFlow(HttpContext, this.context.User, donation.PublicId);
            redirectResult = RedirectToPage("/Thanks", new { donation.PublicId });
            return true;
        }
    }
}
