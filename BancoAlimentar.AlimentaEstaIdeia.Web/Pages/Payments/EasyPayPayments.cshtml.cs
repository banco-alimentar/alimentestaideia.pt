// -----------------------------------------------------------------------
// <copyright file="EasyPayPayments.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages.Payments
{
    using System;
    using System.Threading.Tasks;
    using Azure;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using Easypay.Rest.Client.Api;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Single = Easypay.Rest.Client.Model.Single;

    /// <summary>
    /// This is the easy pay call page model.
    /// </summary>
    public class EasyPayPaymentsModel : PageModel
    {
        private readonly IUnitOfWork context;
        private readonly SinglePaymentApi easyPayApiClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="EasyPayPaymentsModel"/> class.
        /// </summary>
        /// <param name="context">Unit of work.</param>
        /// <param name="easyPayBuilder">EasyPay API builder.</param>
        public EasyPayPaymentsModel(IUnitOfWork context, EasyPayBuilder easyPayBuilder)
        {
            this.context = context;
            this.easyPayApiClient = easyPayBuilder.GetSinglePaymentApi();
        }

        /// <summary>
        /// Gets or sets the donation.
        /// </summary>
        public Donation Donation { get; set; }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <param name="t_key">Donation Public Id.</param>
        /// <param name="s">Operation status.</param>
        /// <param name="ep_k1">EasyPay Id.</param>
        /// <returns>Page.</returns>
        public async Task<IActionResult> OnGetAsync(Guid t_key, string s, Guid ep_k1)
        {
            int donationId = this.context.Donation.GetDonationIdFromPublicId(t_key);

            Donation = this.context.Donation.GetFullDonationById(donationId);
            if (Donation != null)
            {
                Single response = await easyPayApiClient.SingleIdGetAsync(ep_k1);

                this.context.Donation.UpdatePaymentStatus<CreditCardPayment>(t_key, response.PaymentStatus);

                if (!string.IsNullOrEmpty(s))
                {
                    ThanksModel.CompleteDonationFlow(HttpContext, this.context.User, t_key);

                    if (ep_k1 != Guid.Empty)
                    {
                        Subscription subscription = this.context.SubscriptionRepository.GetSubscriptionByEasyPayId(ep_k1);
                        if (subscription != null)
                        {
                            return RedirectToPage(
                                "/SubscriptionThanks",
                                new
                                {
                                    subscription.InitialDonation.PublicId,
                                    SubscriptionPublicId = subscription.PublicId,
                                });
                        }
                    }

                    if (t_key != Guid.Empty)
                    {
                        if (s == "ok")
                        {
                            return RedirectToPage("/Thanks", new { PublicId = t_key });
                        }
                        else
                        {
                            return RedirectToPage("/Payment", new { paymentStatus = s });
                        }
                    }
                }
            }

            return this.RedirectToPage("/Index");
        }
    }
}