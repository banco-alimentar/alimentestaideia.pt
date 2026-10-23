// -----------------------------------------------------------------------
// <copyright file="SimulatedFiscalReceiptDeliveryService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services.Invoices
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;

    /// <summary>
    /// Creates and reloads an immutable Development-only simulated receipt PDF.
    /// </summary>
    public class SimulatedFiscalReceiptDeliveryService : ISimulatedFiscalReceiptDeliveryService
    {
        private const string SimulationNotice = "SIMULAÇÃO — SEM VALIDADE FISCAL";
        private readonly SimulatedFiscalReceiptPdfRenderer renderer;
        private readonly ISimulatedFiscalReceiptPdfBlobStore blobStore;
        private readonly SimulatedFiscalReceiptViewModelFactory viewModelFactory;
        private readonly IFiscalIssuanceModeResolver fiscalIssuanceModeResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimulatedFiscalReceiptDeliveryService"/> class.
        /// </summary>
        /// <param name="viewModelFactory">The simulation-only receipt projection factory.</param>
        /// <param name="renderer">The simulation-only Razor PDF renderer.</param>
        /// <param name="blobStore">The immutable simulation blob store.</param>
        /// <param name="fiscalIssuanceModeResolver">The process-selected fiscal issuance mode.</param>
        public SimulatedFiscalReceiptDeliveryService(
            SimulatedFiscalReceiptViewModelFactory viewModelFactory,
            SimulatedFiscalReceiptPdfRenderer renderer,
            ISimulatedFiscalReceiptPdfBlobStore blobStore,
            IFiscalIssuanceModeResolver fiscalIssuanceModeResolver)
        {
            this.viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
            this.renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            this.blobStore = blobStore ?? throw new ArgumentNullException(nameof(blobStore));
            this.fiscalIssuanceModeResolver = fiscalIssuanceModeResolver ?? throw new ArgumentNullException(nameof(fiscalIssuanceModeResolver));
        }

        /// <inheritdoc />
        public byte[] GetOrCreate(SimulatedFiscalDocument document)
        {
            if (this.fiscalIssuanceModeResolver.Resolve() != FiscalIssuanceMode.Simulation)
            {
                throw new InvalidOperationException("Simulated receipt delivery is available only in the Development simulation mode.");
            }

            ValidateDocument(document);
            if (this.blobStore.Exists(document.ArtifactReference))
            {
                return this.blobStore.Load(document.ArtifactReference);
            }

            SimulatedFiscalReceiptViewModel viewModel = this.viewModelFactory.Create(document);
            if (!string.Equals(viewModel.SimulationNotice, SimulationNotice, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("A simulated receipt must preserve its mandatory non-fiscal notice.");
            }

            byte[] content = this.renderer.Render(viewModel, document.SimulationQrPng);
            if (content == null || content.Length == 0)
            {
                throw new InvalidOperationException("Simulated receipt PDF rendering produced no content.");
            }

            try
            {
                this.blobStore.Save(document.ArtifactReference, content);
                return content;
            }
            catch (InvalidOperationException exception) when (exception.InnerException != null)
            {
                // An equal concurrent request may have created the immutable artifact first.
                return this.blobStore.Load(document.ArtifactReference);
            }
        }

        private static void ValidateDocument(SimulatedFiscalDocument document)
        {
            if (document == null ||
                !string.Equals(document.OriginEnvironment, "Development", StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(document.ArtifactReference) ||
                !document.ArtifactReference.StartsWith("simulation/development/", StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(document.SyntheticIdentifier) ||
                string.IsNullOrWhiteSpace(document.SimulationQrPayload))
            {
                throw new InvalidOperationException("A complete Development simulation document is required for PDF delivery.");
            }
        }
    }
}
