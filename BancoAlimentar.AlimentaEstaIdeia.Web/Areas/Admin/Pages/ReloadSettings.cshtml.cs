// -----------------------------------------------------------------------
// <copyright file="ReloadSettings.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Middleware;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.StaticFileProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Reload tenant and in-memory settings.
    /// </summary>
    [Authorize(Policy = "RoleArea")]
    public class ReloadSettingsModel : PageModel
    {
        private readonly IStringLocalizer<AdminSharedResources> localizer;
        private readonly KeyVaultConfigurationCache keyVaultConfigurationCache;
        private readonly RuntimeCacheInvalidationService invalidation;
        private readonly TenantInitializationService initialization;
        private readonly ITenantStaticLocalCacheService staticCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReloadSettingsModel"/> class.
        /// </summary>
        /// <param name="localizer">Admin shared localizer.</param>
        /// <param name="keyVaultConfigurationCache">Host-wide vault configuration cache.</param>
        /// <param name="invalidation">Durable cache revision coordinator.</param>
        /// <param name="initialization">Tenant initialization status.</param>
        /// <param name="staticCache">Static asset synchronization status.</param>
        public ReloadSettingsModel(
            IStringLocalizer<AdminSharedResources> localizer,
            KeyVaultConfigurationCache keyVaultConfigurationCache,
            RuntimeCacheInvalidationService invalidation,
            TenantInitializationService initialization,
            ITenantStaticLocalCacheService staticCache)
        {
            this.localizer = localizer;
            this.keyVaultConfigurationCache = keyVaultConfigurationCache;
            this.invalidation = invalidation;
            this.initialization = initialization;
            this.staticCache = staticCache;
        }

        /// <summary>
        /// Gets or sets the status message.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>Gets configuration readiness for tenants known to this instance.</summary>
        public IReadOnlyList<TenantConfigurationStatus> ConfigurationStatuses { get; private set; }

        /// <summary>Gets the current tenant's initialization status.</summary>
        public TenantInitializationStatus InitializationStatus { get; private set; }

        /// <summary>Gets the current tenant's static asset synchronization status.</summary>
        public TenantStaticSyncStatus StaticSyncStatus { get; private set; }

        /// <summary>Gets shared invalidation observation status.</summary>
        public RuntimeInvalidationStatus InvalidationStatus { get; private set; }

        /// <summary>Gets the instance-local configuration generation.</summary>
        public System.Guid CacheGeneration => this.keyVaultConfigurationCache.Generation;

        /// <summary>
        /// Execute get operation.
        /// </summary>
        public void OnGet()
        {
            var tenant = this.HttpContext.GetTenant();
            this.ConfigurationStatuses = this.keyVaultConfigurationCache.GetStatuses();
            this.InitializationStatus = this.initialization.GetStatus(tenant.Id);
            this.StaticSyncStatus = this.staticCache.GetStatus(tenant.PublicId);
            this.InvalidationStatus = this.invalidation.GetStatus();
        }

        /// <summary>
        /// Clears runtime caches so tenant settings are reloaded on next request.
        /// </summary>
        /// <returns>The current page.</returns>
        public async Task<IActionResult> OnPostAsync()
        {
            await this.invalidation.PublishAsync(this.HttpContext.RequestAborted);

            this.StatusMessage = this.localizer["SettingsCacheCleared"];
            return this.RedirectToPage();
        }
    }
}
