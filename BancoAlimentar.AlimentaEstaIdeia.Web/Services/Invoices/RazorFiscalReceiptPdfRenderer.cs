// -----------------------------------------------------------------------
// <copyright file="RazorFiscalReceiptPdfRenderer.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services.Invoices
{
    using System;
    using System.IO;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Pages;
    using PdfSharp.Pdf;
    using TheArtOfDev.HtmlRenderer.PdfSharp;

    /// <summary>
    /// Renders an immutable fiscal receipt Razor model into a PDF.
    /// </summary>
    public class RazorFiscalReceiptPdfRenderer : IFiscalReceiptPdfRenderer
    {
        private const string FiscalReceiptPage = "Fiscal/FiscalReceipt";
        private const string AdminArea = "Admin";
        private readonly IViewRenderService viewRenderService;

        /// <summary>
        /// Initializes a new instance of the <see cref="RazorFiscalReceiptPdfRenderer"/> class.
        /// </summary>
        /// <param name="viewRenderService">The existing application Razor render service.</param>
        public RazorFiscalReceiptPdfRenderer(IViewRenderService viewRenderService)
        {
            this.viewRenderService = viewRenderService ?? throw new ArgumentNullException(nameof(viewRenderService));
        }

        /// <inheritdoc />
        public byte[] Render(FiscalReceiptViewModel viewModel)
        {
            if (viewModel == null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            string html = this.viewRenderService
                .RenderToStringAsync(
                    FiscalReceiptPage,
                    AdminArea,
                    new FiscalReceiptPdfPageModel { Receipt = viewModel })
                .GetAwaiter()
                .GetResult();
            if (string.IsNullOrWhiteSpace(html))
            {
                throw new InvalidOperationException("Fiscal receipt Razor rendering produced no content.");
            }

            PdfDocument document = PdfGenerator.GeneratePdf(
                html,
                new PdfGenerateConfig
                {
                    PageSize = PdfSharp.PageSize.A4,
                    PageOrientation = PdfSharp.PageOrientation.Portrait,
                });
            using var stream = new MemoryStream();
            document.Save(stream);
            return stream.ToArray();
        }
    }
}
