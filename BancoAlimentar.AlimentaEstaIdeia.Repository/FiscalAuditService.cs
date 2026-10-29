// -----------------------------------------------------------------------
// <copyright file="FiscalAuditService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Linq;
    using System.Security.Claims;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Appends compact fiscal operational audit events using the middleware-resolved tenant and actor.
    /// </summary>
    public class FiscalAuditService : IFiscalAuditService
    {
        private readonly ApplicationDbContext applicationDbContext;
        private readonly IHttpContextAccessor httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="FiscalAuditService"/> class.
        /// </summary>
        /// <param name="applicationDbContext">The current tenant fiscal persistence context.</param>
        /// <param name="httpContextAccessor">The middleware-resolved tenant and actor context.</param>
        public FiscalAuditService(
            ApplicationDbContext applicationDbContext,
            IHttpContextAccessor httpContextAccessor)
        {
            this.applicationDbContext = applicationDbContext ?? throw new ArgumentNullException(nameof(applicationDbContext));
            this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <inheritdoc/>
        public FiscalAuditEvent Record(FiscalAuditEventRequest request)
        {
            Tenant tenant = this.GetTrustedTenant();
            string actorId = this.GetTrustedActorId();
            this.ValidateRequest(request);

            FiscalAuditScope auditScope = this.ValidateRelatedFiscalEntities(request, tenant);
            var fiscalAuditEvent = new FiscalAuditEvent
            {
                TenantId = tenant.Id,
                FoodBankId = auditScope.FoodBankId,
                FiscalIssuerProfileId = auditScope.FiscalIssuerProfileId,
                FiscalDocumentSeriesId = auditScope.FiscalDocumentSeriesId,
                FiscalDocumentId = auditScope.FiscalDocumentId,
                Action = request.Action.Trim(),
                Result = request.Result.Trim(),
                ActorId = actorId,
                OccurredAtUtc = DateTimeOffset.UtcNow,
                CorrelationId = string.IsNullOrWhiteSpace(request.CorrelationId)
                    ? this.httpContextAccessor.HttpContext.TraceIdentifier
                    : request.CorrelationId.Trim(),
            };

            this.applicationDbContext.FiscalAuditEvents.Add(fiscalAuditEvent);
            this.applicationDbContext.SaveChanges();
            return fiscalAuditEvent;
        }

        private Tenant GetTrustedTenant()
        {
            Tenant tenant = this.httpContextAccessor.HttpContext?.GetTenant();
            if (tenant == null || ReferenceEquals(tenant, Tenant.EmptyTenant) || tenant.Id <= 0)
            {
                throw new InvalidOperationException("Fiscal audit recording requires a trusted current tenant.");
            }

            return tenant;
        }

        private string GetTrustedActorId()
        {
            string actorId = this.httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(actorId))
            {
                throw new InvalidOperationException("Fiscal audit recording requires an authenticated actor.");
            }

            return actorId;
        }

        private void ValidateRequest(FiscalAuditEventRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Action) ||
                string.IsNullOrWhiteSpace(request.Result) || request.Action.Trim().Length > 64 ||
                request.Result.Trim().Length > 64 ||
                (!string.IsNullOrWhiteSpace(request.CorrelationId) && request.CorrelationId.Trim().Length > 128) ||
                request.FiscalIssuerProfileId.GetValueOrDefault() < 0 ||
                request.FiscalDocumentSeriesId.GetValueOrDefault() < 0 ||
                request.FiscalDocumentId.GetValueOrDefault() < 0)
            {
                throw new InvalidOperationException("Fiscal audit event contains invalid operational identifiers.");
            }
        }

        private FiscalAuditScope ValidateRelatedFiscalEntities(FiscalAuditEventRequest request, Tenant tenant)
        {
            FiscalDocument document = request.FiscalDocumentId.HasValue
                ? this.applicationDbContext.FiscalDocuments
                    .AsNoTracking()
                    .SingleOrDefault(item => item.Id == request.FiscalDocumentId.Value)
                : null;
            if (request.FiscalDocumentId.HasValue &&
                (document == null || document.InvoicingStrategy != tenant.InvoicingStrategy))
            {
                throw new InvalidOperationException("Fiscal document does not belong to the trusted tenant scope.");
            }

            int? seriesId = request.FiscalDocumentSeriesId ?? document?.SeriesId;
            FiscalDocumentSeries series = seriesId.HasValue
                ? this.applicationDbContext.FiscalDocumentSeries
                    .AsNoTracking()
                    .SingleOrDefault(item => item.Id == seriesId.Value)
                : null;
            if (seriesId.HasValue && series == null)
            {
                throw new InvalidOperationException("Fiscal document series was not found.");
            }

            int? issuerProfileId = request.FiscalIssuerProfileId ?? document?.FiscalIssuerProfileId ?? series?.FiscalIssuerProfileId;
            FiscalIssuerProfile issuerProfile = issuerProfileId.HasValue
                ? this.applicationDbContext.FiscalIssuerProfiles
                    .AsNoTracking()
                    .SingleOrDefault(item => item.Id == issuerProfileId.Value)
                : null;
            if (issuerProfileId.HasValue && (issuerProfile == null || !this.ProfileMatchesTenant(issuerProfile, tenant)))
            {
                throw new InvalidOperationException("Fiscal issuer profile does not belong to the trusted tenant scope.");
            }

            if (seriesId.HasValue || document != null)
            {
                if (series == null || issuerProfile == null || series.FiscalIssuerProfileId != issuerProfile.Id)
                {
                    throw new InvalidOperationException("Fiscal document series does not belong to the trusted issuer profile.");
                }
            }

            if (document != null &&
                ((request.FiscalIssuerProfileId.HasValue && request.FiscalIssuerProfileId.Value != document.FiscalIssuerProfileId) ||
                (request.FiscalDocumentSeriesId.HasValue && request.FiscalDocumentSeriesId.Value != document.SeriesId)))
            {
                throw new InvalidOperationException("Fiscal audit event identifiers do not describe the same fiscal document.");
            }

            return new FiscalAuditScope
            {
                FoodBankId = document?.FoodBankId ?? issuerProfile?.FoodBankId,
                FiscalDocumentId = document?.Id,
                FiscalDocumentSeriesId = series?.Id,
                FiscalIssuerProfileId = issuerProfile?.Id,
            };
        }

        private bool ProfileMatchesTenant(FiscalIssuerProfile profile, Tenant tenant)
        {
            return (tenant.InvoicingStrategy == InvoicingStrategy.SingleInvoiceTable &&
                    profile.ScopeKind == FiscalScopeKind.Tenant && !profile.FoodBankId.HasValue) ||
                (tenant.InvoicingStrategy == InvoicingStrategy.MultipleTablesPerFoodBank &&
                    profile.ScopeKind == FiscalScopeKind.FoodBank && profile.FoodBankId.HasValue);
        }

        private class FiscalAuditScope
        {
            public int? FiscalDocumentId { get; set; }

            public int? FiscalDocumentSeriesId { get; set; }

            public int? FiscalIssuerProfileId { get; set; }

            public int? FoodBankId { get; set; }
        }
    }
}
