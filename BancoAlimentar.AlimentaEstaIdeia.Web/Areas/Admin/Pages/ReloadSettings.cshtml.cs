// -----------------------------------------------------------------------
// <copyright file="ReloadSettings.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages
{
    using System;
    using System.Collections.Generic;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Authentication;
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Reload tenant and in-memory settings.
    /// </summary>
    [Authorize(Policy = "RoleArea")]
    public class ReloadSettingsModel : PageModel
    {
        private readonly IMemoryCache memoryCache;
        private readonly InMemoryCacheService tenantConfigurationCache;
        private readonly IStringLocalizer<AdminSharedResources> localizer;
        private readonly IEnumerable<ITenantAuthenticationOptionsCache> authenticationOptionsCaches;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReloadSettingsModel"/> class.
        /// </summary>
        /// <param name="memoryCache">App-level memory cache.</param>
        /// <param name="tenantConfigurationCache">Tenant configuration cache service.</param>
        /// <param name="localizer">Admin shared localizer.</param>
        public ReloadSettingsModel(
            IMemoryCache memoryCache,
            InMemoryCacheService tenantConfigurationCache,
            IStringLocalizer<AdminSharedResources> localizer)
            : this(memoryCache, tenantConfigurationCache, localizer, Array.Empty<ITenantAuthenticationOptionsCache>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReloadSettingsModel"/> class.
        /// </summary>
        /// <param name="memoryCache">App-level memory cache.</param>
        /// <param name="tenantConfigurationCache">Tenant configuration cache.</param>
        /// <param name="localizer">Admin shared localizer.</param>
        /// <param name="authenticationOptionsCaches">Tenant authentication caches.</param>
        [ActivatorUtilitiesConstructor]
        public ReloadSettingsModel(
            IMemoryCache memoryCache,
            InMemoryCacheService tenantConfigurationCache,
            IStringLocalizer<AdminSharedResources> localizer,
            IEnumerable<ITenantAuthenticationOptionsCache> authenticationOptionsCaches)
        {
            this.memoryCache = memoryCache;
            this.tenantConfigurationCache = tenantConfigurationCache;
            this.localizer = localizer;
            this.authenticationOptionsCaches = authenticationOptionsCaches;
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
            foreach (ITenantAuthenticationOptionsCache authenticationCache in this.authenticationOptionsCaches)
            {
                authenticationCache.Clear();
            }

            if (this.memoryCache is MemoryCache cache)
            {
                cache.Compact(1.0);
            }

            this.StatusMessage = this.localizer["SettingsCacheCleared"];
            return this.RedirectToPage();
        }
    }
}
