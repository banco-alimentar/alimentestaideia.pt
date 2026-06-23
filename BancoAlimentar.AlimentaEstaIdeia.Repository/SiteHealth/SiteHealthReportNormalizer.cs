// -----------------------------------------------------------------------
// <copyright file="SiteHealthReportNormalizer.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.SiteHealth
{
    /// <summary>
    /// Normalizes persisted site health report metadata.
    /// </summary>
    public static class SiteHealthReportNormalizer
    {
        /// <summary>
        /// Fills missing slot metadata on reports loaded from legacy blobs.
        /// </summary>
        /// <param name="report">Report to normalize.</param>
        /// <returns>Normalized report or null.</returns>
        public static SiteHealthReport Normalize(SiteHealthReport report)
        {
            if (report == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(report.SlotKey))
            {
                report.SlotKey = SiteHealthAppRoleResolver.ProductionSlotKey;
            }

            if (string.IsNullOrWhiteSpace(report.SlotLabel))
            {
                report.SlotLabel = "Production";
            }

            if (string.IsNullOrWhiteSpace(report.AppRoleName) &&
                !string.IsNullOrWhiteSpace(report.ProductionAppRoleName))
            {
                report.AppRoleName = report.ProductionAppRoleName;
            }

            return report;
        }
    }
}
