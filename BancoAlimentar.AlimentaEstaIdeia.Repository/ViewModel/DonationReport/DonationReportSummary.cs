// -----------------------------------------------------------------------
// <copyright file="DonationReportSummary.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel.DonationReport
{
    /// <summary>
    /// Headline KPI block.
    /// </summary>
    public class DonationReportSummary
    {
        /// <summary>
        /// Gets or sets total paid amount (EUR).
        /// </summary>
        public double TotalPaidAmount { get; set; }

        /// <summary>
        /// Gets or sets count of paid donations.
        /// </summary>
        public int PaidDonationCount { get; set; }

        /// <summary>
        /// Gets or sets count of donations awaiting payment.
        /// </summary>
        public int PendingDonationCount { get; set; }

        /// <summary>
        /// Gets or sets count of failed payment donations.
        /// </summary>
        public int FailedDonationCount { get; set; }

        /// <summary>
        /// Gets or sets average paid ticket (EUR).
        /// </summary>
        public double AveragePaidAmount { get; set; }

        /// <summary>
        /// Gets or sets total product units donated (paid donations).
        /// </summary>
        public long TotalProductUnits { get; set; }

        /// <summary>
        /// Gets or sets estimated food value from catalogue prices (EUR).
        /// </summary>
        public double TotalProductValue { get; set; }

        /// <summary>
        /// Gets or sets share of cash-only donations (%).
        /// </summary>
        public double CashDonationSharePercent { get; set; }

        /// <summary>
        /// Gets or sets payment conversion rate (% paid of all initiated).
        /// </summary>
        public double PaymentConversionPercent { get; set; }

        /// <summary>
        /// Gets or sets distinct food banks receiving donations.
        /// </summary>
        public int ActiveFoodBankCount { get; set; }

        /// <summary>
        /// Gets or sets the largest single paid donation (EUR).
        /// </summary>
        public double MaxSingleDonation { get; set; }
    }
}
