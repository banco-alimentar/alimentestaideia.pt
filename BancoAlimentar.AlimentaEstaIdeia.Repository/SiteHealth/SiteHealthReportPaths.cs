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
        /// Legacy report JSON blob name (production-only reports before per-slot paths).
        /// </summary>
        public const string LegacyReportBlobName = "latest/report.json";

        /// <summary>
        /// Legacy generation status JSON blob name.
        /// </summary>
        public const string LegacyGenerationStatusBlobName = "latest/generation-status.json";

        /// <summary>
        /// Returns the report blob path for a deployment slot.
        /// </summary>
        /// <param name="slotKey">Slot key (production, developer, preprod).</param>
        /// <returns>Blob name.</returns>
        public static string GetReportBlobName(string slotKey)
        {
            return $"latest/{NormalizeSlotKey(slotKey)}/report.json";
        }

        /// <summary>
        /// Returns the generation status blob path for a deployment slot.
        /// </summary>
        /// <param name="slotKey">Slot key (production, developer, preprod).</param>
        /// <returns>Blob name.</returns>
        public static string GetGenerationStatusBlobName(string slotKey)
        {
            return $"latest/{NormalizeSlotKey(slotKey)}/generation-status.json";
        }

        private static string NormalizeSlotKey(string slotKey)
        {
            return string.IsNullOrWhiteSpace(slotKey)
                ? SiteHealthAppRoleResolver.ProductionSlotKey
                : slotKey.ToLowerInvariant();
        }
    }
}
