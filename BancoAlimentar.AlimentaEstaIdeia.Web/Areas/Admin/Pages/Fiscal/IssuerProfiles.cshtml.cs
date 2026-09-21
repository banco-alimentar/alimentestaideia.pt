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
        public FiscalIssuerProfileAdministrationRequest IssuerProfile { get; set; } = new FiscalIssuerProfileAdministrationRequest();

        /// <summary>
        /// Creates an issuer profile within the tenant obtained by the administration service.
        /// </summary>
        /// <returns>The fiscal issuer profiles page.</returns>
        public Task<IActionResult> OnPostCreateAsync()
        {
            if (!this.HasValidIssuerProfile())
            {
                return Task.FromResult<IActionResult>(this.Page());
            }

            try
            {
                this.fiscalAdministrationService.CreateIssuerProfile(this.IssuerProfile);
            }
            catch (System.InvalidOperationException)
            {
                this.ModelState.AddModelError(string.Empty, "Não foi possível gravar o perfil de emitente. Confirme os dados introduzidos.");
                return Task.FromResult<IActionResult>(this.Page());
            }

            return Task.FromResult<IActionResult>(this.RedirectToPage());
        }

        /// <summary>
        /// Updates an issuer profile within the tenant obtained by the administration service.
        /// </summary>
        /// <returns>The fiscal issuer profiles page.</returns>
        public Task<IActionResult> OnPostUpdateAsync()
        {
            if (!this.HasValidIssuerProfile())
            {
                return Task.FromResult<IActionResult>(this.Page());
            }

            try
            {
                this.fiscalAdministrationService.UpdateIssuerProfile(this.IssuerProfile);
            }
            catch (System.InvalidOperationException)
            {
                this.ModelState.AddModelError(string.Empty, "Não foi possível gravar o perfil de emitente. Confirme os dados introduzidos.");
                return Task.FromResult<IActionResult>(this.Page());
            }

            return Task.FromResult<IActionResult>(this.RedirectToPage());
        }

        private bool HasValidIssuerProfile()
        {
            if (!this.ModelState.IsValid || string.IsNullOrWhiteSpace(this.IssuerProfile.LegalName) ||
                string.IsNullOrWhiteSpace(this.IssuerProfile.LegalAddress) ||
                string.IsNullOrWhiteSpace(this.IssuerProfile.TaxRegistrationNumber) ||
                string.IsNullOrWhiteSpace(this.IssuerProfile.SaftDocumentType) ||
                string.IsNullOrWhiteSpace(this.IssuerProfile.TaxRule) ||
                string.IsNullOrWhiteSpace(this.IssuerProfile.SoftwareCertificateNumber) ||
                string.IsNullOrWhiteSpace(this.IssuerProfile.SigningKeyIdentifier) ||
                string.IsNullOrWhiteSpace(this.IssuerProfile.SigningKeyVersion) ||
                this.IssuerProfile.ValidFromUtc == default)
            {
                this.ModelState.AddModelError(string.Empty, "Preencha os campos obrigatórios e corrija os valores inválidos.");
                return false;
            }

            return true;
        }
    }
}
