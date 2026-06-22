// -----------------------------------------------------------------------
// <copyright file="SiteHealthReportSeverity.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.SiteHealth
{
    /// <summary>
    /// Severity of a site health signal.
    /// </summary>
    public enum SiteHealthReportSeverity
    {
        /// <summary>
        /// Informational only; no production action required.
        /// </summary>
        Info = 0,

        /// <summary>
        /// Worth investigating when sustained or increasing.
        /// </summary>
        Warning = 1,

        /// <summary>
        /// Likely user or payment impact; investigate promptly.
        /// </summary>
        Critical = 2,
    }
}
