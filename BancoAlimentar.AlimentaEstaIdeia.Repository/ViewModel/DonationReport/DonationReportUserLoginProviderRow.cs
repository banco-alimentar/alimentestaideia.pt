// -----------------------------------------------------------------------
// <copyright file="DonationReportUserLoginProviderRow.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel.DonationReport
{
    /// <summary>
    /// Login and registration counts for a single authentication provider.
    /// </summary>
    public class DonationReportUserLoginProviderRow
    {
        /// <summary>
        /// Gets or sets the provider key.
        /// </summary>
        public string ProviderKey { get; set; }

        /// <summary>
        /// Gets or sets the display name for the provider.
        /// </summary>
        public string ProviderDisplayName { get; set; }

        /// <summary>
        /// Gets or sets the number of logins for this provider.
        /// </summary>
        public int LoginCount { get; set; }

        /// <summary>
        /// Gets or sets the number of registered users for this provider.
        /// </summary>
        public int RegisteredUserCount { get; set; }
    }
}
