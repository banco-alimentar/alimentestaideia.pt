// -----------------------------------------------------------------------
// <copyright file="FiscalDocumentCorrectionService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Creates a separately numbered, signed and immutable correction from an issued original.
    /// </summary>
    public class FiscalDocumentCorrectionService : IFiscalDocumentCorrectionService
    {
        private const string PortugueseTaxCountryRegion = "PT";
        private const string NormalDocumentStatus = "N";

        private readonly ApplicationDbContext applicationDbContext;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IFiscalScopeResolver fiscalScopeResolver;
        private readonly IFiscalDocumentReservationService fiscalDocumentReservationService;
        private readonly IFiscalSignatureService fiscalSignatureService;
        private readonly IFiscalQrPayloadBuilder fiscalQrPayloadBuilder;
        private readonly IFiscalQrCodeImageGenerator fiscalQrCodeImageGenerator;
        private readonly FiscalReceiptViewModelFactory fiscalReceiptViewModelFactory;
        private readonly FiscalReceiptPdfService fiscalReceiptPdfService;

        /// <summary>
        /// Initializes a new instance of the <see cref="FiscalDocumentCorrectionService"/> class.
        /// </summary>
        /// <param name="applicationDbContext">The tenant fiscal persistence context.</param>
        /// <param name="httpContextAccessor">The middleware-resolved tenant context.</param>
        /// <param name="fiscalScopeResolver">The trusted issuer and series scope resolver.</param>
        /// <param name="fiscalDocumentReservationService">The sequence reservation service.</param>
        /// <param name="fiscalSignatureService">The fiscal signature service.</param>
        /// <param name="fiscalQrPayloadBuilder">The official QR payload builder.</param>
        /// <param name="fiscalQrCodeImageGenerator">The QR PNG generator.</param>
        /// <param name="fiscalReceiptViewModelFactory">The immutable receipt projection factory.</param>
        /// <param name="fiscalReceiptPdfService">The immutable fiscal PDF service.</param>
        public FiscalDocumentCorrectionService(
            ApplicationDbContext applicationDbContext,
            IHttpContextAccessor httpContextAccessor,
            IFiscalScopeResolver fiscalScopeResolver,
            IFiscalDocumentReservationService fiscalDocumentReservationService,
            IFiscalSignatureService fiscalSignatureService,
            IFiscalQrPayloadBuilder fiscalQrPayloadBuilder,
            IFiscalQrCodeImageGenerator fiscalQrCodeImageGenerator,
            FiscalReceiptViewModelFactory fiscalReceiptViewModelFactory,
            FiscalReceiptPdfService fiscalReceiptPdfService)
        {
            this.applicationDbContext = applicationDbContext ?? throw new ArgumentNullException(nameof(applicationDbContext));
            this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            this.fiscalScopeResolver = fiscalScopeResolver ?? throw new ArgumentNullException(nameof(fiscalScopeResolver));
            this.fiscalDocumentReservationService = fiscalDocumentReservationService ?? throw new ArgumentNullException(nameof(fiscalDocumentReservationService));
            this.fiscalSignatureService = fiscalSignatureService ?? throw new ArgumentNullException(nameof(fiscalSignatureService));
            this.fiscalQrPayloadBuilder = fiscalQrPayloadBuilder ?? throw new ArgumentNullException(nameof(fiscalQrPayloadBuilder));
            this.fiscalQrCodeImageGenerator = fiscalQrCodeImageGenerator ?? throw new ArgumentNullException(nameof(fiscalQrCodeImageGenerator));
            this.fiscalReceiptViewModelFactory = fiscalReceiptViewModelFactory ?? throw new ArgumentNullException(nameof(fiscalReceiptViewModelFactory));
            this.fiscalReceiptPdfService = fiscalReceiptPdfService ?? throw new ArgumentNullException(nameof(fiscalReceiptPdfService));
        }

        /// <inheritdoc />
        public FiscalDocument CreateCorrection(FiscalDocumentCorrectionRequest request)
        {
            this.ValidateRequest(request);
            Tenant tenant = this.GetTrustedTenant();
            FiscalDocument original = this.LoadIssuedOriginal(request.OriginalFiscalDocumentId);
            this.RejectExistingCorrection(original.Id, request.IdempotencyKey);
            Donation donation = this.LoadPersistedDonation(original.DonationId);
            FiscalScope fiscalScope = this.fiscalScopeResolver.Resolve(tenant, donation);
            this.ValidateScope(tenant, original, fiscalScope);

            FiscalDocument correction = this.fiscalDocumentReservationService.ReserveCorrection(
                new FiscalDocumentReservationRequest
                {
                    DonationId = donation.Id,
                    SeriesId = fiscalScope.FiscalDocumentSeries.Id,
                    IdempotencyKey = request.IdempotencyKey,
                    FiscalScope = fiscalScope,
                },
                original.Id);
            if (correction == null)
            {
                throw new InvalidOperationException("Fiscal correction reservation did not return a document.");
            }

            this.ValidateReservedCorrection(correction, original, fiscalScope);
            if (correction.Status == FiscalDocumentStatus.Issued)
            {
                return correction;
            }

            this.CopyOriginalSnapshots(original, correction);
            this.CopyOriginalLines(original, correction);
            this.ApplySignature(correction, this.fiscalSignatureService.Sign(new FiscalSignatureRequest
            {
                FiscalDocument = correction,
                SigningKeyIdentifier = correction.SigningKeyIdentifier,
                SigningKeyVersion = correction.SigningKeyVersion,
                SigningAlgorithm = correction.SigningAlgorithm,
            }));

            string qrPayload = this.fiscalQrPayloadBuilder.Build(this.CreateQrSnapshot(correction, fiscalScope));
            byte[] qrPng = this.fiscalQrCodeImageGenerator.GeneratePng(qrPayload);
            if (string.IsNullOrWhiteSpace(qrPayload) || qrPng == null || qrPng.Length == 0)
            {
                throw new InvalidOperationException("Fiscal correction QR generation returned incomplete evidence.");
            }

            correction.QrPayload = qrPayload;
            correction.QrPng = qrPng;
            correction.Events ??= new System.Collections.Generic.List<FiscalDocumentEvent>();
            correction.Events.Add(new FiscalDocumentEvent
            {
                EventType = "CorrectionCreated",
                OccurredAtUtc = DateTimeOffset.UtcNow,
                Reason = request.Motive.Trim(),
            });
            FiscalReceiptViewModel viewModel = this.fiscalReceiptViewModelFactory.Create(correction);
            this.fiscalReceiptPdfService.GetOrCreate(correction, viewModel);
            correction.Status = FiscalDocumentStatus.Issued;
            this.applicationDbContext.SaveChanges();
            return correction;
        }

        private void ApplySignature(FiscalDocument fiscalDocument, FiscalSignatureResult signature)
        {
            if (signature == null || string.IsNullOrWhiteSpace(signature.Hash) || string.IsNullOrWhiteSpace(signature.HashControl))
            {
                throw new InvalidOperationException("Fiscal correction signing returned incomplete signature evidence.");
            }

            fiscalDocument.PreviousHash = signature.PreviousHash;
            fiscalDocument.Hash = signature.Hash;
            fiscalDocument.HashControl = signature.HashControl;
            fiscalDocument.SigningKeyIdentifier = signature.SigningKeyIdentifier;
            fiscalDocument.SigningKeyVersion = signature.SigningKeyVersion;
            fiscalDocument.SigningAlgorithm = signature.SigningAlgorithm;
        }

        private void CopyOriginalLines(FiscalDocument original, FiscalDocument correction)
        {
            if ((correction.Lines?.Any() ?? false) ||
                this.applicationDbContext.FiscalDocumentLines.Any(line => line.FiscalDocumentId == correction.Id))
            {
                return;
            }

            var lines = original.Lines.Select(line => new FiscalDocumentLine
            {
                FiscalDocumentId = correction.Id,
                LineNumber = line.LineNumber,
                ProductCode = line.ProductCode,
                Description = line.Description,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                TaxRate = line.TaxRate,
                TaxExemptionReason = line.TaxExemptionReason,
                NetTotal = line.NetTotal,
                TaxTotal = line.TaxTotal,
                GrossTotal = line.GrossTotal,
            }).ToList();
            correction.Lines = lines;
            this.applicationDbContext.FiscalDocumentLines.AddRange(lines);
        }

        private void CopyOriginalSnapshots(FiscalDocument original, FiscalDocument correction)
        {
            correction.DonorName = original.DonorName;
            correction.DonorTaxRegistrationNumber = original.DonorTaxRegistrationNumber;
            correction.IssuerLegalName = original.IssuerLegalName;
            correction.IssuerTaxRegistrationNumber = original.IssuerTaxRegistrationNumber;
            correction.IssuerLegalAddress = original.IssuerLegalAddress;
            correction.TaxRule = original.TaxRule;
            correction.NetTotal = original.NetTotal;
            correction.TaxTotal = original.TaxTotal;
            correction.GrossTotal = original.GrossTotal;
            correction.SoftwareCertificateNumber = original.SoftwareCertificateNumber;
            correction.PdfTemplateVersion = original.PdfTemplateVersion;
            correction.OriginalFiscalDocument = original;
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

        private Tenant GetTrustedTenant()
        {
            Tenant tenant = this.httpContextAccessor.HttpContext?.GetTenant();
            if (tenant == null || ReferenceEquals(tenant, Tenant.EmptyTenant) || tenant.Id <= 0)
            {
                throw new InvalidOperationException("Fiscal correction requires a trusted current tenant.");
            }

            return tenant;
        }

        private FiscalDocument LoadIssuedOriginal(int originalFiscalDocumentId)
        {
            FiscalDocument original = this.applicationDbContext.FiscalDocuments
                .Include(document => document.Lines)
                .SingleOrDefault(document => document.Id == originalFiscalDocumentId);
            if (original == null || original.Kind != FiscalDocumentKind.Original || original.Status != FiscalDocumentStatus.Issued)
            {
                throw new InvalidOperationException("Fiscal correction requires an issued original fiscal document.");
            }

            return original;
        }

        private Donation LoadPersistedDonation(int donationId)
        {
            Donation donation = this.applicationDbContext.Donations
                .Include(item => item.FoodBank)
                .SingleOrDefault(item => item.Id == donationId);
            if (donation == null)
            {
                throw new InvalidOperationException("Fiscal correction requires the original persisted donation.");
            }

            return donation;
        }

        private void RejectExistingCorrection(int originalFiscalDocumentId, string idempotencyKey)
        {
            FiscalDocument correction = this.applicationDbContext.FiscalDocuments
                .SingleOrDefault(document => document.OriginalFiscalDocumentId == originalFiscalDocumentId);
            if (correction != null && !string.Equals(correction.IdempotencyKey, idempotencyKey, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("An original fiscal document can have only one correction.");
            }
        }

        private void ValidateRequest(FiscalDocumentCorrectionRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.OriginalFiscalDocumentId <= 0 || string.IsNullOrWhiteSpace(request.IdempotencyKey) ||
                request.IdempotencyKey.Length > 128 || string.IsNullOrWhiteSpace(request.Motive) || request.Motive.Trim().Length > 1_024)
            {
                throw new InvalidOperationException("Fiscal correction requires an original document, idempotency key and nonblank statutory motive.");
            }
        }

        private void ValidateReservedCorrection(FiscalDocument correction, FiscalDocument original, FiscalScope fiscalScope)
        {
            if (correction.Kind != FiscalDocumentKind.Correction ||
                correction.OriginalFiscalDocumentId != original.Id ||
                correction.DonationId != original.DonationId ||
                correction.FiscalIssuerProfileId != original.FiscalIssuerProfileId ||
                correction.InvoicingStrategy != original.InvoicingStrategy ||
                correction.FoodBankId != original.FoodBankId ||
                correction.SeriesId != fiscalScope.FiscalDocumentSeries.Id)
            {
                throw new InvalidOperationException("Fiscal correction reservation did not preserve the original fiscal scope.");
            }
        }

        private void ValidateScope(Tenant tenant, FiscalDocument original, FiscalScope fiscalScope)
        {
            bool hasValidFoodBankScope = fiscalScope != null &&
                ((tenant.InvoicingStrategy == InvoicingStrategy.SingleInvoiceTable && !fiscalScope.FoodBankId.HasValue) ||
                (tenant.InvoicingStrategy == InvoicingStrategy.MultipleTablesPerFoodBank && fiscalScope.FoodBankId.HasValue));
            if (fiscalScope == null || fiscalScope.TenantId != tenant.Id ||
                fiscalScope.InvoicingStrategy != tenant.InvoicingStrategy || !hasValidFoodBankScope ||
                fiscalScope.FiscalIssuerProfile == null || fiscalScope.FiscalDocumentSeries == null ||
                fiscalScope.FiscalDocumentSeries.Id <= 0 ||
                fiscalScope.FiscalIssuerProfile.Id != original.FiscalIssuerProfileId ||
                fiscalScope.FiscalDocumentSeries.FiscalIssuerProfileId != original.FiscalIssuerProfileId ||
                original.InvoicingStrategy != fiscalScope.InvoicingStrategy ||
                original.FoodBankId != fiscalScope.FoodBankId)
            {
                throw new InvalidOperationException("Fiscal correction scope does not match the trusted tenant and original document.");
            }
        }
    }
}
