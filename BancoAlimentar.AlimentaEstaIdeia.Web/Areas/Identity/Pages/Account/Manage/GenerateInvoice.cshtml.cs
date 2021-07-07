// -----------------------------------------------------------------------
// <copyright file="GenerateInvoice.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Features;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Pages;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Localization;
    using Microsoft.FeatureManagement;
    using PdfSharpCore.Pdf;
    using VetCV.HtmlRendererCore.Core.Entities;
    using VetCV.HtmlRendererCore.PdfSharpCore;

    /// <summary>
    /// Regerate the invoice in pdf.
    /// </summary>
    [AllowAnonymous]
    public class GenerateInvoiceModel : PageModel
    {
        private readonly IUnitOfWork context;
        private readonly IViewRenderService renderService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IConfiguration configuration;
        private readonly IStringLocalizerFactory stringLocalizerFactory;
        private readonly IFeatureManager featureManager;

        public GenerateInvoiceModel(
            IUnitOfWork context,
            IViewRenderService renderService,
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration,
            IStringLocalizerFactory stringLocalizerFactory,
            IFeatureManager featureManager)
        {
            this.context = context;
            this.renderService = renderService;
            this.webHostEnvironment = webHostEnvironment;
            this.configuration = configuration;
            this.stringLocalizerFactory = stringLocalizerFactory;
            this.featureManager = featureManager;
        }

        public async Task<IActionResult> OnGetAsync(string publicDonationId = null)
        {
            Tuple<Invoice, Stream> pdfTuple = await GenerateInvoiceInternalAsync(publicDonationId);
            IActionResult result = RedirectToPage("/Index");
            if (pdfTuple != null)
            {
                if (pdfTuple.Item1 != null)
                {
                    Response.Headers.Add("Content-Disposition", $"inline; filename={this.context.Invoice.GetInvoiceName(pdfTuple.Item1)}.pdf");
                    result = File(pdfTuple.Item2, "application/pdf");
                }
                else
                {
                    result = NotFound();
                }
            }

            return result;
        }

        public async Task<Tuple<Invoice, Stream>> GenerateInvoiceInternalAsync(string publicDonationId = null)
        {
            Tuple<Invoice, Stream> result = null;

            bool isMaintenanceEanbled = await featureManager.IsEnabledAsync(nameof(MaintenanceFlags.EnableMaintenance));
            if (!isMaintenanceEanbled)
            {
                Invoice invoice = this.context.Invoice.FindInvoiceByPublicId(publicDonationId);
                if (invoice != null)
                {
                    BlobContainerClient container = new BlobContainerClient(this.configuration["AzureStorage:ConnectionString"], this.configuration["AzureStorage:PdfContainerName"]);
                    BlobClient blobClient = container.GetBlobClient(string.Concat(invoice.BlobName.ToString(), ".pdf"));
                    Stream pdfFile = null;

                    if (!await blobClient.ExistsAsync())
                    {
                        MemoryStream ms = new MemoryStream();
                        InvoiceModel invoiceModelRenderer = new InvoiceModel(this.context, this.stringLocalizerFactory)
                        {
                            Invoice = invoice,
                            Campaign = this.context.CampaignRepository.GetCurrentCampaign(),
                        };
                        invoiceModelRenderer.ConvertAmountToText();
                        string html = await renderService.RenderToStringAsync("Account/Manage/Invoice", "Identity", invoiceModelRenderer);
                        PdfDocument document = PdfGenerator.GeneratePdf(
                            html,
                            new PdfGenerateConfig() { PageSize = PdfSharpCore.PageSize.A4, PageOrientation = PdfSharpCore.PageOrientation.Portrait },
                            cssData: null,
                            new EventHandler<HtmlStylesheetLoadEventArgs>(OnStyleSheetLoaded),
                            new EventHandler<HtmlImageLoadEventArgs>(OnHtmlImageLoaded));

                        document.Save(ms);
                        ms.Position = 0;
                        await blobClient.UploadAsync(ms);
                        ms.Position = 0;
                        pdfFile = ms;
                    }
                    else
                    {
                        pdfFile = await blobClient.OpenReadAsync(new BlobOpenReadOptions(false));
                    }

                    result = new Tuple<Invoice, Stream>(invoice, pdfFile);
                }
                else
                {
                    result = new Tuple<Invoice, Stream>(null, null);
                }
            }

            return result;
        }

        private void OnStyleSheetLoaded(object sender, HtmlStylesheetLoadEventArgs eventArgs)
        {
            string cssFilePath = Path.Combine(this.webHostEnvironment.WebRootPath, eventArgs.Src.TrimStart('/').Replace("/", "\\"));
            eventArgs.SetSrc = cssFilePath;
        }

        private void OnHtmlImageLoaded(object sender, HtmlImageLoadEventArgs eventArgs)
        {
            string imageFilePath = Path.Combine(this.webHostEnvironment.WebRootPath, eventArgs.Src.TrimStart('/').Replace("/", "\\"));
            eventArgs.Callback(imageFilePath);
        }
    }
}
