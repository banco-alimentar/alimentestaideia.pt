// -----------------------------------------------------------------------
// <copyright file="DonationReportCampaignFilterOption.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel.DonationReport
{
    /// <summary>
    /// Campaign entry for client-side report filters.
    /// </summary>
    public class DonationReportCampaignFilterOption
    {
        /// <summary>
        /// Gets or sets the filter key (campaign name or sentinel for active period).
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the display label.
        /// </summary>
        public string Label { get; set; }
    }
}
