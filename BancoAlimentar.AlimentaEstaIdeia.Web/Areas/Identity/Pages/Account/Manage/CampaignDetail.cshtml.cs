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
    using System.Text.Json;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
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
        private readonly ReferralQrCodeService referralQrCodeService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CampaignDetailModel"/> class.
        /// </summary>
        /// <param name="userManager">User manager.</param>
        /// <param name="context">Unit of work.</param>
        /// <param name="localizer">Page <paramref name="localizer"/>.</param>
        /// <param name="referralQrCodeService">Referral QR code service.</param>
        public CampaignDetailModel(
            UserManager<WebUser> userManager,
            IUnitOfWork context,
            IHtmlLocalizer<IdentitySharedResources> localizer,
            ReferralQrCodeService referralQrCodeService)
        {
            this.userManager = userManager;
            this.context = context;
            this.localizer = localizer;
            this.referralQrCodeService = referralQrCodeService;
        }

        /// <summary>
        /// Gets or sets the Referral.
        /// </summary>
        public Referral Referral { get; set; }

        /// <summary>
        /// Gets Paid Donations.
        /// </summary>
        public List<Donation> PaidDonations
        {
            get { return Donations.Where(d => d.PaymentStatus == PaymentStatus.Payed).ToList(); }
        }

        /// <summary>
        /// Gets Paid Donations total.
        /// </summary>
        public int PaidDonationsTotal
        {
            get { return PaidDonations.Count(); }
        }

        /// <summary>
        /// Gets Paid Donations total percentage.
        /// </summary>
        public decimal PaidDonationsTotalPercentage
        {
            get { return (PaidDonationsTotal / TotalDonations) * 100; }
        }

        /// <summary>
        /// Gets Paid Donations amount.
        /// </summary>
        public double TotalPaidDonationsAmount
        {
            get { return Math.Round(this.PaidDonations.Sum(x => x.DonationAmount), 2); }
        }

        /// <summary>
        /// Gets Pending Donations.
        /// </summary>
        public List<Donation> PendingDonations
        {
            get { return Donations.Where(d => d.PaymentStatus == PaymentStatus.WaitingPayment).ToList(); }
        }

        /// <summary>
        /// Gets Pending Donations total.
        /// </summary>
        public int PendingDonationsTotal
        {
            get { return PendingDonations.Count(); }
        }

        /// <summary>
        /// Gets Pending Donations total percentage.
        /// </summary>
        public decimal PendingDonationsTotalPercentage
        {
            get { return (PendingDonationsTotal / TotalDonations) * 100; }
        }

        /// <summary>
        /// Gets Pending Donations total amount.
        /// </summary>
        public double TotalPendingDonationsAmount
        {
            get { return Math.Round(this.PendingDonations.Sum(x => x.DonationAmount), 2); }
        }

        /// <summary>
        /// Gets not paid Donations.
        /// </summary>
        public List<Donation> NotPaidDonations
        {
            get
            {
                return Donations.Where(d => d.PaymentStatus == PaymentStatus.NotPayed).ToList();
            }
        }

        /// <summary>
        /// Gets not paid Donations total.
        /// </summary>
        public int NotPaidDonationsTotal
        {
            get
            {
                return NotPaidDonations.Count();
            }
        }

        /// <summary>
        /// Gets not paid Donations total percentage.
        /// </summary>
        public decimal NotPaidDonationsTotalPercentage
        {
            get
            {
                return (NotPaidDonationsTotal / TotalDonations) * 100;
            }
        }

        /// <summary>
        /// Gets not paid Donations total amount.
        /// </summary>
        public double TotalNotPaidDonationsAmount
        {
            get
            {
                return Math.Round(this.NotPaidDonations.Sum(x => x.DonationAmount), 2);
            }
        }

        /// <summary>
        /// Gets payment error Donations.
        /// </summary>
        public List<Donation> PaymentErrorDonations
        {
            get
            {
                return Donations.Where(d => d.PaymentStatus == PaymentStatus.ErrorPayment).ToList();
            }
        }

        /// <summary>
        /// Gets payment error Donations total.
        /// </summary>
        public int PaymentErrorDonationsTotal
        {
            get
            {
                return PaymentErrorDonations.Count();
            }
        }

        /// <summary>
        /// Gets payment error Donations total percentage.
        /// </summary>
        public decimal PaymentErrorDonationsTotalPercentage
        {
            get
            {
                return (PaymentErrorDonationsTotal / TotalDonations) * 100;
            }
        }

        /// <summary>
        /// Gets payment error Donations total amount.
        /// </summary>
        public double TotalPaymentErrorDonationsAmount
        {
            get
            {
                return Math.Round(this.PaymentErrorDonations.Sum(x => x.DonationAmount), 2);
            }
        }

        /// <summary>
        /// Gets failed Donations total.
        /// </summary>
        public int FailedDonationsTotal
        {
            get
            {
                return PaymentErrorDonationsTotal + NotPaidDonationsTotal;
            }
        }

        /// <summary>
        /// Gets failed Donations total percentage.
        /// </summary>
        public decimal FailedDonationsTotalPercentage
        {
            get
            {
                return (FailedDonationsTotal / TotalDonations) * 100;
            }
        }

        /// <summary>
        /// Gets failed Donations total amount.
        /// </summary>
        public double TotalFailedDonationsAmount
        {
            get
            {
                return Math.Round(this.PaymentErrorDonations.Sum(x => x.DonationAmount) + this.NotPaidDonations.Sum(x => x.DonationAmount), 2);
            }
        }

        /// <summary>
        /// Gets or sets Donations.
        /// </summary>
        public List<Donation> Donations { get; set; }

        /// <summary>
        /// Gets JSON payload for the paid donation evolution chart.
        /// </summary>
        public string DonationEvolutionJson { get; private set; } = "{\"labels\":[],\"amounts\":[]}";

        /// <summary>
        /// Gets a value indicating whether the donation evolution chart has data.
        /// </summary>
        public bool HasDonationEvolutionData { get; private set; }

        /// <summary>
        /// Gets the full donation URL for this referral campaign.
        /// </summary>
        public string ReferralDonationUrl { get; private set; }

        /// <summary>
        /// Gets the QR code data URI for the referral donation link.
        /// </summary>
        public string ReferralQrCodeDataUri { get; private set; }

        /// <summary>
        /// Gets total Donations.
        /// </summary>
        public int TotalDonations
        {
            get
            {
                return Donations.Count();
            }
        }

        /// <summary>
        /// Gets latest Donations date.
        /// </summary>
        public DateTime? LatestPaidDonationDate
        {
            get
            {
                if (PaidDonations.Count > 0)
                {
                    return PaidDonations.OrderByDescending(x => x.DonationDate).First().DonationDate;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <param name="id">Referral id.</param>
        /// <returns>A <see cref="IActionResult"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnGetAsync(int id)
        {
            var user = await userManager.GetUserAsync(User);
            this.Referral = this.context.ReferralRepository.GetFullReferral(user?.Id, id);
            if (this.Referral == null)
            {
                return this.RedirectToPage("CampaignsHistory");
            }

            this.Donations = Referral?.Donations != null ? Referral.Donations.ToList() : new List<Donation>();
            this.BuildDonationEvolutionChart();
            this.ReferralDonationUrl = $"{Request.Scheme}://{Request.Host.Value}{Url.Content($"~/Referral/{Referral.Code}")}";
            this.ReferralQrCodeDataUri = this.referralQrCodeService.CreateDataUri(this.ReferralDonationUrl);
            return Page();
        }

        private void BuildDonationEvolutionChart()
        {
            var labels = new List<string>();
            var amounts = new List<double>();
            double cumulativeAmount = 0;

            foreach (var dayGroup in this.PaidDonations
                .GroupBy(d => d.DonationDate.Date)
                .OrderBy(g => g.Key))
            {
                cumulativeAmount += dayGroup.Sum(d => d.DonationAmount);
                labels.Add(dayGroup.Key.ToString("yyyy-MM-dd"));
                amounts.Add(Math.Round(cumulativeAmount, 2));
            }

            this.HasDonationEvolutionData = labels.Count > 0;
            this.DonationEvolutionJson = JsonSerializer.Serialize(new
            {
                labels,
                amounts,
            });
        }
    }
}