// -----------------------------------------------------------------------
// <copyright file="SiteHealthResolvedSlot.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.SiteHealth
{
    /// <summary>
    /// Resolved deployment slot context for site health reporting.
    /// </summary>
    public sealed class SiteHealthResolvedSlot
    {
        /// <summary>
        /// Gets or sets the stable blob path key (production, developer, preprod, …).
        /// </summary>
        public string SlotKey { get; set; }

        /// <summary>
        /// Gets or sets the human-readable slot label.
        /// </summary>
        public string DisplayLabel { get; set; }

        /// <summary>
        /// Gets or sets the Application Insights AppRoleName filter.
        /// </summary>
        public string AppRoleName { get; set; }
    }
}
