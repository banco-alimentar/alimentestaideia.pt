// -----------------------------------------------------------------------
// <copyright file="DonationHistory.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Newtonsoft.Json;

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
            GetsDonatedTotal = donationsOverall.Total;
            GetsDonatedCount = donationsOverall.Count;
            GetsDonatedFirstDate = donationsOverall.FirstDate;
            GetsDonatedFirstDateString = donationsOverall.FirstDate.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Returns donation history rows as JSON for DataTables.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnGetDataTableDataAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return this.Unauthorized();
            }

            var donations = this.context.Donation.GetUserDonationHistory(user.Id);
            var subscriptionsByDonationId = this.context.SubscriptionRepository.GetSubscriptionsByDonationIds(donations.Select(d => d.Id));
            var rows = new List<object>();
            int rowNumber = 1;
            foreach (var item in donations)
            {
                subscriptionsByDonationId.TryGetValue(item.Id, out Subscription subscription);
                rows.Add(this.BuildDonationRow(item, user, rowNumber, subscription));
                rowNumber++;
            }

            return new ContentResult
            {
                Content = JsonConvert.SerializeObject(rows),
                ContentType = "application/json",
                StatusCode = 200,
            };
        }

        private static string FormatDonationDate(DateTime donationDate)
        {
            if (donationDate.Kind == DateTimeKind.Unspecified)
            {
                donationDate = DateTime.SpecifyKind(donationDate, DateTimeKind.Utc);
            }

            return donationDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
        }

        private Dictionary<string, object> BuildDonationRow(Donation item, WebUser user, int rowNumber, Subscription subscription)
        {
            var paymentArray = new List<object>();
            foreach (var payment in item.PaymentList)
            {
                var paymentItem = new Dictionary<string, object>
                {
                    { "PaymentType", this.context.Donation.GetPaymentHumanName(payment).ToString() },
                    { "PaymentItemId", payment.Id },
                };

                if (payment is CreditCardPayment creditCardPayment)
                {
                    if (creditCardPayment.Status != Constants.CreditCardSucceed)
                    {
                        paymentItem.Add("PaymentUrl", creditCardPayment.Url);
                    }

                    paymentItem.Add("PaymentStatus", creditCardPayment.Status);
                }

                paymentArray.Add(paymentItem);
            }

            return new Dictionary<string, object>
            {
                { "Id", rowNumber },
                { "DonationId", item.Id },
                { "DonationDate", FormatDonationDate(item.DonationDate) },
                { "FoodBank", item.FoodBank != null ? item.FoodBank.Name : string.Empty },
                { "DonationAmount", item.DonationAmount },
                { "SubscriptionPublicId", subscription?.PublicId },
                { "PublicId", item.PublicId },
                { "Nif", item.Nif },
                { "UsersNif", user.Nif },
                { "Payments", paymentArray },
                { "PaymentStatus", item.PaymentStatus.ToString() },
            };
        }
    }
}
