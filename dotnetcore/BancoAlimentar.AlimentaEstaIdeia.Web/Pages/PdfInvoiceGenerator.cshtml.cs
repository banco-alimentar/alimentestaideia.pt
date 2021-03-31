namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Localization;
    using PdfSharpCore.Drawing;
    using PdfSharpCore.Pdf;
    using VetCV.HtmlRendererCore.Core;
    using VetCV.HtmlRendererCore.Core.Entities;
    using VetCV.HtmlRendererCore.PdfSharpCore;

    public class PdfInvoiceGeneratorModel : PageModel
    {
        private readonly UserManager<WebUser> userManager;
        private readonly IUnitOfWork context;
        private readonly IViewRenderService renderService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IStringLocalizerFactory stringLocalizerFactory;

        public PdfInvoiceGeneratorModel(
            UserManager<WebUser> userManager,
            IUnitOfWork context,
            IViewRenderService renderService,
            IWebHostEnvironment webHostEnvironment,
            IStringLocalizerFactory stringLocalizerFactory)
        {
            this.userManager = userManager;
            this.context = context;
            this.renderService = renderService;
            this.webHostEnvironment = webHostEnvironment;
            this.stringLocalizerFactory = stringLocalizerFactory;
        }

        public async Task OnGetAsync()
        {
            Response.ContentType = "application/pdf";
            using (MemoryStream ms = new MemoryStream())
            {
                var user = await userManager.GetUserAsync(User);

                InvoiceModel invoiceModelRenderer = new InvoiceModel(this.userManager, this.context, this.stringLocalizerFactory)
                {
                    Invoice = this.context.Invoice.FindInvoiceByDonation(218, user),
                };

                string html = await renderService.RenderToStringAsync("Account/Manage/Invoice", "Identity", invoiceModelRenderer);
                PdfDocument document = PdfGenerator.GeneratePdf(
                    html, new PdfGenerateConfig() { PageSize = PdfSharpCore.PageSize.A4, PageOrientation = PdfSharpCore.PageOrientation.Portrait },
                    cssData: null,
                    new EventHandler<HtmlStylesheetLoadEventArgs>(OnStyleSheetLoaded),
                    new EventHandler<HtmlImageLoadEventArgs>(OnHtmlImageLoaded));

                document.Save(ms);
                ms.Position = 0;
                await ms.CopyToAsync(Response.Body);
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
