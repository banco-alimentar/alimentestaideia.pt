// -----------------------------------------------------------------------
// <copyright file="MBWayPayment.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages.Payments
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using Easypay.Rest.Client.Api;
    using Easypay.Rest.Client.Model;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Localization;

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
        private readonly SinglePaymentApi easyPayApiClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="MBWayPaymentModel"/> class.
        /// </summary>
        /// <param name="context">Unit of work.</param>
        /// <param name="configuration">Configuration.</param>
        /// <param name="easyPayBuilder">Easypay API builder.</param>
        public MBWayPaymentModel(
            IUnitOfWork context,
            IConfiguration configuration,
            EasyPayBuilder easyPayBuilder)
        {
            this.context = context;
            this.configuration = configuration;
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
        /// <param name="donationId">Donation id.</param>
        /// <param name="paymentId">Payment id.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnGetAsync(int donationId, Guid paymentId)
        {
            if (TempData["Donation"] != null)
            {
                donationId = (int)TempData["Donation"];
            }
            else
            {
                var targetDonationId = HttpContext.Session.GetInt32(DonationModel.DonationIdKey);
                if (targetDonationId.HasValue)
                {
                    donationId = targetDonationId.Value;
                }
            }

            if (TempData["mbway.paymend-id"] != null)
            {
                paymentId = (Guid)TempData["mbway.paymend-id"];
            }
            else
            {
                var targetPaymentId = HttpContext.Session.GetString("mbway.paymend-id");
                if (!string.IsNullOrEmpty(targetPaymentId))
                {
                    paymentId = Guid.Parse(targetPaymentId);
                }
            }

            Donation = this.context.Donation.GetFullDonationById(donationId);
            PaymentStatus = Donation.PaymentStatus;
            SinglePaymentWithTransactionsResponse spResp = await easyPayApiClient.GetSinglePaymentAsync(paymentId, CancellationToken.None);

            // Validate Payment status (EasyPay+Repository)
            if (spResp.PaymentStatus == "pending")
            {
                PaymentStatus = PaymentStatus.WaitingPayment;
                Response.Headers.Add("Refresh", PageRefreshInSeconds.ToString());
            }
            else if (spResp.PaymentStatus == "paid")
            {
                PaymentStatus = PaymentStatus.Payed;
                TempData["Donation"] = donationId;
                return RedirectToPage("/Thanks");
            }
            else
            {
                PaymentStatus = Donation.PaymentStatus = PaymentStatus.ErrorPayment;
                this.context.Complete();
            }

            return Page();
        }
    }
}
