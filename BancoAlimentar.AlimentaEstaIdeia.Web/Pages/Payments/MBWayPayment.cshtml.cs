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
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using Easypay.Rest.Client.Api;
    using Easypay.Rest.Client.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Configuration;

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
        private readonly EasyPayBuilder easyPayBuilder;
        private readonly SinglePaymentApi easyPayApiClient;

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
            this.easyPayBuilder = easyPayBuilder;
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
            if (Donation != null)
            {
                PaymentStatus = Donation.PaymentStatus;
                SinglePaymentWithTransactionsResponse response;

                try
                {
                    response = await easyPayApiClient.GetSinglePaymentAsync(paymentId, CancellationToken.None);

                    if (response != null)
                    {
                        // Validate Payment status (EasyPay+Repository)
                        if (response.PaymentStatus == "pending")
                        {
                            PaymentStatus = PaymentStatus.WaitingPayment;
                            Response.Headers.Add("Refresh", PageRefreshInSeconds.ToString());
                        }
                        else if (response.PaymentStatus == "paid")
                        {
                            PaymentStatus = PaymentStatus.Payed;
                            this.context.Donation.UpdatePaymentStatus<MBWayPayment>(Donation.PublicId, response.PaymentStatus);
                            ThanksModel.CompleteDonationFlow(HttpContext, this.context.User);
                            return RedirectToPage("/Thanks", new { PublicId = Donation.PublicId });
                        }
                        else
                        {
                            PaymentStatus = Donation.PaymentStatus = PaymentStatus.ErrorPayment;
                            this.context.Donation.UpdatePaymentStatus<MBWayPayment>(Donation.PublicId, response.PaymentStatus);
                            this.context.Complete();
                        }
                    }
                    else
                    {
                        PaymentStatus = PaymentStatus.WaitingPayment;
                        Response.Headers.Add("Refresh", PageRefreshInSeconds.ToString());
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
                }

                return Page();
            }
            else
            {
                return RedirectToPage("/Payment");
            }
        }
    }
}
