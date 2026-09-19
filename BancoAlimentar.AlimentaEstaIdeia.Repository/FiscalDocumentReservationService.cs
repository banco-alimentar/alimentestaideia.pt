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
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Storage;

    /// <summary>
    /// Reserves fiscal series numbers and creates draft fiscal documents transactionally.
    /// </summary>
    public class FiscalDocumentReservationService : IFiscalDocumentReservationService
    {
        private const int MaximumReservationAttempts = 2;

        private readonly ApplicationDbContext applicationDbContext;
        private readonly IFiscalScopeResolver fiscalScopeResolver;
        private readonly IHttpContextAccessor httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="FiscalDocumentReservationService"/> class.
        /// </summary>
        /// <param name="applicationDbContext">The current tenant application database.</param>
        /// <param name="httpContextAccessor">The accessor for the middleware-resolved tenant.</param>
        public FiscalDocumentReservationService(
            ApplicationDbContext applicationDbContext,
            IHttpContextAccessor httpContextAccessor)
        {
            this.applicationDbContext = applicationDbContext ?? throw new ArgumentNullException(nameof(applicationDbContext));
            this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FiscalDocumentReservationService"/> class.
        /// </summary>
        /// <param name="applicationDbContext">The current tenant application database.</param>
        /// <param name="httpContextAccessor">The accessor for the middleware-resolved tenant.</param>
        /// <param name="fiscalScopeResolver">The resolver for the trusted issuer and series scope.</param>
        public FiscalDocumentReservationService(
            ApplicationDbContext applicationDbContext,
            IHttpContextAccessor httpContextAccessor,
            IFiscalScopeResolver fiscalScopeResolver)
        {
            this.applicationDbContext = applicationDbContext ?? throw new ArgumentNullException(nameof(applicationDbContext));
            this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            this.fiscalScopeResolver = fiscalScopeResolver ?? throw new ArgumentNullException(nameof(fiscalScopeResolver));
        }

        /// <inheritdoc />
        public FiscalDocument ReserveOriginal(FiscalDocumentReservationRequest request)
        {
            Tenant currentTenant = this.GetTrustedTenant();
            FiscalScope requestedFiscalScope = this.ValidateRequest(request);
            Donation donation = this.LoadPersistedDonation(request.DonationId);
            FiscalScope fiscalScope = requestedFiscalScope;

            if (this.fiscalScopeResolver != null)
            {
                fiscalScope = this.fiscalScopeResolver.Resolve(currentTenant, donation);
                this.ValidateResolvedFiscalScope(currentTenant, fiscalScope);
                this.ValidateRequestedFiscalScope(requestedFiscalScope, fiscalScope);
            }
            else
            {
                this.ValidateResolvedFiscalScope(currentTenant, requestedFiscalScope);
            }

            for (int attempt = 0; attempt < MaximumReservationAttempts; attempt++)
            {
                FiscalDocument fiscalDocument = this.ReserveOriginalAttempt(request, fiscalScope, donation);
                if (fiscalDocument != null)
                {
                    return fiscalDocument;
                }
            }

            throw new InvalidOperationException("Fiscal document reservation could not be completed after a concurrency conflict.");
        }

        private FiscalDocument ReserveOriginalAttempt(
            FiscalDocumentReservationRequest request,
            FiscalScope fiscalScope,
            Donation donation)
        {
            FiscalDocument existingDocument = this.FindExistingDocument(request.IdempotencyKey);
            if (existingDocument != null)
            {
                this.ValidateExistingDocumentRequest(existingDocument, request, fiscalScope);
                this.ValidateExistingFiscalScope(existingDocument, fiscalScope);
                return existingDocument;
            }

            IDbContextTransaction transaction = this.applicationDbContext.Database.IsRelational()
                ? this.applicationDbContext.Database.BeginTransaction(IsolationLevel.Serializable)
                : null;
            FiscalDocumentSeries series = null;
            FiscalDocument fiscalDocument = null;

            try
            {
                existingDocument = this.FindExistingDocument(request.IdempotencyKey);
                if (existingDocument != null)
                {
                    this.ValidateExistingDocumentRequest(existingDocument, request, fiscalScope);
                    this.ValidateExistingFiscalScope(existingDocument, fiscalScope);
                    transaction?.Commit();
                    return existingDocument;
                }

                series = this.applicationDbContext.FiscalDocumentSeries
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
                DateTimeOffset issuedAtUtc = DateTimeOffset.UtcNow;
                fiscalDocument = new FiscalDocument
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
                    IssuedAtUtc = issuedAtUtc,
                    InvoiceDate = DateOnly.FromDateTime(issuedAtUtc.UtcDateTime),
                    SystemEntryDate = issuedAtUtc,
                    Hash = string.Empty,
                    HashControl = string.Empty,
                    SigningKeyIdentifier = series.FiscalIssuerProfile.SigningKeyIdentifier,
                    SigningKeyVersion = series.FiscalIssuerProfile.SigningKeyVersion,
                    SigningAlgorithm = "RSA",
                    QrPayload = string.Empty,
                    SoftwareCertificateNumber = series.FiscalIssuerProfile.SoftwareCertificateNumber,
                    IssuerLegalName = series.FiscalIssuerProfile.LegalName,
                    IssuerTaxRegistrationNumber = series.FiscalIssuerProfile.TaxRegistrationNumber,
                    IssuerLegalAddress = series.FiscalIssuerProfile.LegalAddress,
                    TaxRule = series.FiscalIssuerProfile.TaxRule,
                };

                this.applicationDbContext.FiscalDocuments.Add(fiscalDocument);
                try
                {
                    this.applicationDbContext.SaveChanges();
                }
                catch (DbUpdateException)
                {
                    transaction?.Rollback();
                    this.DiscardFailedReservation(fiscalDocument, series);

                    FiscalDocument concurrentDocument = this.FindExistingDocument(request.IdempotencyKey);
                    if (concurrentDocument == null)
                    {
                        return null;
                    }

                    this.ValidateExistingDocumentRequest(concurrentDocument, request, fiscalScope);
                    this.ValidateExistingFiscalScope(concurrentDocument, fiscalScope);
                    return concurrentDocument;
                }

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

        private Tenant GetTrustedTenant()
        {
            Tenant currentTenant = this.httpContextAccessor.HttpContext?.GetTenant();
            if (currentTenant == null || ReferenceEquals(currentTenant, Tenant.EmptyTenant) || currentTenant.Id <= 0)
            {
                throw new InvalidOperationException("Fiscal document reservation requires a trusted current tenant.");
            }

            return currentTenant;
        }

        private Donation LoadPersistedDonation(int donationId)
        {
            Donation donation = this.applicationDbContext.Donations
                .Include(item => item.FoodBank)
                .SingleOrDefault(item => item.Id == donationId);
            if (donation == null)
            {
                throw new InvalidOperationException("Fiscal document reservation requires a persisted donation.");
            }

            return donation;
        }

        private void ValidateResolvedFiscalScope(Tenant currentTenant, FiscalScope fiscalScope)
        {
            if (fiscalScope == null || fiscalScope.TenantId != currentTenant.Id)
            {
                throw new InvalidOperationException(
                    "The resolved fiscal document reservation scope does not match the current tenant.");
            }

            bool hasValidFoodBankScope =
                (currentTenant.InvoicingStrategy == InvoicingStrategy.SingleInvoiceTable &&
                    !fiscalScope.FoodBankId.HasValue) ||
                (currentTenant.InvoicingStrategy == InvoicingStrategy.MultipleTablesPerFoodBank &&
                    fiscalScope.FoodBankId.HasValue);
            if (currentTenant.InvoicingStrategy != fiscalScope.InvoicingStrategy || !hasValidFoodBankScope)
            {
                throw new InvalidOperationException(
                    "The fiscal document reservation scope does not match the current tenant invoicing strategy.");
            }
        }

        private void ValidateRequestedFiscalScope(FiscalScope requestedFiscalScope, FiscalScope resolvedFiscalScope)
        {
            if (requestedFiscalScope.TenantId != resolvedFiscalScope.TenantId ||
                requestedFiscalScope.InvoicingStrategy != resolvedFiscalScope.InvoicingStrategy ||
                requestedFiscalScope.FoodBankId != resolvedFiscalScope.FoodBankId ||
                requestedFiscalScope.FiscalIssuerProfile.Id != resolvedFiscalScope.FiscalIssuerProfile?.Id ||
                requestedFiscalScope.FiscalDocumentSeries.Id != resolvedFiscalScope.FiscalDocumentSeries?.Id ||
                requestedFiscalScope.FiscalDocumentSeries.FiscalIssuerProfileId !=
                    resolvedFiscalScope.FiscalDocumentSeries?.FiscalIssuerProfileId ||
                !this.HasMatchingFiscalConfiguration(
                    requestedFiscalScope,
                    resolvedFiscalScope.FiscalIssuerProfile,
                    resolvedFiscalScope.FiscalDocumentSeries))
            {
                throw new InvalidOperationException(
                    "The fiscal document reservation request scope does not match the internally resolved fiscal scope.");
            }
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
                fiscalScope.FoodBankId != foodBankId ||
                !this.HasMatchingFiscalConfiguration(
                    fiscalScope,
                    fiscalDocumentSeries.FiscalIssuerProfile,
                    fiscalDocumentSeries))
            {
                throw new InvalidOperationException(
                    "The fiscal document reservation request does not match the trusted fiscal scope.");
            }
        }

        private void ValidateExistingFiscalScope(FiscalDocument fiscalDocument, FiscalScope fiscalScope)
        {
            FiscalDocumentSeries fiscalDocumentSeries = this.applicationDbContext.FiscalDocumentSeries
                .Include(item => item.FiscalIssuerProfile)
                .SingleOrDefault(item => item.Id == fiscalDocument.SeriesId);

            if (fiscalDocumentSeries == null || fiscalDocumentSeries.FiscalIssuerProfile == null)
            {
                throw new InvalidOperationException("The fiscal document reservation scope no longer has a persisted series.");
            }

            this.ValidateFiscalScope(
                fiscalScope,
                fiscalDocumentSeries,
                this.GetInvoicingStrategy(fiscalDocumentSeries.FiscalIssuerProfile),
                fiscalDocument.FoodBankId);
        }

        private void DiscardFailedReservation(
            FiscalDocument fiscalDocument,
            FiscalDocumentSeries fiscalDocumentSeries)
        {
            if (fiscalDocument != null && this.applicationDbContext.Entry(fiscalDocument).State != EntityState.Detached)
            {
                this.applicationDbContext.Entry(fiscalDocument).State = EntityState.Detached;
            }

            if (fiscalDocumentSeries != null && this.applicationDbContext.Entry(fiscalDocumentSeries).State != EntityState.Detached)
            {
                this.applicationDbContext.Entry(fiscalDocumentSeries).Reload();
            }
        }

        private bool HasMatchingFiscalConfiguration(
            FiscalScope fiscalScope,
            FiscalIssuerProfile persistedProfile,
            FiscalDocumentSeries fiscalDocumentSeries)
        {
            FiscalIssuerProfile suppliedProfile = fiscalScope.FiscalIssuerProfile;
            FiscalDocumentSeries suppliedSeries = fiscalScope.FiscalDocumentSeries;

            return suppliedProfile.ScopeKind == persistedProfile.ScopeKind &&
                suppliedProfile.FoodBankId == persistedProfile.FoodBankId &&
                suppliedProfile.LegalName == persistedProfile.LegalName &&
                suppliedProfile.LegalAddress == persistedProfile.LegalAddress &&
                suppliedProfile.TaxRegistrationNumber == persistedProfile.TaxRegistrationNumber &&
                suppliedProfile.SaftDocumentType == persistedProfile.SaftDocumentType &&
                suppliedProfile.TaxRule == persistedProfile.TaxRule &&
                suppliedProfile.SoftwareCertificateNumber == persistedProfile.SoftwareCertificateNumber &&
                suppliedProfile.SigningKeyIdentifier == persistedProfile.SigningKeyIdentifier &&
                suppliedProfile.SigningKeyVersion == persistedProfile.SigningKeyVersion &&
                suppliedProfile.IsActive == persistedProfile.IsActive &&
                suppliedProfile.ValidFromUtc == persistedProfile.ValidFromUtc &&
                suppliedProfile.ValidToUtc == persistedProfile.ValidToUtc &&
                suppliedSeries.SeriesCode == fiscalDocumentSeries.SeriesCode &&
                suppliedSeries.SaftDocumentType == fiscalDocumentSeries.SaftDocumentType &&
                suppliedSeries.ProcessingMethod == fiscalDocumentSeries.ProcessingMethod &&
                suppliedSeries.FirstSequence == fiscalDocumentSeries.FirstSequence &&
                suppliedSeries.LastSequence == fiscalDocumentSeries.LastSequence &&
                suppliedSeries.AtcudValidationCode == fiscalDocumentSeries.AtcudValidationCode &&
                suppliedSeries.Status == fiscalDocumentSeries.Status &&
                suppliedSeries.StartsAtUtc == fiscalDocumentSeries.StartsAtUtc &&
                suppliedSeries.ClosedAtUtc == fiscalDocumentSeries.ClosedAtUtc &&
                suppliedSeries.AtCommunicationReference == fiscalDocumentSeries.AtCommunicationReference;
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
                !string.IsNullOrWhiteSpace(issuerProfile.SigningKeyIdentifier) &&
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
