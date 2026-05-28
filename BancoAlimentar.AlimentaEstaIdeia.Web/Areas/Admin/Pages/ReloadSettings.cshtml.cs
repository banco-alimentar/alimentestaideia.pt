// -----------------------------------------------------------------------
// <copyright file="ReloadSettings.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages
{
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Caching.Memory;

    /// <summary>
    /// Reload tenant and in-memory settings.
    /// </summary>
    public class ReloadSettingsModel : PageModel
    {
        private readonly IMemoryCache memoryCache;
        private readonly InMemoryCacheService tenantConfigurationCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReloadSettingsModel"/> class.
        /// </summary>
        /// <param name="memoryCache">App-level memory cache.</param>
        /// <param name="tenantConfigurationCache">Tenant configuration cache service.</param>
        public ReloadSettingsModel(
            IMemoryCache memoryCache,
            InMemoryCacheService tenantConfigurationCache)
        {
            this.memoryCache = memoryCache;
            this.tenantConfigurationCache = tenantConfigurationCache;
        }

        /// <summary>
        /// Gets or sets the status message.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        /// Execute get operation.
        /// </summary>
        public void OnGet()
        {
        }

        /// <summary>
        /// Clears runtime caches so tenant settings are reloaded on next request.
        /// </summary>
        /// <returns>The current page.</returns>
        public IActionResult OnPost()
        {
            this.tenantConfigurationCache.Clear();
            if (this.memoryCache is MemoryCache cache)
            {
                cache.Compact(1.0);
            }

            this.StatusMessage = "In-memory settings cache cleared. Tenant settings will reload on next request.";
            return this.RedirectToPage();
        }
    }
}
