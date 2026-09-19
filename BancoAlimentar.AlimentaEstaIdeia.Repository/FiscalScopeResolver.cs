// -----------------------------------------------------------------------
// <copyright file="FiscalScopeResolver.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Resolves the configured fiscal issuer and series for a donation without reserving a sequence.
    /// </summary>
    public class FiscalScopeResolver : IFiscalScopeResolver
    {
        private readonly ApplicationDbContext applicationDbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="FiscalScopeResolver"/> class.
        /// </summary>
        /// <param name="applicationDbContext">The current tenant application database.</param>
        public FiscalScopeResolver(ApplicationDbContext applicationDbContext)
        {
            this.applicationDbContext = applicationDbContext ?? throw new ArgumentNullException(nameof(applicationDbContext));
        }

        /// <inheritdoc />
        public FiscalScope Resolve(Tenant tenant, Donation donation)
        {
            if (tenant == null)
            {
                throw new ArgumentNullException(nameof(tenant));
            }

            if (donation == null)
            {
                throw new ArgumentNullException(nameof(donation));
            }

            DateTimeOffset nowUtc = DateTimeOffset.UtcNow;
            int? foodBankId = this.ResolveFoodBankId(tenant.InvoicingStrategy, donation);
            FiscalIssuerProfile issuerProfile = this.ResolveIssuerProfile(
                tenant.InvoicingStrategy,
                foodBankId,
                nowUtc);
            FiscalDocumentSeries fiscalDocumentSeries = this.ResolveFiscalDocumentSeries(issuerProfile, nowUtc);

            return new FiscalScope
            {
                TenantId = tenant.Id,
                InvoicingStrategy = tenant.InvoicingStrategy,
                FoodBankId = foodBankId,
                FiscalIssuerProfile = issuerProfile,
                FiscalDocumentSeries = fiscalDocumentSeries,
            };
        }

        private int? ResolveFoodBankId(InvoicingStrategy invoicingStrategy, Donation donation)
        {
            if (invoicingStrategy == InvoicingStrategy.SingleInvoiceTable)
            {
                return null;
            }

            if (invoicingStrategy != InvoicingStrategy.MultipleTablesPerFoodBank)
            {
                throw new InvalidOperationException("The tenant invoicing strategy is not supported for fiscal issuance.");
            }

            if (donation.FoodBank == null || donation.FoodBank.Id <= 0)
            {
                throw new InvalidOperationException("A FoodBank is required when the tenant uses MultipleTablesPerFoodBank.");
            }

            return donation.FoodBank.Id;
        }

        private FiscalIssuerProfile ResolveIssuerProfile(
            InvoicingStrategy invoicingStrategy,
            int? foodBankId,
            DateTimeOffset nowUtc)
        {
            List<FiscalIssuerProfile> eligibleProfiles = this.applicationDbContext.FiscalIssuerProfiles
                .AsNoTracking()
                .Where(profile => profile.IsActive &&
                    profile.ValidFromUtc <= nowUtc &&
                    (!profile.ValidToUtc.HasValue || profile.ValidToUtc.Value > nowUtc))
                .ToList()
                .Where(profile => this.IsEligibleProfile(profile, invoicingStrategy, foodBankId))
                .ToList();

            if (eligibleProfiles.Count != 1)
            {
                throw new InvalidOperationException(
                    "Fiscal issuance requires exactly one active, in-date issuer profile for the resolved scope.");
            }

            return eligibleProfiles.Single();
        }

        private FiscalDocumentSeries ResolveFiscalDocumentSeries(
            FiscalIssuerProfile issuerProfile,
            DateTimeOffset nowUtc)
        {
            List<FiscalDocumentSeries> eligibleSeries = this.applicationDbContext.FiscalDocumentSeries
                .AsNoTracking()
                .Where(series => series.FiscalIssuerProfileId == issuerProfile.Id &&
                    series.Status == FiscalSeriesStatus.Active &&
                    series.StartsAtUtc <= nowUtc &&
                    !series.ClosedAtUtc.HasValue)
                .ToList()
                .Where(series => this.IsEligibleSeries(series, issuerProfile))
                .ToList();

            if (eligibleSeries.Count != 1)
            {
                throw new InvalidOperationException(
                    "Fiscal issuance requires exactly one active series for the resolved issuer profile.");
            }

            return eligibleSeries.Single();
        }

        private bool IsEligibleProfile(
            FiscalIssuerProfile issuerProfile,
            InvoicingStrategy invoicingStrategy,
            int? foodBankId)
        {
            bool hasRequiredConfiguration =
                !string.IsNullOrWhiteSpace(issuerProfile.LegalName) &&
                !string.IsNullOrWhiteSpace(issuerProfile.LegalAddress) &&
                !string.IsNullOrWhiteSpace(issuerProfile.TaxRegistrationNumber) &&
                !string.IsNullOrWhiteSpace(issuerProfile.SaftDocumentType) &&
                !string.IsNullOrWhiteSpace(issuerProfile.TaxRule) &&
                !string.IsNullOrWhiteSpace(issuerProfile.SoftwareCertificateNumber) &&
                !string.IsNullOrWhiteSpace(issuerProfile.SigningKeyIdentifier) &&
                !string.IsNullOrWhiteSpace(issuerProfile.SigningKeyVersion);

            return hasRequiredConfiguration &&
                ((invoicingStrategy == InvoicingStrategy.SingleInvoiceTable &&
                    issuerProfile.ScopeKind == FiscalScopeKind.Tenant &&
                    !issuerProfile.FoodBankId.HasValue) ||
                (invoicingStrategy == InvoicingStrategy.MultipleTablesPerFoodBank &&
                    issuerProfile.ScopeKind == FiscalScopeKind.FoodBank &&
                    issuerProfile.FoodBankId == foodBankId));
        }

        private bool IsEligibleSeries(FiscalDocumentSeries fiscalDocumentSeries, FiscalIssuerProfile issuerProfile)
        {
            return !string.IsNullOrWhiteSpace(fiscalDocumentSeries.SeriesCode) &&
                !string.IsNullOrWhiteSpace(fiscalDocumentSeries.SaftDocumentType) &&
                !string.IsNullOrWhiteSpace(fiscalDocumentSeries.AtcudValidationCode) &&
                fiscalDocumentSeries.SaftDocumentType == issuerProfile.SaftDocumentType &&
                fiscalDocumentSeries.FirstSequence > 0 &&
                fiscalDocumentSeries.NextSequence >= fiscalDocumentSeries.FirstSequence &&
                (!fiscalDocumentSeries.LastSequence.HasValue ||
                    fiscalDocumentSeries.NextSequence <= fiscalDocumentSeries.LastSequence.Value);
        }
    }
}
