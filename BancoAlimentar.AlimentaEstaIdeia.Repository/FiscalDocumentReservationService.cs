// -----------------------------------------------------------------------
// <copyright file="FiscalDocumentReservationService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Data;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Storage;

    /// <summary>
    /// Reserves fiscal series numbers and creates draft fiscal documents transactionally.
    /// </summary>
    public class FiscalDocumentReservationService : IFiscalDocumentReservationService
    {
        private readonly ApplicationDbContext applicationDbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="FiscalDocumentReservationService"/> class.
        /// </summary>
        /// <param name="applicationDbContext">The current tenant application database.</param>
        public FiscalDocumentReservationService(ApplicationDbContext applicationDbContext)
        {
            this.applicationDbContext = applicationDbContext ?? throw new ArgumentNullException(nameof(applicationDbContext));
        }

        /// <inheritdoc />
        public FiscalDocument ReserveOriginal(FiscalDocumentReservationRequest request)
        {
            FiscalScope fiscalScope = this.ValidateRequest(request);

            FiscalDocument existingDocument = this.FindExistingDocument(request.IdempotencyKey);
            if (existingDocument != null)
            {
                this.ValidateExistingDocumentRequest(existingDocument, request, fiscalScope);
                return existingDocument;
            }

            IDbContextTransaction transaction = this.applicationDbContext.Database.IsRelational()
                ? this.applicationDbContext.Database.BeginTransaction(IsolationLevel.Serializable)
                : null;

            try
            {
                existingDocument = this.FindExistingDocument(request.IdempotencyKey);
                if (existingDocument != null)
                {
                    this.ValidateExistingDocumentRequest(existingDocument, request, fiscalScope);
                    transaction?.Commit();
                    return existingDocument;
                }

                Donation donation = this.applicationDbContext.Donations
                    .Include(item => item.FoodBank)
                    .SingleOrDefault(item => item.Id == request.DonationId);
                if (donation == null)
                {
                    throw new InvalidOperationException("Fiscal document reservation requires a persisted donation.");
                }

                FiscalDocumentSeries series = this.applicationDbContext.FiscalDocumentSeries
                    .Include(item => item.FiscalIssuerProfile)
                    .SingleOrDefault(item => item.Id == request.SeriesId);
                if (!this.IsEligibleSeries(series, DateTimeOffset.UtcNow))
                {
                    throw new InvalidOperationException("Fiscal document reservation requires an eligible active series.");
                }

                InvoicingStrategy invoicingStrategy = this.GetInvoicingStrategy(series.FiscalIssuerProfile);
                int? foodBankId = this.GetFoodBankId(series.FiscalIssuerProfile, donation);
                this.ValidateFiscalScope(fiscalScope, series, invoicingStrategy, foodBankId);
                int sequence = series.NextSequence;
                series.NextSequence++;
                var fiscalDocument = new FiscalDocument
                {
                    DonationId = donation.Id,
                    SeriesId = series.Id,
                    FiscalIssuerProfileId = series.FiscalIssuerProfileId,
                    InvoicingStrategy = invoicingStrategy,
                    FoodBankId = foodBankId,
                    Kind = FiscalDocumentKind.Original,
                    Status = FiscalDocumentStatus.Draft,
                    Sequence = sequence,
                    DocumentNumber = $"{series.SeriesCode}/{sequence}",
                    Atcud = $"{series.AtcudValidationCode}-{sequence}",
                    IdempotencyKey = request.IdempotencyKey,
                    IssuedAtUtc = DateTimeOffset.UtcNow,
                    Hash = string.Empty,
                    HashControl = string.Empty,
                    SigningKeyVersion = series.FiscalIssuerProfile.SigningKeyVersion,
                    QrPayload = string.Empty,
                    SoftwareCertificateNumber = series.FiscalIssuerProfile.SoftwareCertificateNumber,
                    IssuerLegalName = series.FiscalIssuerProfile.LegalName,
                    IssuerTaxRegistrationNumber = series.FiscalIssuerProfile.TaxRegistrationNumber,
                    IssuerLegalAddress = series.FiscalIssuerProfile.LegalAddress,
                    TaxRule = series.FiscalIssuerProfile.TaxRule,
                };

                this.applicationDbContext.FiscalDocuments.Add(fiscalDocument);
                this.applicationDbContext.SaveChanges();
                transaction?.Commit();
                return fiscalDocument;
            }
            catch
            {
                transaction?.Rollback();
                throw;
            }
            finally
            {
                transaction?.Dispose();
            }
        }

        private FiscalDocument FindExistingDocument(string idempotencyKey)
        {
            return this.applicationDbContext.FiscalDocuments
                .SingleOrDefault(item => item.IdempotencyKey == idempotencyKey);
        }

        private FiscalScope ValidateRequest(FiscalDocumentReservationRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.DonationId <= 0 || request.SeriesId <= 0 || string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                throw new InvalidOperationException("Fiscal document reservation requires positive donation and series identifiers and an idempotency key.");
            }

            if (request.IdempotencyKey.Length > 128)
            {
                throw new InvalidOperationException("The fiscal document reservation idempotency key is too long.");
            }

            FiscalScope fiscalScope = request.FiscalScope;
            if (fiscalScope == null || fiscalScope.TenantId <= 0 ||
                fiscalScope.FiscalIssuerProfile == null || fiscalScope.FiscalIssuerProfile.Id <= 0 ||
                fiscalScope.FiscalDocumentSeries == null || fiscalScope.FiscalDocumentSeries.Id <= 0 ||
                fiscalScope.FiscalDocumentSeries.Id != request.SeriesId)
            {
                throw new InvalidOperationException("Fiscal document reservation requires a valid fiscal scope.");
            }

            return fiscalScope;
        }

        private void ValidateExistingDocumentRequest(
            FiscalDocument fiscalDocument,
            FiscalDocumentReservationRequest request,
            FiscalScope fiscalScope)
        {
            if (fiscalDocument.DonationId != request.DonationId ||
                fiscalDocument.SeriesId != request.SeriesId ||
                fiscalDocument.FiscalIssuerProfileId != fiscalScope.FiscalIssuerProfile.Id ||
                fiscalDocument.SeriesId != fiscalScope.FiscalDocumentSeries.Id ||
                fiscalDocument.FiscalIssuerProfileId != fiscalScope.FiscalDocumentSeries.FiscalIssuerProfileId ||
                fiscalDocument.InvoicingStrategy != fiscalScope.InvoicingStrategy ||
                fiscalDocument.FoodBankId != fiscalScope.FoodBankId)
            {
                throw new InvalidOperationException(
                    "A fiscal document reservation idempotency key cannot be reused for a different donation, series, or fiscal scope.");
            }
        }

        private void ValidateFiscalScope(
            FiscalScope fiscalScope,
            FiscalDocumentSeries fiscalDocumentSeries,
            InvoicingStrategy invoicingStrategy,
            int? foodBankId)
        {
            if (fiscalScope.FiscalIssuerProfile.Id != fiscalDocumentSeries.FiscalIssuerProfileId ||
                fiscalScope.FiscalDocumentSeries.Id != fiscalDocumentSeries.Id ||
                fiscalScope.FiscalDocumentSeries.FiscalIssuerProfileId != fiscalDocumentSeries.FiscalIssuerProfileId ||
                fiscalScope.InvoicingStrategy != invoicingStrategy ||
                fiscalScope.FoodBankId != foodBankId)
            {
                throw new InvalidOperationException(
                    "The fiscal document reservation request does not match the trusted fiscal scope.");
            }
        }

        private bool IsEligibleSeries(FiscalDocumentSeries series, DateTimeOffset nowUtc)
        {
            FiscalIssuerProfile issuerProfile = series?.FiscalIssuerProfile;
            return series != null &&
                issuerProfile != null &&
                series.Status == FiscalSeriesStatus.Active &&
                series.StartsAtUtc <= nowUtc &&
                !series.ClosedAtUtc.HasValue &&
                series.FirstSequence > 0 &&
                series.NextSequence >= series.FirstSequence &&
                (!series.LastSequence.HasValue || series.NextSequence <= series.LastSequence.Value) &&
                series.ProcessingMethod == FiscalProcessingMethod.CertifiedSoftware &&
                !string.IsNullOrWhiteSpace(series.SeriesCode) &&
                !string.IsNullOrWhiteSpace(series.SaftDocumentType) &&
                !string.IsNullOrWhiteSpace(series.AtcudValidationCode) &&
                !string.IsNullOrWhiteSpace(series.AtCommunicationReference) &&
                issuerProfile.IsActive &&
                issuerProfile.ValidFromUtc <= nowUtc &&
                (!issuerProfile.ValidToUtc.HasValue || issuerProfile.ValidToUtc.Value > nowUtc) &&
                !string.IsNullOrWhiteSpace(issuerProfile.LegalName) &&
                !string.IsNullOrWhiteSpace(issuerProfile.LegalAddress) &&
                !string.IsNullOrWhiteSpace(issuerProfile.TaxRegistrationNumber) &&
                !string.IsNullOrWhiteSpace(issuerProfile.TaxRule) &&
                !string.IsNullOrWhiteSpace(issuerProfile.SoftwareCertificateNumber) &&
                !string.IsNullOrWhiteSpace(issuerProfile.SigningKeyVersion) &&
                ((issuerProfile.ScopeKind == FiscalScopeKind.Tenant && !issuerProfile.FoodBankId.HasValue) ||
                    (issuerProfile.ScopeKind == FiscalScopeKind.FoodBank && issuerProfile.FoodBankId.HasValue)) &&
                issuerProfile.SaftDocumentType == series.SaftDocumentType;
        }

        private InvoicingStrategy GetInvoicingStrategy(FiscalIssuerProfile issuerProfile)
        {
            if (issuerProfile.ScopeKind == FiscalScopeKind.Tenant)
            {
                return InvoicingStrategy.SingleInvoiceTable;
            }

            if (issuerProfile.ScopeKind == FiscalScopeKind.FoodBank)
            {
                return InvoicingStrategy.MultipleTablesPerFoodBank;
            }

            throw new InvalidOperationException("Fiscal document reservation requires a valid issuer scope.");
        }

        private int? GetFoodBankId(FiscalIssuerProfile issuerProfile, Donation donation)
        {
            if (issuerProfile.ScopeKind != FiscalScopeKind.FoodBank)
            {
                return null;
            }

            if (!issuerProfile.FoodBankId.HasValue || donation.FoodBank == null ||
                donation.FoodBank.Id != issuerProfile.FoodBankId.Value)
            {
                throw new InvalidOperationException("The fiscal series issuer scope does not match the donation FoodBank.");
            }

            return issuerProfile.FoodBankId.Value;
        }
    }
}
