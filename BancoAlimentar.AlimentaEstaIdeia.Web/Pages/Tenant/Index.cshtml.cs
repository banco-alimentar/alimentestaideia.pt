// -----------------------------------------------------------------------
// <copyright file="Index.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages.Tenant
{
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Layout;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Default index page for tenant.
    /// </summary>
    // [Authorize(Roles = "SuperAdmin")]
    public class IndexModel : PageModel
    {
        private readonly IConfiguration configuration;
        private readonly ITenantLayout tenantLayout;
        private readonly IWebHostEnvironment environment;

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexModel"/> class.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="tenantLayout">Tenant Layout.</param>
        /// <param name="environment">WebHostEnvironment.</param>
        public IndexModel(
            IConfiguration configuration,
            ITenantLayout tenantLayout,
            IWebHostEnvironment environment)
        {
            this.configuration = configuration;
            this.tenantLayout = tenantLayout;
            this.environment = environment;
        }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <returns>The data to display.</returns>
        public IActionResult OnGet()
        {
            Tenant tenant = this.HttpContext.GetTenant();
            JObject result = new JObject();
            if (tenant != null)
            {
                result["Tenant"] = JObject.FromObject(tenant);
            }
            else
            {
                result["Error"] = "Tenant is null";
            }

            if (this.configuration["Tenant-Name"] != null)
            {
                result["Tenant-Name"] = this.configuration["Tenant-Name"];
            }

            result["Layout"] = this.tenantLayout.Layout;
            result["AdminLayout"] = this.tenantLayout.AdminLayout;
            result["DebugLayout"] = this.tenantLayout.Debug;
            result["Environment"] = this.environment.EnvironmentName;

            return this.Content(result.ToString(), "application/json");
        }
    }
}
