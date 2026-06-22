// -----------------------------------------------------------------------
// <copyright file="SiteHealthAppRoleResolver.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.SiteHealth
{
    using System;

    /// <summary>
    /// Maps the current Azure App Service deployment slot to Application Insights cloud role names.
    /// </summary>
    public static class SiteHealthAppRoleResolver
    {
        /// <summary>
        /// Azure sets <c>WEBSITE_SLOT_NAME</c> on non-production deployment slots.
        /// </summary>
        public const string WebsiteSlotNameVariable = "WEBSITE_SLOT_NAME";

        /// <summary>
        /// Blob path key for the production slot report.
        /// </summary>
        public const string ProductionSlotKey = "production";

        /// <summary>
        /// Blob path key for the developer slot report.
        /// </summary>
        public const string DeveloperSlotKey = "developer";

        /// <summary>
        /// Blob path key for the pre-production slot report.
        /// </summary>
        public const string PreprodSlotKey = "preprod";

        /// <summary>
        /// Resolves the slot from the current process environment.
        /// </summary>
        /// <param name="options">Site health options.</param>
        /// <returns>Resolved slot.</returns>
        public static SiteHealthResolvedSlot ResolveCurrentSlot(SiteHealthReportOptions options)
        {
            string slotName = Environment.GetEnvironmentVariable(WebsiteSlotNameVariable);
            return ResolveFromWebsiteSlotName(slotName, options);
        }

        /// <summary>
        /// Resolves production slot context (for scheduled jobs that must not follow the host slot).
        /// </summary>
        /// <param name="options">Site health options.</param>
        /// <returns>Production slot context.</returns>
        public static SiteHealthResolvedSlot ResolveProduction(SiteHealthReportOptions options)
        {
            return ResolveFromSlotKey(ProductionSlotKey, options);
        }

        /// <summary>
        /// Resolves a slot from its blob path key.
        /// </summary>
        /// <param name="slotKey">Slot key (production, developer, preprod).</param>
        /// <param name="options">Site health options.</param>
        /// <returns>Resolved slot.</returns>
        public static SiteHealthResolvedSlot ResolveFromSlotKey(string slotKey, SiteHealthReportOptions options)
        {
            if (string.Equals(slotKey, ProductionSlotKey, StringComparison.OrdinalIgnoreCase))
            {
                return ResolveFromWebsiteSlotName(null, options);
            }

            if (string.Equals(slotKey, DeveloperSlotKey, StringComparison.OrdinalIgnoreCase))
            {
                return ResolveFromWebsiteSlotName(DeveloperSlotKey, options);
            }

            if (string.Equals(slotKey, PreprodSlotKey, StringComparison.OrdinalIgnoreCase))
            {
                return ResolveFromWebsiteSlotName(PreprodSlotKey, options);
            }

            return new SiteHealthResolvedSlot
            {
                SlotKey = slotKey,
                DisplayLabel = slotKey,
                AppRoleName = $"{options.ProductionAppRoleName}-{slotKey}",
            };
        }

        /// <summary>
        /// Resolves slot context from an Azure <c>WEBSITE_SLOT_NAME</c> value.
        /// </summary>
        /// <param name="websiteSlotName">Slot name from Azure or null/Production for production.</param>
        /// <param name="options">Site health options.</param>
        /// <returns>Resolved slot.</returns>
        public static SiteHealthResolvedSlot ResolveFromWebsiteSlotName(string websiteSlotName, SiteHealthReportOptions options)
        {
            if (string.IsNullOrWhiteSpace(websiteSlotName) ||
                string.Equals(websiteSlotName, "Production", StringComparison.OrdinalIgnoreCase))
            {
                return new SiteHealthResolvedSlot
                {
                    SlotKey = ProductionSlotKey,
                    DisplayLabel = "Production",
                    AppRoleName = options.ProductionAppRoleName,
                };
            }

            if (string.Equals(websiteSlotName, DeveloperSlotKey, StringComparison.OrdinalIgnoreCase))
            {
                return new SiteHealthResolvedSlot
                {
                    SlotKey = DeveloperSlotKey,
                    DisplayLabel = "Developer",
                    AppRoleName = options.DeveloperAppRoleName,
                };
            }

            if (string.Equals(websiteSlotName, PreprodSlotKey, StringComparison.OrdinalIgnoreCase))
            {
                return new SiteHealthResolvedSlot
                {
                    SlotKey = PreprodSlotKey,
                    DisplayLabel = "Pre-production",
                    AppRoleName = options.PreprodAppRoleName,
                };
            }

            return new SiteHealthResolvedSlot
            {
                SlotKey = websiteSlotName.ToLowerInvariant(),
                DisplayLabel = websiteSlotName,
                AppRoleName = $"{options.ProductionAppRoleName}-{websiteSlotName}",
            };
        }
    }
}
