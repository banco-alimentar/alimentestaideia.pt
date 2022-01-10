// -----------------------------------------------------------------------
// <copyright file="DonationHistory.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage
{
    using System;
    using System.Globalization;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Donation history model.
    /// </summary>
    public class DonationHistoryModel : PageModel
    {
        private readonly UserManager<WebUser> userManager;
        private readonly IUnitOfWork context;

        /// <summary>
        /// Initializes a new instance of the <see cref="DonationHistoryModel"/> class.
        /// </summary>
        /// <param name="userManager">User Manager.</param>
        /// <param name="context">Unit of work.</param>
        public DonationHistoryModel(
            UserManager<WebUser> userManager,
            IUnitOfWork context)
        {
            this.userManager = userManager;
            this.context = context;
        }

        /// <summary>
        /// Gets or sets the Total ammoun this user donated.
        /// </summary>
        public double GetsDonatedTotal { get; set; }

        /// <summary>
        /// Gets or sets  the Number of donations the user completed.
        /// </summary>
        public int GetsDonatedCount { get; set; }

        /// <summary>
        /// Gets or sets  the date of the first time the user completed a donation.
        /// </summary>
        public DateTime GetsDonatedFirstDate { get; set; }

        /// <summary>
        /// Gets or sets  the date of the first time the user completed a donation.
        /// </summary>
        public string GetsDonatedFirstDateString { get; set; }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        public async Task OnGetAsync()
        {
            var user = await userManager.GetUserAsync(User);
            var donationsOverall = this.context.Donation.GetTotalUserDonations(user.Id);
            GetsDonatedTotal = donationsOverall.total;
            GetsDonatedCount = donationsOverall.count;
            GetsDonatedFirstDate = donationsOverall.firstDate;
            GetsDonatedFirstDateString = donationsOverall.firstDate.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Special handler to get the data as json.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnGetDataTableData()
        {
            var user = await userManager.GetUserAsync(User);
            var donations = this.context.Donation.GetUserDonation(user.Id);

            JArray list = new JArray();
            int count = 1;
            foreach (var item in donations)
            {
                Subscription subscription = this.context.SubscriptionRepository.GetSubscriptionFromDonationId(item.Id);
                JObject obj = new JObject();
                obj.Add("Id", count);
                obj.Add("DonationDate", item.DonationDate.ToString());
                obj.Add("FoodBank", item.FoodBank != null ? item.FoodBank.Name : string.Empty);
                obj.Add("DonationAmount", item.DonationAmount);
                obj.Add("SubscriptionPublicId", subscription?.PublicId);
                obj.Add("PublicId", item.PublicId);
                obj.Add("Nif", item.Nif);
                obj.Add("UsersNif", user.Nif);
                JArray paymentArray = new JArray();
                foreach (var payment in item.PaymentList)
                {
                    JObject paymentItem = new JObject();
                    paymentItem.Add("PaymentType", this.context.Donation.GetPaymentHumanName(payment).ToString());
                    if (payment is CreditCardPayment)
                    {
                        CreditCardPayment creditCardPayment = (CreditCardPayment)payment;
                        if (creditCardPayment.Status != Constants.CreditCardSucceed)
                        {
                            paymentItem.Add("PaymentUrl", creditCardPayment.Url);
                        }

                        paymentItem.Add("PaymentStatus", creditCardPayment.Status);
                    }
                    else if (payment is PayPalPayment)
                    {
                        PayPalPayment payPalPayment = (PayPalPayment)payment;
                    }

                    paymentItem.Add("PaymentItemId", payment.Id);
                    paymentArray.Add(paymentItem);
                }

                obj.Add("Payments", paymentArray);
                obj.Add("PaymentStatus", item.PaymentStatus.ToString());
                list.Add(obj);
                count++;
            }

            return new ContentResult()
            {
                Content = JsonConvert.SerializeObject(list),
                ContentType = "application/json",
                StatusCode = 200,
            };
        }
    }
}
