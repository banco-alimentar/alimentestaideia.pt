// -----------------------------------------------------------------------
// <copyright file="DonationReportDonorCampaignFoodBankRow.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel.DonationReport
{
    /// <summary>
    /// Distinct donor count for a campaign and food bank combination.
    /// </summary>
    public class DonationReportDonorCampaignFoodBankRow
    {
        /// <summary>
        /// Gets or sets the campaign key.
        /// </summary>
        public string CampaignKey { get; set; }

        /// <summary>
        /// Gets or sets the campaign display name.
        /// </summary>
        public string CampaignName { get; set; }

        /// <summary>
        /// Gets or sets the food bank identifier.
        /// </summary>
        public int FoodBankId { get; set; }

        /// <summary>
        /// Gets or sets the food bank display name.
        /// </summary>
        public string FoodBankName { get; set; }

        /// <summary>
        /// Gets or sets the number of distinct donors with paid donations.
        /// </summary>
        public int DistinctDonorCount { get; set; }
    }
}
