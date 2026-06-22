// -----------------------------------------------------------------------
// <copyright file="SiteHealthReportPaths.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.SiteHealth
{
    /// <summary>
    /// Blob paths for persisted site health artifacts.
    /// </summary>
    public static class SiteHealthReportPaths
    {
        /// <summary>
        /// Latest report JSON blob name.
        /// </summary>
        public const string ReportBlobName = "latest/report.json";

        /// <summary>
        /// Generation status JSON blob name.
        /// </summary>
        public const string GenerationStatusBlobName = "latest/generation-status.json";
    }
}
