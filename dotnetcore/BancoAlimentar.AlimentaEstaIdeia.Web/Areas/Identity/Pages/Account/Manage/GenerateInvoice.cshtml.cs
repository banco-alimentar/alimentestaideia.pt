namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Azure.Storage.Blobs;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Pages;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Localization;
    using PdfSharpCore.Pdf;
    using VetCV.HtmlRendererCore.Core.Entities;
    using VetCV.HtmlRendererCore.PdfSharpCore;

    public class GenerateInvoiceModel : PageModel
    {
        private readonly UserManager<WebUser> userManager;
        private readonly IUnitOfWork context;
        private readonly IViewRenderService renderService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IConfiguration configuration;
        private readonly IStringLocalizerFactory stringLocalizerFactory;

        public GenerateInvoiceModel(
            UserManager<WebUser> userManager,
            IUnitOfWork context,
            IViewRenderService renderService,
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration,
            IStringLocalizerFactory stringLocalizerFactory)
        {
            this.userManager = userManager;
            this.context = context;
            this.renderService = renderService;
            this.webHostEnvironment = webHostEnvironment;
            this.configuration = configuration;
            this.stringLocalizerFactory = stringLocalizerFactory;
        }

        public async Task OnGetAsync(string publicDonationId = null)
        {
            Invoice invoice = this.context.Invoice.FindInvoiceByPublicId(publicDonationId);
            BlobContainerClient container = new BlobContainerClient(this.configuration["AzureStorage.ConnectionString"], this.configuration["AzureStorage.PdfContainerName"]);
            BlobClient blobClient = container.GetBlobClient(string.Concat(invoice.InvoicePublicId.ToString(), ".pdf"));
            if (!await blobClient.ExistsAsync())
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    InvoiceModel invoiceModelRenderer = new InvoiceModel(this.userManager, this.context, this.stringLocalizerFactory)
                    {
                        Invoice = invoice,
                    };
                    string html = await renderService.RenderToStringAsync("Account/Manage/Invoice", "Identity", invoiceModelRenderer);
                    PdfDocument document = PdfGenerator.GeneratePdf(
                        html, new PdfGenerateConfig() { PageSize = PdfSharpCore.PageSize.A4, PageOrientation = PdfSharpCore.PageOrientation.Portrait },
                        cssData: null,
                        new EventHandler<HtmlStylesheetLoadEventArgs>(OnStyleSheetLoaded),
                        new EventHandler<HtmlImageLoadEventArgs>(OnHtmlImageLoaded));

                    document.Save(ms);
                    ms.Position = 0;
                    await blobClient.UploadAsync(ms);
                }
            }
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
