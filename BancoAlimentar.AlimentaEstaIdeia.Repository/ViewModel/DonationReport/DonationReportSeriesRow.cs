// -----------------------------------------------------------------------
// <copyright file="DonationReportSeriesRow.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel.DonationReport
{
    using System.Collections.Generic;

    /// <summary>
    /// A labelled series of values aligned to campaign columns (for evolution charts).
    /// </summary>
    public class DonationReportSeriesRow
    {
        /// <summary>
        /// Gets or sets the series label (food bank or product name).
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Gets or sets numeric values per campaign (same order as <see cref="DonationReportCampaignComparison.CampaignLabels"/>).
        /// </summary>
        public IList<double> Values { get; set; } = new List<double>();
    }
}
