// -----------------------------------------------------------------------
// <copyright file="GenerateInvoice.cshtml.cs" company="Federa��o Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federa��o Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
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
    using BancoAlimentar.AlimentaEstaIdeia.Web.Validation;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Localization;
    using Microsoft.FeatureManagement;
    using PdfSharpCore.Drawing;
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
        private readonly IWebHostEnvironment env;

        public GenerateInvoiceModel(
            IUnitOfWork context,
            IViewRenderService renderService,
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration,
            IStringLocalizerFactory stringLocalizerFactory,
            IFeatureManager featureManager,
            IWebHostEnvironment env)
        {
            this.context = context;
            this.renderService = renderService;
            this.webHostEnvironment = webHostEnvironment;
            this.configuration = configuration;
            this.stringLocalizerFactory = stringLocalizerFactory;
            this.featureManager = featureManager;
            this.env = env;
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

            bool isMaintenanceEnabled = await featureManager.IsEnabledAsync(nameof(MaintenanceFlags.EnableMaintenance));
            if (!isMaintenanceEnabled)
            {
                Invoice invoice = this.context.Invoice.FindInvoiceByPublicId(publicDonationId);
                if (invoice != null)
                {
                    BlobContainerClient container = new BlobContainerClient(this.configuration["AzureStorage:ConnectionString"], this.configuration["AzureStorage:PdfContainerName"]);
                    BlobClient blobClient = container.GetBlobClient(string.Concat(invoice.BlobName.ToString(), ".pdf"));
                    Stream pdfFile = null;

                    if (!await blobClient.ExistsAsync())
                    {
                        string nif = invoice.Donation.Nif;
                        string usersNif = invoice.User.Nif;

                        if (!NifAttribute.ValidateNif(nif))
                        {
                            if (!NifAttribute.ValidateNif(usersNif))
                            {
                                nif = invoice.User.Nif;
                            }
                            else 
                            {
                                return result;
                            }
                        }
                        else 
                        {
                            return result;
                        }

                        MemoryStream ms = new MemoryStream();
                        InvoiceModel invoiceModelRenderer = new InvoiceModel(this.context, this.stringLocalizerFactory)
                        {
                            FullName = invoice.User.FullName,
                            DonationAmount = invoice.Donation.DonationAmount,
                            InvoiceName = this.context.Invoice.GetInvoiceName(invoice),
                            Nif = nif,
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

                        bool stagingOrDev = this.env.IsStaging() || this.env.IsDevelopment();
                        if (stagingOrDev)
                        {
                            string watermark = "NOT A REAL INVOICE";
                            XFont font = new XFont("Arial", 72d);
                            PdfPage page = document.Pages[0];
                            var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Prepend);
                            var size = gfx.MeasureString(watermark, font);
                            gfx.TranslateTransform(page.Width / 2, page.Height / 2);
                            gfx.RotateTransform(-Math.Atan(page.Height / page.Width) * 180 / Math.PI);
                            gfx.TranslateTransform(-page.Width / 2, -page.Height / 2);
                            var format = new XStringFormat();
                            format.Alignment = XStringAlignment.Near;
                            format.LineAlignment = XLineAlignment.Near;
                            XBrush brush = new XSolidBrush(XColor.FromArgb(128, 255, 0, 0));

                            gfx.DrawString(watermark, font, brush,
                                new XPoint((page.Width - size.Width) / 2, (page.Height - size.Height) / 2),
                                format);
                        }

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
