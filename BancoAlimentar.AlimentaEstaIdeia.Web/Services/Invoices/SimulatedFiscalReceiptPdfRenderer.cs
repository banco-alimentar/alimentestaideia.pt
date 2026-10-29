// -----------------------------------------------------------------------
// <copyright file="SimulatedFiscalReceiptPdfRenderer.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    /// Renders a clearly non-fiscal, Development-only simulation into a PDF.
    /// </summary>
    public class SimulatedFiscalReceiptPdfRenderer
    {
        private const string SimulationReceiptPage = "Fiscal/SimulatedFiscalReceipt";
        private const string AdminArea = "Admin";
        private readonly IViewRenderService viewRenderService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimulatedFiscalReceiptPdfRenderer"/> class.
        /// </summary>
        /// <param name="viewRenderService">The existing application Razor render service.</param>
        public SimulatedFiscalReceiptPdfRenderer(IViewRenderService viewRenderService)
        {
            this.viewRenderService = viewRenderService ?? throw new ArgumentNullException(nameof(viewRenderService));
        }

        /// <summary>
        /// Renders a simulation-only receipt model to PDF.
        /// </summary>
        /// <param name="viewModel">The simulation-only receipt model.</param>
        /// <param name="simulationQrPng">The private simulation QR PNG persisted with the simulation document.</param>
        /// <returns>The PDF content.</returns>
        public byte[] Render(SimulatedFiscalReceiptViewModel viewModel, byte[] simulationQrPng)
        {
            if (viewModel == null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            if (!string.Equals(viewModel.SimulationNotice, "SIMULAÇÃO — SEM VALIDADE FISCAL", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("A simulated receipt must contain its mandatory non-fiscal notice.");
            }

            string html = this.viewRenderService
                .RenderToStringAsync(
                    SimulationReceiptPage,
                    AdminArea,
                    new SimulatedFiscalReceiptPdfPageModel
                    {
                        Receipt = viewModel,
                        SimulationQrPng = simulationQrPng ?? Array.Empty<byte>(),
                    })
                .GetAwaiter()
                .GetResult();
            if (string.IsNullOrWhiteSpace(html))
            {
                throw new InvalidOperationException("Simulated receipt Razor rendering produced no content.");
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
