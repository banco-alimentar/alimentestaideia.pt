// -----------------------------------------------------------------------
// <copyright file="GenerateInvoice.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.Validation;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage.Invoices;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Features;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Pages;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using Microsoft.FeatureManagement;
    using Newtonsoft.Json.Linq;
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
        private readonly NifApiValidator nifApiValidator;
        private readonly TelemetryClient telemetryClient;

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
        /// <param name="nifApiValidator">Nif validation api.</param>
        /// <param name="telemetryClient">TelemetryClient.</param>
        public GenerateInvoiceModel(
            IUnitOfWork context,
            IViewRenderService renderService,
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration,
            IStringLocalizerFactory stringLocalizerFactory,
            IFeatureManager featureManager,
            IWebHostEnvironment env,
            NifApiValidator nifApiValidator,
            TelemetryClient telemetryClient)
        {
            this.context = context;
            this.renderService = renderService;
            this.webHostEnvironment = webHostEnvironment;
            this.configuration = configuration;
            this.stringLocalizerFactory = stringLocalizerFactory;
            this.featureManager = featureManager;
            this.env = env;
            this.nifApiValidator = nifApiValidator;
            this.telemetryClient = telemetryClient;
        }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <param name="publicDonationId">Public Id for the donation.</param>
        /// <returns>The pdf file.</returns>
        public async Task<IActionResult> OnGetAsync(string publicDonationId = null)
        {
            IActionResult result = null;

            (Invoice invoice, Stream pdfFile, InvoiceStatusResult invoiceStatusResult) = await GenerateInvoiceInternalAsync(publicDonationId, this.HttpContext.GetTenant());
            if (invoice != null)
            {
                Response.Headers.Append("Content-Disposition", $"inline; filename={this.context.Invoice.GetInvoiceName(invoice)}.pdf");
                result = File(pdfFile, "application/pdf");
            }
            else
            {
                if (invoiceStatusResult == InvoiceStatusResult.NotPayed || invoiceStatusResult == InvoiceStatusResult.ConfirmedPaymentIsNull)
                {
                    return this.RedirectToPage("CheckPaymentStatusInvoice", new { PublicId = publicDonationId });
                }
                else if (invoiceStatusResult == InvoiceStatusResult.DonationIsOneYearOld)
                {
                    this.telemetryClient.TrackEvent("GenerateInvoiceErrorMessage", new Dictionary<string, string>()
                    {
                            { "publicDonationId", publicDonationId },
                    });
                    result = Page();
                }
            }

            return result;
        }

        /// <summary>
        /// Generate the invoice for the donation.
        /// </summary>
        /// <param name="publicDonationId">PubicId for the donation.</param>
        /// <param name="tenant">Current tenant.</param>
        /// <param name="generateInvoice">True to generate the invoice, false for just get the invoice if previously generated.</param>
        /// <returns>A tuple with the invoice and the <see cref="Stream"/> with the pdf file.</returns>
        public async Task<(Invoice Invoice, Stream PdfFile, InvoiceStatusResult InvoiceStatusResult)> GenerateInvoiceInternalAsync(
            string publicDonationId,
            Tenant tenant,
            bool generateInvoice = true)
        {
            bool isMaintenanceEnabled = await featureManager.IsEnabledAsync(nameof(MaintenanceFlags.EnableMaintenance));
            if (!isMaintenanceEnabled)
            {
                InvoiceStatusResult invoiceStatusResult = InvoiceStatusResult.None;
                Invoice invoice = this.context.Invoice.FindInvoiceByPublicId(publicDonationId, tenant, out invoiceStatusResult, generateInvoice);
                if (invoice != null && invoice != Invoice.DefaultInvalidInvoice)
                {
                    BlobContainerClient container = new BlobContainerClient(this.configuration["AzureStorage:ConnectionString"], this.configuration["AzureStorage:PdfContainerName"]);
                    BlobClient blobClient = container.GetBlobClient(string.Concat(invoice.BlobName.ToString(), ".pdf"));
                    Stream pdfFile = null;

#if DEBUG
                    if (await blobClient.ExistsAsync())
                    {
                        await blobClient.DeleteAsync();
                    }
#endif

                    if (!await blobClient.ExistsAsync())
                    {
                        string nif = invoice.Donation.Nif;
                        string usersNif = invoice.User.Nif;

                        if (!nifApiValidator.IsValidNif(nif))
                        {
                            if (nifApiValidator.IsValidNif(usersNif))
                            {
                                nif = invoice.User.Nif;
                            }
                        }

                        if (invoice.Donation.PaymentStatus != PaymentStatus.Payed)
                        {
                            throw new InvalidOperationException(string.Format("GenerateInvoiceInternalAsync but Not Paid. DonationId={0}", invoice.Donation.Id.ToString()));
                        }

                        InvoiceRenderModel invoiceRender = new InvoiceRenderModel();

                        if (tenant.InvoicingStrategy == InvoicingStrategy.SingleInvoiceTable)
                        {
                            invoiceRender.HeaderImage = tenant.InvoiceConfiguration.HeaderImage;
                            invoiceRender.FooterSignatureImage = tenant.InvoiceConfiguration.FooterSignatureImage;
                            invoiceRender.PageTitle = tenant.Name;
                        }
                        else if (tenant.InvoicingStrategy == InvoicingStrategy.MultipleTablesPerFoodBank)
                        {
                            invoiceRender.HeaderImage = invoice.Donation.FoodBank.ReceiptHeader;
                            invoiceRender.FooterSignatureImage = invoice.Donation.FoodBank.ReceiptSignatureImg;
                            invoiceRender.PageTitle = tenant.Name;
                        }

                        MemoryStream ms = new MemoryStream();
                        BaseInvoicePageModel invoiceModelRenderer = ActivateTenantInvoicePageModel(tenant);
                        invoiceModelRenderer.FullName = invoice.User.FullName;
                        invoiceModelRenderer.DonationAmount = invoice.Donation.DonationAmount;
                        invoiceModelRenderer.InvoiceName = this.context.Invoice.GetInvoiceName(invoice);
                        invoiceModelRenderer.InvoiceDate = invoice.Donation.ConfirmedPayment.Created;
                        invoiceModelRenderer.Nif = nif;
                        invoiceModelRenderer.Campaign = this.context.CampaignRepository.GetCurrentCampaign();
                        invoiceModelRenderer.InvoiceRenderModel = invoiceRender;
                        invoiceModelRenderer.InitializeInvoice();

                        MethodInfo methodInfo = renderService
                            .GetType()
                            .GetMethod("RenderToStringAsync", BindingFlags.Public | BindingFlags.Instance);

                        methodInfo = methodInfo.MakeGenericMethod(invoiceModelRenderer.GetType());

                        // The reason behind this dynamic invoke instead of calling it normally, is that we don't
                        // know the type of the PageModel at compile time, so we need to use reflection. But here is
                        // the interesting part, the method RenderToStringAsync is generic, so we need to call it with a
                        // generic parameter, so the ViewDataDictionary<T> class used internally match the expected @Model
                        // in the page view.
                        string html = await (Task<string>)methodInfo.Invoke(
                            renderService,
                            new object[] { $"Account/Manage/Invoices/{tenant.NormalizedName}/Invoice", "Identity", invoiceModelRenderer, });

                        // string html = await renderService.RenderToStringAsync($"Account/Manage/Invoices/{tenant.NormalizedName}/Invoice", "Identity", invoiceModelRenderer);
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

                        // in a rare condition, the pdf is already on the server, so we check again here.
                        if (!await blobClient.ExistsAsync())
                        {
                            try
                            {
                                await blobClient.UploadAsync(ms);
                            }
                            catch (Azure.RequestFailedException ex)
                            {
                                // this is what rare condition, but it can happen, so we just ignore it.
                                // the pdf with the invoice is already on the server, so we just ignore it.
                                if (ex.Message != "The specified blob already exists")
                                {
                                    throw;
                                }
                            }
                        }

                        ms.Position = 0;
                        pdfFile = ms;
                    }
                    else
                    {
                        pdfFile = await blobClient.OpenReadAsync(new BlobOpenReadOptions(false));
                    }

                    return (invoice, pdfFile, invoiceStatusResult);
                }
                else
                {
                    return (null, null, invoiceStatusResult);
                }
            }

            return (null, null, InvoiceStatusResult.None);
        }

        private BaseInvoicePageModel ActivateTenantInvoicePageModel(Tenant tenant)
        {
            Type[] allTypes = Assembly.GetExecutingAssembly().GetTypes();
            string targetTypeName = $"bancoalimentar.alimentaestaideia.web.areas.identity.pages.account.manage.invoices.{tenant.NormalizedName.ToLowerInvariant()}.invoicemodel";
            Type targetType = allTypes
                .Where(p => p.FullName.ToLowerInvariant() == targetTypeName)
                .First();

            return (BaseInvoicePageModel)Activator.CreateInstance(targetType);
        }

        private void OnStyleSheetLoaded(object sender, HtmlStylesheetLoadEventArgs eventArgs)
        {
            if (!string.IsNullOrEmpty(eventArgs.Src) && !eventArgs.Src.StartsWith("https"))
            {
                string cssFilePath = Path.Combine(this.webHostEnvironment.WebRootPath, eventArgs.Src.TrimStart('/').Replace("/", "\\"));
                eventArgs.SetSrc = cssFilePath;
            }
        }

        private void OnHtmlImageLoaded(object sender, HtmlImageLoadEventArgs eventArgs)
        {
            if (!string.IsNullOrEmpty(eventArgs.Src) && !eventArgs.Src.StartsWith("https"))
            {
                string imageFilePath = Path.Combine(this.webHostEnvironment.WebRootPath, eventArgs.Src.TrimStart('/').Replace("/", "\\"));
                eventArgs.Callback(imageFilePath);
            }
        }
    }
}
