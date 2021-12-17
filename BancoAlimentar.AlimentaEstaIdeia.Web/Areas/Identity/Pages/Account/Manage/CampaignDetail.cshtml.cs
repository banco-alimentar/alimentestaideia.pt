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

        public List<Donation> PaidDonations { get { return Donations.Where(d => d.PaymentStatus == PaymentStatus.Payed).ToList(); } }
        public int PaidDonationsTotal { get { return PaidDonations.Count(); } }
        public decimal PaidDonationsTotalPercentage { get { return (PaidDonationsTotal / TotalDonations) * 100; } }
        public double TotalPaidDonationsAmount { get { return this.PaidDonations.Sum(x => x.DonationAmount); } }


        public List<Donation> PendingDonations { get { return Donations.Where(d => d.PaymentStatus == PaymentStatus.WaitingPayment).ToList(); } }
        public int PendingDonationsTotal { get { return PendingDonations.Count(); } }
        public decimal PendingDonationsTotalPercentage { get { return (PendingDonationsTotal / TotalDonations) * 100; } }
        public double TotalPendingDonationsAmount { get { return this.PendingDonations.Sum(x => x.DonationAmount); } }


        public List<Donation> NotPaidDonations { get { return Donations.Where(d => d.PaymentStatus == PaymentStatus.NotPayed).ToList(); } }
        public int NotPaidDonationsTotal { get { return NotPaidDonations.Count(); } }
        public decimal NotPaidDonationsTotalPercentage { get { return (NotPaidDonationsTotal / TotalDonations) * 100; } }
        public double TotalNotPaidDonationsAmount { get { return this.NotPaidDonations.Sum(x => x.DonationAmount); } }

        
        public List<Donation> PaymentErrorDonations { get { return Donations.Where(d => d.PaymentStatus == PaymentStatus.ErrorPayment).ToList(); } }
        public int PaymentErrorDonationsTotal { get { return PaymentErrorDonations.Count(); } }
        public decimal PaymentErrorDonationsTotalPercentage { get { return (PaymentErrorDonationsTotal / TotalDonations) * 100; } }
        public double TotalPaymentErrorDonationsAmount { get { return this.PaymentErrorDonations.Sum(x => x.DonationAmount); } }


        public int FailedDonationsTotal { get { return PaymentErrorDonationsTotal + NotPaidDonationsTotal; } }
        public decimal FailedDonationsTotalPercentage { get { return (FailedDonationsTotal / TotalDonations) * 100; } }  
        public double TotalFailedDonationsAmount { get { return this.PaymentErrorDonations.Sum(x => x.DonationAmount) + this.NotPaidDonations.Sum(x => x.DonationAmount); } }


        public List<Donation> Donations { get; set;}
        public int TotalDonations { get { return Donations.Count(); } }

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
