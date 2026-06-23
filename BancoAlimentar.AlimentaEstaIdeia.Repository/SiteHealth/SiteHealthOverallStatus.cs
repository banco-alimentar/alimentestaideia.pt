// -----------------------------------------------------------------------
// <copyright file="SiteHealthOverallStatus.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.SiteHealth
{
    /// <summary>
    /// Overall health assessment for a reporting window.
    /// </summary>
    public enum SiteHealthOverallStatus
    {
        /// <summary>
        /// No significant production issues detected.
        /// </summary>
        Healthy = 0,

        /// <summary>
        /// Some warnings that should be reviewed.
        /// </summary>
        Attention = 1,

        /// <summary>
        /// Critical signals that may affect donors or payments.
        /// </summary>
        Critical = 2,
    }
}
