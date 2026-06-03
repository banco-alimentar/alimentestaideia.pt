// -----------------------------------------------------------------------
// <copyright file="ClearTenantStaticCache.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages
{
    using System;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.StaticFileProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Admin action to clear and optionally refresh the tenant static file local cache.
    /// </summary>
    public class ClearTenantStaticCacheModel : PageModel
    {
        private readonly ITenantStaticLocalCacheService localCacheService;
        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClearTenantStaticCacheModel"/> class.
        /// </summary>
        /// <param name="localCacheService">Tenant static local cache service.</param>
        /// <param name="configuration">Tenant configuration.</param>
        public ClearTenantStaticCacheModel(
            ITenantStaticLocalCacheService localCacheService,
            IConfiguration configuration)
        {
            this.localCacheService = localCacheService;
            this.configuration = configuration;
        }

        /// <summary>
        /// Gets or sets the success status message.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        /// Gets or sets the error status message.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the last operation result.
        /// </summary>
        public TenantStaticLocalCacheResult LastResult { get; set; }

        /// <summary>
        /// Execute get operation.
        /// </summary>
        public void OnGet()
        {
        }

        /// <summary>
        /// Clears the local tenant static cache for the current tenant.
        /// </summary>
        /// <returns>The current page.</returns>
        public IActionResult OnPostClear()
        {
            return this.ExecuteClear();
        }

        /// <summary>
        /// Clears the local cache and downloads the latest static files from blob storage.
        /// </summary>
        /// <returns>The current page.</returns>
        public async Task<IActionResult> OnPostClearAndResyncAsync()
        {
            return await this.ExecuteClearAndResyncAsync();
        }

        private IActionResult ExecuteClear()
        {
            try
            {
                Tenant tenant = this.GetCurrentTenant();
                this.LastResult = this.localCacheService.Clear(tenant.PublicId);
                this.StatusMessage = this.LastResult.FilesRemoved == 0
                    ? "No local static cache files were found for this tenant."
                    : $"Removed {this.LastResult.FilesRemoved} cached file(s). Static requests will read from blob storage until the cache is rebuilt.";
                return this.Page();
            }
            catch (Exception ex)
            {
                this.ErrorMessage = "Failed to clear tenant static cache: " + ex.Message;
                return this.Page();
            }
        }

        private async Task<IActionResult> ExecuteClearAndResyncAsync()
        {
            try
            {
                Tenant tenant = this.GetCurrentTenant();
                this.LastResult = this.localCacheService.Clear(tenant.PublicId);
                TenantStaticLocalCacheResult resyncResult = await this.localCacheService.ResyncFromBlobAsync(
                    tenant.PublicId,
                    tenant.NormalizedName,
                    this.configuration["AzureStorage:ConnectionString"],
                    onlyIfSizeChanged: false);
                this.LastResult.FilesDownloaded = resyncResult.FilesDownloaded;
                this.StatusMessage =
                    $"Removed {this.LastResult.FilesRemoved} cached file(s) and downloaded {this.LastResult.FilesDownloaded} file(s) from blob storage.";
                return this.Page();
            }
            catch (Exception ex)
            {
                this.ErrorMessage = "Failed to refresh tenant static cache: " + ex.Message;
                return this.Page();
            }
        }

        private Tenant GetCurrentTenant()
        {
            Tenant tenant = this.HttpContext.GetTenant();
            if (tenant == null)
            {
                throw new InvalidOperationException("Current tenant is not available.");
            }

            return tenant;
        }
    }
}
