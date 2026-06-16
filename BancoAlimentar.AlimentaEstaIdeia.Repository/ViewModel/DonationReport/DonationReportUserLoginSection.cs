// -----------------------------------------------------------------------
// <copyright file="DonationReportUserLoginSection.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel.DonationReport
{
    using System.Collections.Generic;

    /// <summary>
    /// User authentication analytics for static donation reports.
    /// </summary>
    public class DonationReportUserLoginSection
    {
        /// <summary>
        /// Gets or sets the total number of recorded logins.
        /// </summary>
        public int TotalLogins { get; set; }

        /// <summary>
        /// Gets or sets the total number of registered users.
        /// </summary>
        public int TotalRegisteredUsers { get; set; }

        /// <summary>
        /// Gets or sets per-provider breakdown rows.
        /// </summary>
        public IList<DonationReportUserLoginProviderRow> Providers { get; set; } =
            new List<DonationReportUserLoginProviderRow>();
    }
}
