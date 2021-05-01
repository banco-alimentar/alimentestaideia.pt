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

    public class EasyPayPaymentsModel : PageModel
    {
        private readonly IUnitOfWork context;

        public EasyPayPaymentsModel(IUnitOfWork context)
        {
            this.context = context;
        }

        public IActionResult OnGet(Guid t_key, string s)
        {
            this.context.Donation.UpdateCreditCardPayment(t_key, s);
            if (!string.IsNullOrEmpty(s))
            {
                ThanksModel.CompleteDonationFlow(HttpContext);
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
