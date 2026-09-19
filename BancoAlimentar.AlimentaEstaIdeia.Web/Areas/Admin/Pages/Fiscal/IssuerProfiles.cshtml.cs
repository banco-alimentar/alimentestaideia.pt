// -----------------------------------------------------------------------
// <copyright file="IssuerProfiles.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Fiscal
{
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// Configures the non-secret fiscal issuer profile settings for the current tenant.
    /// </summary>
    [Authorize(Policy = "RoleArea")]
    public class IssuerProfilesModel : PageModel
    {
        private readonly IFiscalAdministrationService fiscalAdministrationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="IssuerProfilesModel"/> class.
        /// </summary>
        /// <param name="fiscalAdministrationService">The trusted fiscal administration service.</param>
        public IssuerProfilesModel(IFiscalAdministrationService fiscalAdministrationService)
        {
            this.fiscalAdministrationService = fiscalAdministrationService;
        }

        /// <summary>
        /// Gets or sets the issuer profile configuration. It contains no private signing-key material.
        /// </summary>
        [BindProperty]
        public FiscalIssuerProfileAdministrationRequest IssuerProfile { get; set; }

        /// <summary>
        /// Creates an issuer profile within the tenant obtained by the administration service.
        /// </summary>
        /// <returns>The fiscal issuer profiles page.</returns>
        public Task<IActionResult> OnPostCreateAsync()
        {
            this.fiscalAdministrationService.CreateIssuerProfile(this.IssuerProfile);
            return Task.FromResult<IActionResult>(this.RedirectToPage());
        }

        /// <summary>
        /// Updates an issuer profile within the tenant obtained by the administration service.
        /// </summary>
        /// <returns>The fiscal issuer profiles page.</returns>
        public Task<IActionResult> OnPostUpdateAsync()
        {
            this.fiscalAdministrationService.UpdateIssuerProfile(this.IssuerProfile);
            return Task.FromResult<IActionResult>(this.RedirectToPage());
        }
    }
}
