// -----------------------------------------------------------------------
// <copyright file="FiscalDocumentIssuanceService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Coordinates reservation, signing, QR generation and immutable PDF persistence for fiscal issuance.
    /// </summary>
    public class FiscalDocumentIssuanceService : IFiscalDocumentIssuanceService
    {
        private const string PortugueseTaxCountryRegion = "PT";
        private const string NormalDocumentStatus = "N";

        private readonly ApplicationDbContext applicationDbContext;
        private readonly IFiscalDocumentReservationService fiscalDocumentReservationService;
        private readonly IFiscalQrCodeImageGenerator fiscalQrCodeImageGenerator;
        private readonly IFiscalQrPayloadBuilder fiscalQrPayloadBuilder;
        private readonly IFiscalScopeResolver fiscalScopeResolver;
        private readonly FiscalReceiptPdfService fiscalReceiptPdfService;
        private readonly FiscalReceiptViewModelFactory fiscalReceiptViewModelFactory;
        private readonly IFiscalSignatureService fiscalSignatureService;

        /// <summary>
        /// Initializes a new instance of the <see cref="FiscalDocumentIssuanceService"/> class.
        /// </summary>
        /// <param name="applicationDbContext">The tenant fiscal database.</param>
        /// <param name="fiscalScopeResolver">The trusted fiscal scope resolver.</param>
        /// <param name="fiscalDocumentReservationService">The idempotent series reservation service.</param>
        /// <param name="fiscalSignatureService">The protected-key fiscal signature service.</param>
        /// <param name="fiscalQrPayloadBuilder">The official fiscal QR payload builder.</param>
        /// <param name="fiscalQrCodeImageGenerator">The fiscal QR PNG generator.</param>
        /// <param name="fiscalReceiptViewModelFactory">The immutable receipt snapshot factory.</param>
        /// <param name="fiscalReceiptPdfService">The immutable receipt PDF persistence service.</param>
        public FiscalDocumentIssuanceService(
            ApplicationDbContext applicationDbContext,
            IFiscalScopeResolver fiscalScopeResolver,
            IFiscalDocumentReservationService fiscalDocumentReservationService,
            IFiscalSignatureService fiscalSignatureService,
            IFiscalQrPayloadBuilder fiscalQrPayloadBuilder,
            IFiscalQrCodeImageGenerator fiscalQrCodeImageGenerator,
            FiscalReceiptViewModelFactory fiscalReceiptViewModelFactory,
            FiscalReceiptPdfService fiscalReceiptPdfService)
        {
            this.applicationDbContext = applicationDbContext ?? throw new ArgumentNullException(nameof(applicationDbContext));
            this.fiscalScopeResolver = fiscalScopeResolver ?? throw new ArgumentNullException(nameof(fiscalScopeResolver));
            this.fiscalDocumentReservationService = fiscalDocumentReservationService ?? throw new ArgumentNullException(nameof(fiscalDocumentReservationService));
            this.fiscalSignatureService = fiscalSignatureService ?? throw new ArgumentNullException(nameof(fiscalSignatureService));
            this.fiscalQrPayloadBuilder = fiscalQrPayloadBuilder ?? throw new ArgumentNullException(nameof(fiscalQrPayloadBuilder));
            this.fiscalQrCodeImageGenerator = fiscalQrCodeImageGenerator ?? throw new ArgumentNullException(nameof(fiscalQrCodeImageGenerator));
            this.fiscalReceiptViewModelFactory = fiscalReceiptViewModelFactory ?? throw new ArgumentNullException(nameof(fiscalReceiptViewModelFactory));
            this.fiscalReceiptPdfService = fiscalReceiptPdfService ?? throw new ArgumentNullException(nameof(fiscalReceiptPdfService));
        }

        /// <inheritdoc />
        public FiscalDocument IssueOriginal(Tenant tenant, Donation donation)
        {
            this.ValidateRequestedEntities(tenant, donation);
            Donation persistedDonation = this.LoadPaidDonation(donation.Id);
            FiscalDocument existingIssuedDocument = this.FindIssuedOriginal(persistedDonation.Id);
            if (existingIssuedDocument != null)
            {
                return existingIssuedDocument;
            }

            FiscalScope fiscalScope = this.fiscalScopeResolver.Resolve(tenant, persistedDonation);
            this.ValidateFiscalScope(tenant, fiscalScope);
            FiscalDocument fiscalDocument = this.fiscalDocumentReservationService.ReserveOriginal(new FiscalDocumentReservationRequest
            {
                DonationId = persistedDonation.Id,
                SeriesId = fiscalScope.FiscalDocumentSeries.Id,
                IdempotencyKey = persistedDonation.ConfirmedPayment.IdempotencyKey,
                FiscalScope = fiscalScope,
            });

            if (fiscalDocument == null)
            {
                throw new InvalidOperationException("Fiscal document reservation did not return a document.");
            }

            if (fiscalDocument.Status == FiscalDocumentStatus.Issued)
            {
                return fiscalDocument;
            }

            FiscalSignatureResult signature = this.fiscalSignatureService.Sign(new FiscalSignatureRequest
            {
                FiscalDocument = fiscalDocument,
                SigningKeyIdentifier = fiscalDocument.SigningKeyIdentifier,
                SigningKeyVersion = fiscalDocument.SigningKeyVersion,
                SigningAlgorithm = fiscalDocument.SigningAlgorithm,
            });
            this.ApplySignature(fiscalDocument, signature);

            FiscalQrPayloadSnapshot qrSnapshot = this.CreateQrSnapshot(fiscalDocument, fiscalScope);
            string qrPayload = this.fiscalQrPayloadBuilder.Build(qrSnapshot);
            byte[] qrPng = this.fiscalQrCodeImageGenerator.GeneratePng(qrPayload);
            if (qrPng == null || qrPng.Length == 0)
            {
                throw new InvalidOperationException("Fiscal QR generation returned no image.");
            }

            fiscalDocument.QrPayload = qrPayload;
            fiscalDocument.QrPng = qrPng;
            FiscalReceiptViewModel receiptViewModel = this.fiscalReceiptViewModelFactory.Create(fiscalDocument);
            this.fiscalReceiptPdfService.GetOrCreate(fiscalDocument, receiptViewModel);
            fiscalDocument.Status = FiscalDocumentStatus.Issued;
            this.applicationDbContext.SaveChanges();
            return fiscalDocument;
        }

        private void ApplySignature(FiscalDocument fiscalDocument, FiscalSignatureResult signature)
        {
            if (signature == null || string.IsNullOrWhiteSpace(signature.Hash) ||
                string.IsNullOrWhiteSpace(signature.HashControl))
            {
                throw new InvalidOperationException("Fiscal signing returned incomplete signature evidence.");
            }

            fiscalDocument.PreviousHash = signature.PreviousHash;
            fiscalDocument.Hash = signature.Hash;
            fiscalDocument.HashControl = signature.HashControl;
            fiscalDocument.SigningKeyIdentifier = signature.SigningKeyIdentifier;
            fiscalDocument.SigningKeyVersion = signature.SigningKeyVersion;
            fiscalDocument.SigningAlgorithm = signature.SigningAlgorithm;
        }

        private FiscalQrPayloadSnapshot CreateQrSnapshot(FiscalDocument fiscalDocument, FiscalScope fiscalScope)
        {
            return new FiscalQrPayloadSnapshot
            {
                IssuerTaxRegistrationNumber = fiscalDocument.IssuerTaxRegistrationNumber,
                BuyerTaxRegistrationNumber = fiscalDocument.DonorTaxRegistrationNumber,
                BuyerCountryCode = PortugueseTaxCountryRegion,
                DocumentType = fiscalScope.FiscalDocumentSeries.SaftDocumentType,
                DocumentStatus = NormalDocumentStatus,
                InvoiceDate = fiscalDocument.InvoiceDate,
                InvoiceIdentifier = fiscalDocument.DocumentNumber,
                Atcud = fiscalDocument.Atcud,
                TaxCountryRegion = PortugueseTaxCountryRegion,
                TaxableBaseNormal = fiscalDocument.NetTotal,
                TaxAmountNormal = fiscalDocument.TaxTotal,
                TaxPayable = fiscalDocument.TaxTotal,
                GrossTotal = fiscalDocument.GrossTotal,
                Hash = fiscalDocument.Hash,
                HashControl = fiscalDocument.HashControl,
                SoftwareCertificateNumber = fiscalDocument.SoftwareCertificateNumber,
            };
        }

        private FiscalDocument FindIssuedOriginal(int donationId)
        {
            return this.applicationDbContext.FiscalDocuments
                .Include(item => item.Lines)
                .Include(item => item.Events)
                .SingleOrDefault(item => item.DonationId == donationId &&
                    item.Kind == FiscalDocumentKind.Original &&
                    item.Status == FiscalDocumentStatus.Issued);
        }

        private Donation LoadPaidDonation(int donationId)
        {
            Donation donation = this.applicationDbContext.Donations
                .Include(item => item.ConfirmedPayment)
                .Include(item => item.FoodBank)
                .SingleOrDefault(item => item.Id == donationId);
            if (donation == null || donation.PaymentStatus != PaymentStatus.Payed ||
                donation.ConfirmedPayment == null ||
                string.IsNullOrWhiteSpace(donation.ConfirmedPayment.IdempotencyKey))
            {
                throw new InvalidOperationException("Fiscal issuance requires a paid donation with a payment idempotency key.");
            }

            return donation;
        }

        private void ValidateFiscalScope(Tenant tenant, FiscalScope fiscalScope)
        {
            if (fiscalScope == null || fiscalScope.TenantId != tenant.Id ||
                fiscalScope.FiscalIssuerProfile == null ||
                fiscalScope.FiscalDocumentSeries == null || fiscalScope.FiscalDocumentSeries.Id <= 0)
            {
                throw new InvalidOperationException("Fiscal issuance requires a resolved fiscal scope.");
            }
        }

        private void ValidateRequestedEntities(Tenant tenant, Donation donation)
        {
            if (tenant == null || tenant.Id <= 0)
            {
                throw new InvalidOperationException("Fiscal issuance requires a trusted tenant.");
            }

            if (donation == null || donation.Id <= 0)
            {
                throw new InvalidOperationException("Fiscal issuance requires a persisted donation.");
            }
        }
    }
}
