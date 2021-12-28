// -----------------------------------------------------------------------
// <copyright file="Configuration.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Show configuration model.
    /// </summary>
    public class ConfigurationModel : PageModel
    {
        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationModel"/> class.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        public ConfigurationModel(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        /// <summary>
        /// Gets or sets the actual configuration in the system.
        /// </summary>
        [BindProperty]
        public Dictionary<string, string> ActualConfiguration { get; set; }

        /// <summary>
        /// Execute get operation.
        /// </summary>
        public void OnGet()
        {
            ActualConfiguration = new Dictionary<string, string>();
            foreach (var item in this.configuration.AsEnumerable())
            {
                ActualConfiguration.Add(item.Key, item.Value);
            }
        }
    }
}
