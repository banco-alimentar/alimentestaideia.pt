// -----------------------------------------------------------------------
// <copyright file="Status.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages
{
    using System.Collections.Generic;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Get status values.
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Mvc.RazorPages.PageModel" />
    public class StatusModel : PageModel
    {
        private readonly IConfiguration configuration;
        private readonly IUnitOfWork context;

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusModel"/> class.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="context">A reference to the <see cref="IUnitOfWork"/>.</param>
        public StatusModel(
            IConfiguration configuration,
            IUnitOfWork context)
        {
            this.configuration = configuration;
            this.context = context;
        }

        /// <summary>
        /// Gets or sets the services.
        /// </summary>
        public Dictionary<string, string> Services { get; set; } = new ();

        /// <summary>
        /// Gets the values.
        /// </summary>
        public void OnGet()
        {
            this.Services.Add("Test", "Value");
        }
    }
}
