// -----------------------------------------------------------------------
// <copyright file="EasyPayPayments.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages.Payments
{
    using System;
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
        /// <returns>Page.</returns>
        public IActionResult OnGet(Guid t_key, string s)
        {
            this.context.Donation.UpdateCreditCardPayment(t_key, s);
            if (!string.IsNullOrEmpty(s))
            {
                ThanksModel.CompleteDonationFlow(HttpContext, this.context.User);
                TempData["Donation"] = this.context.Donation.GetDonationIdFromPublicId(t_key);
                TempData["Paymen-Status"] = s;
                if (s == "ok")
                {
                    return RedirectToPage("/Thanks");
                }
                else
                {
                    return RedirectToPage("/Payment");
                }
            }
            else
            {
                return this.RedirectToPage("/Index");
            }
        }
    }
}
