// -----------------------------------------------------------------------
// <copyright file="SiteHealthTelemetryScope.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.SiteHealth
{
    /// <summary>
    /// Cloud role filter used when opening an investigate link in Log Analytics.
    /// </summary>
    public enum SiteHealthTelemetryScope
    {
        /// <summary>
        /// Production App Insights cloud role (e.g. alimentaestaideia).
        /// </summary>
        Production = 0,

        /// <summary>
        /// Developer deployment slot cloud role.
        /// </summary>
        Developer = 1,

        /// <summary>
        /// CI or unscoped telemetry (empty AppRoleName or non-production roles).
        /// </summary>
        CiUnscoped = 2,
    }
}
