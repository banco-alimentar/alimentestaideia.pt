// -----------------------------------------------------------------------
// <copyright file="UserLoginReportSnapshot.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel.UserLoginReport
{
    using System.Collections.Generic;

    /// <summary>
    /// Aggregated user login report data.
    /// </summary>
    public class UserLoginReportSnapshot
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
        public IList<UserLoginProviderRow> Providers { get; set; } = new List<UserLoginProviderRow>();
    }
}
