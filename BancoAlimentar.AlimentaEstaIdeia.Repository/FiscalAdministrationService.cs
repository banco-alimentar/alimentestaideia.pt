// -----------------------------------------------------------------------
// <copyright file="FiscalAdministrationService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    /// Applies tenant-scoped, non-secret fiscal issuer and series lifecycle changes.
    /// </summary>
    public class FiscalAdministrationService : IFiscalAdministrationService
    {
        private readonly ApplicationDbContext applicationDbContext;
        private readonly IFiscalAuditService fiscalAuditService;
        private readonly IHttpContextAccessor httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="FiscalAdministrationService"/> class.
        /// </summary>
        /// <param name="applicationDbContext">The current tenant fiscal persistence context.</param>
        /// <param name="httpContextAccessor">The middleware-resolved tenant context.</param>
        public FiscalAdministrationService(
            ApplicationDbContext applicationDbContext,
            IHttpContextAccessor httpContextAccessor)
        {
            this.applicationDbContext = applicationDbContext ?? throw new ArgumentNullException(nameof(applicationDbContext));
            this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FiscalAdministrationService"/> class.
        /// </summary>
        /// <param name="applicationDbContext">The current tenant fiscal persistence context.</param>
        /// <param name="httpContextAccessor">The middleware-resolved tenant context.</param>
        /// <param name="fiscalAuditService">The append-only fiscal audit recorder.</param>
        public FiscalAdministrationService(
            ApplicationDbContext applicationDbContext,
            IHttpContextAccessor httpContextAccessor,
            IFiscalAuditService fiscalAuditService)
            : this(applicationDbContext, httpContextAccessor)
        {
            this.fiscalAuditService = fiscalAuditService ?? throw new ArgumentNullException(nameof(fiscalAuditService));
        }

        /// <inheritdoc />
        public FiscalIssuerProfile CreateIssuerProfile(FiscalIssuerProfileAdministrationRequest request)
        {
            Tenant tenant = this.GetTrustedTenant();
            this.ValidateIssuerRequest(request, tenant);
            if (request.Id != 0)
            {
                throw new InvalidOperationException("A new fiscal issuer profile cannot have an existing identifier.");
            }

            var profile = new FiscalIssuerProfile();
            this.CopyIssuerRequest(request, profile);
            this.applicationDbContext.FiscalIssuerProfiles.Add(profile);
            this.applicationDbContext.SaveChanges();
            this.RecordAudit("IssuerProfileCreated", profile.Id, null, null);
            return profile;
        }

        /// <inheritdoc />
        public FiscalIssuerProfile UpdateIssuerProfile(FiscalIssuerProfileAdministrationRequest request)
        {
            Tenant tenant = this.GetTrustedTenant();
            this.ValidateIssuerRequest(request, tenant);
            if (request.Id <= 0)
            {
                throw new InvalidOperationException("Fiscal issuer profile updates require an identifier.");
            }

            FiscalIssuerProfile profile = this.applicationDbContext.FiscalIssuerProfiles
                .SingleOrDefault(item => item.Id == request.Id);
            if (profile == null)
            {
                throw new InvalidOperationException("Fiscal issuer profile was not found.");
            }

            if (this.applicationDbContext.FiscalDocuments.Any(document =>
                document.FiscalIssuerProfileId == profile.Id && document.Status != FiscalDocumentStatus.Draft))
            {
                throw new InvalidOperationException("Fiscal issuer profile snapshots cannot be changed after fiscal issuance.");
            }

            this.CopyIssuerRequest(request, profile);
            this.applicationDbContext.SaveChanges();
            this.RecordAudit("IssuerProfileUpdated", profile.Id, null, null);
            return profile;
        }

        /// <inheritdoc />
        public FiscalDocumentSeries ConfigureSeries(FiscalDocumentSeriesAdministrationRequest request)
        {
            Tenant tenant = this.GetTrustedTenant();
            this.ValidateSeriesRequest(request, tenant);
            FiscalIssuerProfile profile = this.applicationDbContext.FiscalIssuerProfiles
                .SingleOrDefault(item => item.Id == request.FiscalIssuerProfileId);
            if (profile == null)
            {
                throw new InvalidOperationException("Fiscal series requires a persisted issuer profile.");
            }

            FiscalDocumentSeries series;
            if (request.Id == 0)
            {
                series = new FiscalDocumentSeries();
                this.applicationDbContext.FiscalDocumentSeries.Add(series);
            }
            else
            {
                series = this.applicationDbContext.FiscalDocumentSeries.SingleOrDefault(item => item.Id == request.Id);
                if (series == null)
                {
                    throw new InvalidOperationException("Fiscal document series was not found.");
                }

                if (this.applicationDbContext.FiscalDocuments.Any(document =>
                    document.SeriesId == series.Id && document.Status != FiscalDocumentStatus.Draft))
                {
                    throw new InvalidOperationException("A series used by issued fiscal evidence cannot be modified; close it instead.");
                }
            }

            series.FiscalIssuerProfileId = profile.Id;
            series.SeriesCode = request.SeriesCode.Trim();
            series.SaftDocumentType = request.SaftDocumentType.Trim();
            series.ProcessingMethod = request.ProcessingMethod;
            series.FirstSequence = request.FirstSequence;
            series.NextSequence = request.NextSequence;
            series.LastSequence = request.LastSequence;
            series.AtcudValidationCode = request.AtcudValidationCode.Trim();
            series.AtCommunicationReference = request.AtCommunicationReference.Trim();
            series.StartsAtUtc = request.StartsAtUtc;
            series.Status = request.Status;
            this.applicationDbContext.SaveChanges();
            this.RecordAudit("SeriesConfigured", profile.Id, series.Id, null);
            return series;
        }

        /// <inheritdoc />
        public FiscalDocumentSeries CloseSeries(int fiscalDocumentSeriesId)
        {
            Tenant tenant = this.GetTrustedTenant();
            if (fiscalDocumentSeriesId <= 0)
            {
                throw new InvalidOperationException("Closing a fiscal series requires an identifier.");
            }

            FiscalDocumentSeries series = this.applicationDbContext.FiscalDocumentSeries
                .Include(item => item.FiscalIssuerProfile)
                .SingleOrDefault(item => item.Id == fiscalDocumentSeriesId);
            if (series == null || series.FiscalIssuerProfile == null || !this.ProfileMatchesTenant(series.FiscalIssuerProfile, tenant))
            {
                throw new InvalidOperationException("Fiscal series does not belong to the trusted tenant scope.");
            }

            if (series.Status != FiscalSeriesStatus.Closed)
            {
                series.Status = FiscalSeriesStatus.Closed;
                series.ClosedAtUtc = DateTimeOffset.UtcNow;
                this.applicationDbContext.SaveChanges();
                this.RecordAudit("SeriesClosed", series.FiscalIssuerProfileId, series.Id, null);
            }

            return series;
        }

        /// <inheritdoc />
        public string ExportSaft(int fiscalIssuerProfileId, DateOnly startDate, DateOnly endDate)
        {
            Tenant tenant = this.GetTrustedTenant();
            if (fiscalIssuerProfileId <= 0)
            {
                throw new InvalidOperationException("SAF-T export requires a fiscal issuer profile identifier.");
            }

            FiscalIssuerProfile profile = this.applicationDbContext.FiscalIssuerProfiles
                .SingleOrDefault(item => item.Id == fiscalIssuerProfileId);
            if (profile == null || !this.ProfileMatchesTenant(profile, tenant))
            {
                throw new InvalidOperationException("Fiscal issuer profile does not belong to the trusted tenant scope.");
            }

            string saft = new SaftExportService(this.applicationDbContext).Export(new SaftExportRequest
            {
                Tenant = tenant,
                FiscalIssuerProfileId = fiscalIssuerProfileId,
                StartDate = startDate,
                EndDate = endDate,
            });
            this.RecordAudit("SaftExported", fiscalIssuerProfileId, null, null);
            return saft;
        }

        private void CopyIssuerRequest(FiscalIssuerProfileAdministrationRequest request, FiscalIssuerProfile profile)
        {
            profile.ScopeKind = request.ScopeKind;
            profile.FoodBankId = request.FoodBankId;
            profile.LegalName = request.LegalName.Trim();
            profile.LegalAddress = request.LegalAddress.Trim();
            profile.TaxRegistrationNumber = request.TaxRegistrationNumber.Trim();
            profile.SaftDocumentType = request.SaftDocumentType.Trim();
            profile.TaxRule = request.TaxRule.Trim();
            profile.SoftwareCertificateNumber = request.SoftwareCertificateNumber.Trim();
            profile.SigningKeyIdentifier = request.SigningKeyIdentifier.Trim();
            profile.SigningKeyVersion = request.SigningKeyVersion.Trim();
            profile.ValidFromUtc = request.ValidFromUtc;
            profile.ValidToUtc = request.ValidToUtc;
            profile.IsActive = request.IsActive;
        }

        private Tenant GetTrustedTenant()
        {
            Tenant tenant = this.httpContextAccessor.HttpContext?.GetTenant();
            if (tenant == null || ReferenceEquals(tenant, Tenant.EmptyTenant) || tenant.Id <= 0)
            {
                throw new InvalidOperationException("Fiscal administration requires a trusted current tenant.");
            }

            return tenant;
        }

        private bool ProfileMatchesTenant(FiscalIssuerProfile profile, Tenant tenant)
        {
            return (tenant.InvoicingStrategy == InvoicingStrategy.SingleInvoiceTable &&
                    profile.ScopeKind == FiscalScopeKind.Tenant && !profile.FoodBankId.HasValue) ||
                (tenant.InvoicingStrategy == InvoicingStrategy.MultipleTablesPerFoodBank &&
                    profile.ScopeKind == FiscalScopeKind.FoodBank && profile.FoodBankId.HasValue);
        }

        private void ValidateIssuerRequest(FiscalIssuerProfileAdministrationRequest request, Tenant tenant)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.LegalName) ||
                string.IsNullOrWhiteSpace(request.LegalAddress) || string.IsNullOrWhiteSpace(request.TaxRegistrationNumber) ||
                string.IsNullOrWhiteSpace(request.SaftDocumentType) || string.IsNullOrWhiteSpace(request.TaxRule) ||
                string.IsNullOrWhiteSpace(request.SoftwareCertificateNumber) || string.IsNullOrWhiteSpace(request.SigningKeyIdentifier) ||
                string.IsNullOrWhiteSpace(request.SigningKeyVersion) || request.ValidFromUtc == default ||
                (request.ValidToUtc.HasValue && request.ValidToUtc <= request.ValidFromUtc) ||
                !this.ProfileRequestMatchesTenant(request, tenant))
            {
                throw new InvalidOperationException("Fiscal issuer profile configuration is incomplete or outside the current tenant scope.");
            }
        }

        private bool ProfileRequestMatchesTenant(FiscalIssuerProfileAdministrationRequest request, Tenant tenant)
        {
            return (tenant.InvoicingStrategy == InvoicingStrategy.SingleInvoiceTable &&
                    request.ScopeKind == FiscalScopeKind.Tenant && !request.FoodBankId.HasValue) ||
                (tenant.InvoicingStrategy == InvoicingStrategy.MultipleTablesPerFoodBank &&
                    request.ScopeKind == FiscalScopeKind.FoodBank && request.FoodBankId.HasValue);
        }

        private void ValidateSeriesRequest(FiscalDocumentSeriesAdministrationRequest request, Tenant tenant)
        {
            if (request == null || request.FiscalIssuerProfileId <= 0 || string.IsNullOrWhiteSpace(request.SeriesCode) ||
                string.IsNullOrWhiteSpace(request.SaftDocumentType) || request.ProcessingMethod != FiscalProcessingMethod.CertifiedSoftware ||
                request.FirstSequence <= 0 || request.NextSequence < request.FirstSequence ||
                (request.LastSequence.HasValue && request.LastSequence.Value < request.NextSequence) ||
                string.IsNullOrWhiteSpace(request.AtcudValidationCode) || string.IsNullOrWhiteSpace(request.AtCommunicationReference) ||
                request.StartsAtUtc == default || request.Status != FiscalSeriesStatus.Active || tenant == null)
            {
                throw new InvalidOperationException("Fiscal series must be active, certified and communicated to AT before it can be configured.");
            }
        }

        private void RecordAudit(string action, int? fiscalIssuerProfileId, int? fiscalDocumentSeriesId, int? fiscalDocumentId)
        {
            this.fiscalAuditService?.Record(new FiscalAuditEventRequest
            {
                Action = action,
                Result = "Succeeded",
                FiscalIssuerProfileId = fiscalIssuerProfileId,
                FiscalDocumentSeriesId = fiscalDocumentSeriesId,
                FiscalDocumentId = fiscalDocumentId,
            });
        }
    }
}
