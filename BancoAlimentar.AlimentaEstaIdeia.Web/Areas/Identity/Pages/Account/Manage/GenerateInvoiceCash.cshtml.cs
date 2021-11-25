// -----------------------------------------------------------------------
// <copyright file="GenerateInvoiceCash.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Localization;
    using Microsoft.FeatureManagement;

    /// <summary>
    /// Generate the invoice for the cash donation.
    /// </summary>
    public class GenerateInvoiceCashModel : BaseGenerateInvoice
    {
        private readonly IUnitOfWork context;
        private string publicDonationId;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerateInvoiceCashModel"/> class.
        /// </summary>
        /// <param name="context">Unit of context.</param>
        /// <param name="renderService">Render service.</param>
        /// <param name="webHostEnvironment">Web hosting environment.</param>
        /// <param name="configuration">Configuration.</param>
        /// <param name="stringLocalizerFactory">Localization factory.</param>
        /// <param name="featureManager">Flag feature manager.</param>
        /// <param name="env">Web host environment.</param>
        public GenerateInvoiceCashModel(
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
        }

        /// <inheritdoc/>
        public override (Invoice Invoice, T Model) GenerateInvoice<T>()
        {
            Invoice invoice = this.context.Invoice.FindInvoiceByPublicId(publicDonationId);
            string nif = invoice.Donation.Nif;
            string usersNif = invoice.User.Nif;

            if (!NifValidation.ValidateNif(nif))
            {
                if (NifValidation.ValidateNif(usersNif))
                {
                    nif = invoice.User.Nif;
                }
            }

            InvoiceModel invoiceModelRenderer = new InvoiceModel()
            {
                FullName = invoice.User.FullName,
                DonationAmount = invoice.Donation.DonationAmount,
                InvoiceName = this.context.Invoice.GetInvoiceName(invoice),
                Nif = nif,
                Campaign = this.context.CampaignRepository.GetCurrentCampaign(),
            };
            invoiceModelRenderer.ConvertAmountToText();

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
    }
}
