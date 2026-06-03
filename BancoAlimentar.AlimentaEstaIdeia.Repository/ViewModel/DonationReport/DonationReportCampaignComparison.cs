// -----------------------------------------------------------------------
// <copyright file="DonationReportCampaignComparison.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel.DonationReport
{
    using System.Collections.Generic;

    /// <summary>
    /// Cross-campaign comparison matrix for evolution charts.
    /// </summary>
    public class DonationReportCampaignComparison
    {
        /// <summary>
        /// Gets or sets campaign column labels (chronological).
        /// </summary>
        public IList<string> CampaignLabels { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets paid amount (€) per food bank across campaigns.
        /// </summary>
        public IList<DonationReportSeriesRow> FoodBankAmountSeries { get; set; } = new List<DonationReportSeriesRow>();

        /// <summary>
        /// Gets or sets product units per product across campaigns.
        /// </summary>
        public IList<DonationReportSeriesRow> ProductUnitSeries { get; set; } = new List<DonationReportSeriesRow>();

        /// <summary>
        /// Gets or sets total paid amount per campaign column.
        /// </summary>
        public IList<double> CampaignTotals { get; set; } = new List<double>();
    }
}
