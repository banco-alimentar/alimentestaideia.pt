// -----------------------------------------------------------------------
// <copyright file="FiscalReceiptDeliveryService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services.Invoices
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;

    /// <summary>
    /// Routes a loaded fiscal document through its immutable receipt and PDF artifact pipeline.
    /// </summary>
    public class FiscalReceiptDeliveryService : IFiscalReceiptDeliveryService
    {
        private readonly FiscalReceiptPdfService fiscalReceiptPdfService;
        private readonly FiscalReceiptViewModelFactory fiscalReceiptViewModelFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="FiscalReceiptDeliveryService"/> class.
        /// </summary>
        /// <param name="fiscalReceiptViewModelFactory">The final fiscal receipt snapshot factory.</param>
        /// <param name="fiscalReceiptPdfService">The immutable PDF artifact service.</param>
        public FiscalReceiptDeliveryService(
            FiscalReceiptViewModelFactory fiscalReceiptViewModelFactory,
            FiscalReceiptPdfService fiscalReceiptPdfService)
        {
            this.fiscalReceiptViewModelFactory = fiscalReceiptViewModelFactory ?? throw new ArgumentNullException(nameof(fiscalReceiptViewModelFactory));
            this.fiscalReceiptPdfService = fiscalReceiptPdfService ?? throw new ArgumentNullException(nameof(fiscalReceiptPdfService));
        }

        /// <inheritdoc />
        public FiscalReceiptPdfArtifact GetOrCreate(FiscalDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            FiscalReceiptViewModel viewModel = this.fiscalReceiptViewModelFactory.Create(document);
            return this.fiscalReceiptPdfService.GetOrCreate(document, viewModel);
        }
    }
}
