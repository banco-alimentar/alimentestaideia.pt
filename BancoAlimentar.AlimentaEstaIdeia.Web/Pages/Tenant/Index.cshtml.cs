// -----------------------------------------------------------------------
// <copyright file="Index.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages.Tenant
{
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Default index page for tenant.
    /// </summary>
    [Authorize(Roles = "SuperAdmin")]
    public class IndexModel : PageModel
    {
        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexModel"/> class.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        public IndexModel(IConfiguration configuration)
        {
            this.configuration = configuration;
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

            return this.Content(result.ToString(), "application/json");
        }
    }
}
