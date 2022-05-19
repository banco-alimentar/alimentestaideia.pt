// -----------------------------------------------------------------------
// <copyright file="EasyPayPayments.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages.Payments;

using System;
using BancoAlimentar.AlimentaEstaIdeia.Model;
using BancoAlimentar.AlimentaEstaIdeia.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// This is the easy pay call page model.
/// </summary>
public class EasyPayPaymentsModel : PageModel
{
    private readonly IUnitOfWork context;

    /// <summary>
    /// Initializes a new instance of the <see cref="EasyPayPaymentsModel"/> class.
    /// </summary>
    /// <param name="context">Unit of work.</param>
    public EasyPayPaymentsModel(IUnitOfWork context)
    {
        this.context = context;
    }

    /// <summary>
    /// Execute the get operation.
    /// </summary>
    /// <param name="t_key">Key for the operation.</param>
    /// <param name="s">Operation status.</param>
    /// <param name="ep_k1">Subscription id.</param>
    /// <returns>Page.</returns>
    public IActionResult OnGet(Guid t_key, string s, Guid ep_k1)
    {
        if (t_key != Guid.Empty)
        {
            this.context.Donation.UpdateCreditCardPayment(t_key, s);
        }

        if (!string.IsNullOrEmpty(s))
        {
            ThanksModel.CompleteDonationFlow(HttpContext, this.context.User);

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
            else if (ep_k1 != Guid.Empty)
            {
                Subscription subscription = this.context.SubscriptionRepository.GetSubscriptionByEasyPayId(ep_k1);
                if (subscription != null)
                {
                    return RedirectToPage(
                        "/SubscriptionThanks",
                        new
                        {
                            PublicId = subscription.InitialDonation.PublicId,
                            SubscriptionPublicId = subscription.PublicId,
                        });
                }
            }
        }

        return this.RedirectToPage("/Index");
    }
}
