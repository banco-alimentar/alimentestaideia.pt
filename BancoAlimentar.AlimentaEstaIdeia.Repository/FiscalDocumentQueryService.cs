// -----------------------------------------------------------------------
// <copyright file="FiscalDocumentQueryService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Queries fiscal documents after resolving the tenant and validating the issuer scope.
    /// </summary>
    public class FiscalDocumentQueryService : IFiscalDocumentQueryService
    {
        private readonly ApplicationDbContext applicationDbContext;
        private readonly IHttpContextAccessor httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="FiscalDocumentQueryService"/> class.
        /// </summary>
        /// <param name="applicationDbContext">The current tenant fiscal persistence context.</param>
        /// <param name="httpContextAccessor">The middleware-resolved tenant context.</param>
        public FiscalDocumentQueryService(
            ApplicationDbContext applicationDbContext,
            IHttpContextAccessor httpContextAccessor)
        {
            this.applicationDbContext = applicationDbContext ?? throw new ArgumentNullException(nameof(applicationDbContext));
            this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <inheritdoc/>
        public IReadOnlyList<FiscalDocumentQueryResult> Query(FiscalDocumentQueryRequest request)
        {
            Tenant tenant = this.GetTrustedTenant();
            this.ValidateRequest(request, tenant);

            IQueryable<FiscalDocument> documents = this.applicationDbContext.FiscalDocuments
                .AsNoTracking()
                .Where(document =>
                    document.FiscalIssuerProfileId == request.FiscalIssuerProfileId &&
                    document.InvoicingStrategy == tenant.InvoicingStrategy);

            if (request.FiscalDocumentSeriesId.HasValue)
            {
                documents = documents.Where(document => document.SeriesId == request.FiscalDocumentSeriesId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.DocumentNumber))
            {
                string documentNumber = request.DocumentNumber.Trim();
                documents = documents.Where(document => document.DocumentNumber == documentNumber);
            }

            if (!string.IsNullOrWhiteSpace(request.Atcud))
            {
                string atcud = request.Atcud.Trim();
                documents = documents.Where(document => document.Atcud == atcud);
            }

            if (request.DonationId.HasValue)
            {
                documents = documents.Where(document => document.DonationId == request.DonationId.Value);
            }

            if (request.StartDate.HasValue)
            {
                documents = documents.Where(document => document.InvoiceDate >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                documents = documents.Where(document => document.InvoiceDate <= request.EndDate.Value);
            }

            if (request.Status.HasValue)
            {
                documents = documents.Where(document => document.Status == request.Status.Value);
            }

            return documents
                .OrderByDescending(document => document.InvoiceDate)
                .ThenByDescending(document => document.Id)
                .Select(document => new FiscalDocumentQueryResult
                {
                    FiscalDocumentId = document.Id,
                    FiscalIssuerProfileId = document.FiscalIssuerProfileId,
                    FiscalDocumentSeriesId = document.SeriesId,
                    DocumentNumber = document.DocumentNumber,
                    Atcud = document.Atcud,
                    DonationId = document.DonationId,
                    InvoiceDate = document.InvoiceDate,
                    Status = document.Status,
                })
                .ToList();
        }

        private Tenant GetTrustedTenant()
        {
            Tenant tenant = this.httpContextAccessor.HttpContext?.GetTenant();
            if (tenant == null || ReferenceEquals(tenant, Tenant.EmptyTenant) || tenant.Id <= 0)
            {
                throw new InvalidOperationException("Fiscal document consultation requires a trusted current tenant.");
            }

            return tenant;
        }

        private void ValidateRequest(FiscalDocumentQueryRequest request, Tenant tenant)
        {
            if (request == null || request.FiscalIssuerProfileId <= 0 ||
                request.FiscalDocumentSeriesId.GetValueOrDefault() < 0 ||
                request.DonationId.GetValueOrDefault() < 0 ||
                (request.StartDate.HasValue && request.EndDate.HasValue && request.EndDate < request.StartDate))
            {
                throw new InvalidOperationException("Fiscal document consultation filters are invalid.");
            }

            FiscalIssuerProfile issuerProfile = this.applicationDbContext.FiscalIssuerProfiles
                .AsNoTracking()
                .SingleOrDefault(profile => profile.Id == request.FiscalIssuerProfileId);
            if (issuerProfile == null || !this.ProfileMatchesTenant(issuerProfile, tenant))
            {
                throw new InvalidOperationException("Fiscal issuer profile does not belong to the trusted tenant scope.");
            }

            if (request.FiscalDocumentSeriesId.HasValue && !this.applicationDbContext.FiscalDocumentSeries
                .AsNoTracking()
                .Any(series =>
                    series.Id == request.FiscalDocumentSeriesId.Value &&
                    series.FiscalIssuerProfileId == issuerProfile.Id))
            {
                throw new InvalidOperationException("Fiscal document series does not belong to the selected issuer profile.");
            }
        }

        private bool ProfileMatchesTenant(FiscalIssuerProfile profile, Tenant tenant)
        {
            return (tenant.InvoicingStrategy == InvoicingStrategy.SingleInvoiceTable &&
                    profile.ScopeKind == FiscalScopeKind.Tenant && !profile.FoodBankId.HasValue) ||
                (tenant.InvoicingStrategy == InvoicingStrategy.MultipleTablesPerFoodBank &&
                    profile.ScopeKind == FiscalScopeKind.FoodBank && profile.FoodBankId.HasValue);
        }
    }
}
