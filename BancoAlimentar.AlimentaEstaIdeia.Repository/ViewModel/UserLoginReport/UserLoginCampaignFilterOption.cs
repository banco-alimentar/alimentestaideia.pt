// -----------------------------------------------------------------------
// <copyright file="UserLoginCampaignFilterOption.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel.UserLoginReport
{
    /// <summary>
    /// Campaign option for the user login report filter.
    /// </summary>
    public class UserLoginCampaignFilterOption
    {
        /// <summary>
        /// Gets or sets the campaign identifier.
        /// </summary>
        public int CampaignId { get; set; }

        /// <summary>
        /// Gets or sets the campaign label.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Gets or sets the number of registered users in this campaign.
        /// </summary>
        public int RegisteredUserCount { get; set; }

        /// <summary>
        /// Gets or sets the number of logins recorded for this campaign.
        /// </summary>
        public int LoginCount { get; set; }
    }
}
