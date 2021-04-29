namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages.Payments
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
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

            ThanksModel.CompleteDonationFlow(HttpContext);
        }
    }
}
