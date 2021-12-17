// -----------------------------------------------------------------------
// <copyright file="CampaignDetail.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc.Localization;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// Campaign details.
    /// </summary>
    public class CampaignDetailModel : PageModel
    {
        private readonly UserManager<WebUser> userManager;
        private readonly IUnitOfWork context;
        private readonly IHtmlLocalizer<IdentitySharedResources> localizer;


        /// <summary>
        /// Initializes a new instance of the <see cref="CampaignDetailModel"/> class.
        /// </summary>
        /// <param name="userManager">User manager.</param>
        /// <param name="context">Unit of work.</param>
        public CampaignDetailModel(
            UserManager<WebUser> userManager,
            IUnitOfWork context,
            IHtmlLocalizer<IdentitySharedResources> localizer)
        {
            this.userManager = userManager;
            this.context = context;
            this.localizer = localizer;
        }

        /// <summary>
        /// Gets or sets the Referral.
        /// </summary>
        public Referral Referral { get; set; }

        /// <summary>
        /// Gets Paid Donations.
        /// </summary>
        public List<Donation> PaidDonations { get { return Donations.Where(d => d.PaymentStatus == PaymentStatus.Payed).ToList(); } }
        
        /// <summary>
        /// Gets Paid Donations total.
        /// </summary>
        public int PaidDonationsTotal { get { return PaidDonations.Count(); } }
        
        /// <summary>
        /// Gets Paid Donations total percentage.
        /// </summary>
        public decimal PaidDonationsTotalPercentage { get { return (PaidDonationsTotal / TotalDonations) * 100; } }
        
        /// <summary>
        /// Gets Paid Donations amount.
        /// </summary>
        public double TotalPaidDonationsAmount { get { return this.PaidDonations.Sum(x => x.DonationAmount); } }

        /// <summary>
        /// Gets Pending Donations.
        /// </summary>
        public List<Donation> PendingDonations { get { return Donations.Where(d => d.PaymentStatus == PaymentStatus.WaitingPayment).ToList(); } }
        
        /// <summary>
        /// Gets Pending Donations total.
        /// </summary>
        public int PendingDonationsTotal { get { return PendingDonations.Count(); } }
        
        /// <summary>
        /// Gets Pending Donations total percentage.
        /// </summary>
        public decimal PendingDonationsTotalPercentage { get { return (PendingDonationsTotal / TotalDonations) * 100; } }
        
        /// <summary>
        /// Gets Pending Donations total amount.
        /// </summary>
        public double TotalPendingDonationsAmount { get { return this.PendingDonations.Sum(x => x.DonationAmount); } }

        /// <summary>
        /// Gets not paid Donations.
        /// </summary>
        public List<Donation> NotPaidDonations { get { return Donations.Where(d => d.PaymentStatus == PaymentStatus.NotPayed).ToList(); } }
        
        /// <summary>
        /// Gets not paid Donations total.
        /// </summary>
        public int NotPaidDonationsTotal { get { return NotPaidDonations.Count(); } }
        
        /// <summary>
        /// Gets not paid Donations total percentage.
        /// </summary>
        public decimal NotPaidDonationsTotalPercentage { get { return (NotPaidDonationsTotal / TotalDonations) * 100; } }
        
        /// <summary>
        /// Gets not paid Donations total amount.
        /// </summary>
        public double TotalNotPaidDonationsAmount { get { return this.NotPaidDonations.Sum(x => x.DonationAmount); } }

        /// <summary>
        /// Gets payment error Donations.
        /// </summary>
        public List<Donation> PaymentErrorDonations { get { return Donations.Where(d => d.PaymentStatus == PaymentStatus.ErrorPayment).ToList(); } }
        
        /// <summary>
        /// Gets payment error Donations total.
        /// </summary>
        public int PaymentErrorDonationsTotal { get { return PaymentErrorDonations.Count(); } }
        
        /// <summary>
        /// Gets payment error Donations total percentage.
        /// </summary>
        public decimal PaymentErrorDonationsTotalPercentage { get { return (PaymentErrorDonationsTotal / TotalDonations) * 100; } }
        
        /// <summary>
        /// Gets payment error Donations total amount.
        /// </summary>
        public double TotalPaymentErrorDonationsAmount { get { return this.PaymentErrorDonations.Sum(x => x.DonationAmount); } }

        /// <summary>
        /// Gets failed Donations total.
        /// </summary>
        public int FailedDonationsTotal { get { return PaymentErrorDonationsTotal + NotPaidDonationsTotal; } }
        
        /// <summary>
        /// Gets failed Donations total percentage.
        /// </summary>
        public decimal FailedDonationsTotalPercentage { get { return (FailedDonationsTotal / TotalDonations) * 100; } }  
        
        /// <summary>
        /// Gets failed Donations total amount.
        /// </summary>
        public double TotalFailedDonationsAmount { get { return this.PaymentErrorDonations.Sum(x => x.DonationAmount) + this.NotPaidDonations.Sum(x => x.DonationAmount); } }

        /// <summary>
        /// Gets or sets Donations.
        /// </summary>
        public List<Donation> Donations { get; set;}

        /// <summary>
        /// Gets total Donations.
        /// </summary>
        public int TotalDonations { get { return Donations.Count(); } }

        /// <summary>
        /// Gets latest Donations date.
        /// </summary>
        public DateTime LatestPaidDonationDate { get { return PaidDonations.OrderByDescending(x => x.DonationDate).First().DonationDate; } }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <param name="id">Referral id.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task OnGet(int id)
        {
            var user = await userManager.GetUserAsync(User);

            this.Referral = this.context.ReferralRepository.GetFullReferral(user?.Id, id);
            this.Donations = (Referral?.Donations != null ? Referral.Donations.ToList() : new List<Donation>());
        }
    }
}
