// -----------------------------------------------------------------------
// <copyright file="GenerateInvoice.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage
{
    using System.IO;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.Validation;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Pages;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Localization;
    using Microsoft.FeatureManagement;

    /// <summary>
    /// Regerate the invoice in pdf.
    /// </summary>
    [AllowAnonymous]
    public class GenerateInvoiceModel : BaseGenerateInvoice
    {
        private readonly IUnitOfWork context;
        private string publicDonationId;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerateInvoiceModel"/> class.
        /// </summary>
        /// <param name="context">Unit of context.</param>
        /// <param name="renderService">Render service.</param>
        /// <param name="webHostEnvironment">Web hosting environment.</param>
        /// <param name="configuration">Configuration.</param>
        /// <param name="stringLocalizerFactory">Localization factory.</param>
        /// <param name="featureManager">Flag feature manager.</param>
        /// <param name="env">Web host environment.</param>
        public GenerateInvoiceModel(
            IUnitOfWork context,
            IViewRenderService renderService,
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration,
            IStringLocalizerFactory stringLocalizerFactory,
            IFeatureManager featureManager,
            IWebHostEnvironment env)
            : base(context, renderService, webHostEnvironment, configuration, stringLocalizerFactory, featureManager, env)
        {
            this.context = context;
            WillGenerateInvoice = true;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the invoice will be generated.
        /// </summary>
        public bool WillGenerateInvoice { get; set; }

        /// <inheritdoc/>
        public override (Invoice Invoice, T Model) GenerateInvoice<T>()
        {
            InvoiceModel invoiceModelRenderer = null;
            Invoice invoice = this.context.Invoice.FindInvoiceByPublicId(publicDonationId, WillGenerateInvoice);
            if (invoice != null)
            {
                string nif = invoice.Donation.Nif;
                string usersNif = invoice.User.Nif;

                if (!NifValidation.ValidateNif(nif))
                {
                    if (NifValidation.ValidateNif(usersNif))
                    {
                        nif = invoice.User.Nif;
                    }
                }

                invoiceModelRenderer = new InvoiceModel()
                {
                    FullName = invoice.User.FullName,
                    DonationAmount = invoice.Donation.DonationAmount,
                    InvoiceName = this.context.Invoice.GetInvoiceName(invoice),
                    Nif = nif,
                    Campaign = this.context.CampaignRepository.GetCurrentCampaign(),
                };
                invoiceModelRenderer.ConvertAmountToText();
            }

            return (invoice, (T)(object)invoiceModelRenderer);
        }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <param name="publicDonationId">Public Id for the donation.</param>
        /// <returns>The pdf file.</returns>
        public async Task<IActionResult> OnGetAsync(string publicDonationId = null)
        {
            this.publicDonationId = publicDonationId;
            (Invoice invoice, Stream pdfFile) = await GenerateInvoiceInternalAsync<InvoiceModel>();
            IActionResult result = null;

            if (invoice != null)
            {
                Response.Headers.Add("Content-Disposition", $"inline; filename={this.context.Invoice.GetInvoiceName(invoice)}.pdf");
                result = File(pdfFile, "application/pdf");
            }
            else
            {
                result = NotFound();
            }

            return result;
        }

        /// <summary>
        /// Helper method to render the invoice from the email.
        /// </summary>
        /// <param name="publicDonationId">Public donation id.</param>
        /// <returns>A refernce to the invoice and the pdf file.</returns>
        public async Task<(Invoice Invoice, Stream PdfFile)> RenderInternal(string publicDonationId = null)
        {
            this.publicDonationId = publicDonationId;
            return await GenerateInvoiceInternalAsync<InvoiceModel>();
        }
    }
}
