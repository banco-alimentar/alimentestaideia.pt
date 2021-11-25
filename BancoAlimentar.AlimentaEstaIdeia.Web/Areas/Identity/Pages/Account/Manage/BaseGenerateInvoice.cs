// -----------------------------------------------------------------------
// <copyright file="BaseGenerateInvoice.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    using BancoAlimentar.AlimentaEstaIdeia.Repository.Validation;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Features;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Pages;
    using Microsoft.AspNetCore.Hosting;
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
    /// Base class for the Generate invoice process.
    /// </summary>
    public abstract class BaseGenerateInvoice : PageModel
    {
        private readonly IUnitOfWork context;
        private readonly IViewRenderService renderService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IConfiguration configuration;
        private readonly IStringLocalizerFactory stringLocalizerFactory;
        private readonly IFeatureManager featureManager;
        private readonly IWebHostEnvironment env;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseGenerateInvoice"/> class.
        /// </summary>
        /// <param name="context">Unit of context.</param>
        /// <param name="renderService">Render service.</param>
        /// <param name="webHostEnvironment">Web hosting environment.</param>
        /// <param name="configuration">Configuration.</param>
        /// <param name="stringLocalizerFactory">Localization factory.</param>
        /// <param name="featureManager">Flag feature manager.</param>
        /// <param name="env">Web host environment.</param>
        public BaseGenerateInvoice(
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

        /// <summary>
        /// Generate the invoice.
        /// </summary>
        /// <returns>The invoce.</returns>
        public abstract (Invoice Invoice, T Model) GenerateInvoice<T>()
            where T : PageModel;

        /// <summary>
        /// Generate the invoice for the donation.
        /// </summary>
        /// <returns>A tuple with the invoice and the <see cref="Stream"/> with the pdf file.</returns>
        protected async Task<(Invoice invoice, Stream PdfItem)> GenerateInvoiceInternalAsync<T>()
            where T : PageModel
        {
            bool isMaintenanceEnabled = await featureManager.IsEnabledAsync(nameof(MaintenanceFlags.EnableMaintenance));
            if (!isMaintenanceEnabled)
            {
                (Invoice invoice, T model) = GenerateInvoice<T>();
                if (invoice != null)
                {
                    BlobContainerClient container = new BlobContainerClient(this.configuration["AzureStorage:ConnectionString"], this.configuration["AzureStorage:PdfContainerName"]);
                    BlobClient blobClient = container.GetBlobClient(string.Concat(invoice.BlobName.ToString(), ".pdf"));
                    Stream pdfFile = null;

                    if (!await blobClient.ExistsAsync())
                    {
                        MemoryStream ms = new MemoryStream();
                        string html = await renderService.RenderToStringAsync("Account/Manage/Invoice", "Identity", model);
                        PdfDocument document = PdfGenerator.GeneratePdf(
                            html,
                            new PdfGenerateConfig() { PageSize = PdfSharpCore.PageSize.A4, PageOrientation = PdfSharpCore.PageOrientation.Portrait },
                            cssData: null,
                            new EventHandler<HtmlStylesheetLoadEventArgs>(OnStyleSheetLoaded),
                            new EventHandler<HtmlImageLoadEventArgs>(OnHtmlImageLoaded));

                        bool stagingOrDev = this.env.IsStaging() || this.env.IsDevelopment();
                        if (stagingOrDev)
                        {
                            string watermark = "RECIBO DE TESTES";
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

                            gfx.DrawString(
                                watermark,
                                font,
                                brush,
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

                    return (invoice, pdfFile);
                }
            }

            return (null, null);
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
