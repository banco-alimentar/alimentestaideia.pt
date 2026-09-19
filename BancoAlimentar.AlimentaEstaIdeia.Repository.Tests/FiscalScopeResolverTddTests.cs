// -----------------------------------------------------------------------
// <copyright file="FiscalScopeResolverTddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Reflection;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using Microsoft.EntityFrameworkCore;
    using Xunit;

    /// <summary>
    /// Defines TDD coverage for resolving an active fiscal issuer and series.
    /// </summary>
    public class FiscalScopeResolverTddTests
    {
        /// <summary>
        /// A tenant-wide strategy resolves only its active tenant-scoped issuer and series.
        /// </summary>
        [Fact]
        public void Resolve_SingleInvoiceTableSelectsOnlyActiveTenantScopedIssuerAndSeries()
        {
            using var context = CreateContext();
            FiscalIssuerProfile tenantProfile = PersistProfile(context, FiscalScopeKind.Tenant);
            FiscalDocumentSeries tenantSeries = PersistSeries(context, tenantProfile, 42);
            FiscalIssuerProfile foodBankProfile = PersistProfile(context, FiscalScopeKind.FoodBank, 20);
            PersistSeries(context, foodBankProfile, 43);
            Donation donation = PersistDonation(context, foodBankId: 20);

            FiscalScope scope = CreateResolver(context).Resolve(
                CreateTenant(InvoicingStrategy.SingleInvoiceTable),
                donation);

            Assert.Equal(tenantProfile.Id, scope.FiscalIssuerProfile.Id);
            Assert.Equal(tenantSeries.Id, scope.FiscalDocumentSeries.Id);
            Assert.Null(scope.FoodBankId);
        }

        /// <summary>
        /// A FoodBank strategy requires and resolves the profile and series belonging to the
        /// donation's FoodBank, never the tenant-wide or another FoodBank profile.
        /// </summary>
        [Fact]
        public void Resolve_MultipleTablesPerFoodBankSelectsOnlyDonationFoodBankIssuerAndSeries()
        {
            using var context = CreateContext();
            FiscalIssuerProfile selectedProfile = PersistProfile(context, FiscalScopeKind.FoodBank, 20);
            FiscalDocumentSeries selectedSeries = PersistSeries(context, selectedProfile, 42);
            FiscalIssuerProfile otherFoodBankProfile = PersistProfile(context, FiscalScopeKind.FoodBank, 21);
            PersistSeries(context, otherFoodBankProfile, 43);
            FiscalIssuerProfile tenantProfile = PersistProfile(context, FiscalScopeKind.Tenant);
            PersistSeries(context, tenantProfile, 44);
            Donation donation = PersistDonation(context, foodBankId: 20);

            FiscalScope scope = CreateResolver(context).Resolve(
                CreateTenant(InvoicingStrategy.MultipleTablesPerFoodBank),
                donation);

            Assert.Equal(selectedProfile.Id, scope.FiscalIssuerProfile.Id);
            Assert.Equal(selectedSeries.Id, scope.FiscalDocumentSeries.Id);
            Assert.Equal(20, scope.FoodBankId);
        }

        /// <summary>
        /// A FoodBank strategy fails before selecting a series when the donation has no
        /// FoodBank boundary.
        /// </summary>
        [Fact]
        public void Resolve_MultipleTablesPerFoodBankRejectsDonationWithoutFoodBank()
        {
            using var context = CreateContext();
            FiscalIssuerProfile profile = PersistProfile(context, FiscalScopeKind.FoodBank, 20);
            FiscalDocumentSeries series = PersistSeries(context, profile, 42);
            IFiscalScopeResolver resolver = CreateResolver(context);

            Assert.ThrowsAny<Exception>(() => resolver.Resolve(
                CreateTenant(InvoicingStrategy.MultipleTablesPerFoodBank),
                new Donation()));
            Assert.Equal(42, series.NextSequence);
        }

        /// <summary>
        /// No active profile means fiscal scope resolution fails before the series counter is
        /// changed.
        /// </summary>
        [Fact]
        public void Resolve_RejectsAbsentIssuerProfileBeforeSequenceAllocation()
        {
            using var context = CreateContext();
            IFiscalScopeResolver resolver = CreateResolver(context);

            Assert.ThrowsAny<Exception>(() => resolver.Resolve(
                CreateTenant(InvoicingStrategy.SingleInvoiceTable),
                CreateDonation()));
        }

        /// <summary>
        /// Inactive and expired issuer profiles cannot be selected for a tenant-wide scope.
        /// </summary>
        /// <param name="isActive">Whether the profile is marked active.</param>
        /// <param name="validToUtc">The optional end of the profile validity period.</param>
        [Theory]
        [InlineData(false, null)]
        [InlineData(true, "2000-01-01T00:00:00+00:00")]
        public void Resolve_RejectsInactiveOrExpiredIssuerProfileBeforeSequenceAllocation(
            bool isActive,
            string validToUtc)
        {
            using var context = CreateContext();
            FiscalIssuerProfile profile = PersistProfile(
                context,
                FiscalScopeKind.Tenant,
                isActive: isActive,
                validToUtc: string.IsNullOrEmpty(validToUtc) ? null : DateTimeOffset.Parse(validToUtc));
            FiscalDocumentSeries series = PersistSeries(context, profile, 42);
            IFiscalScopeResolver resolver = CreateResolver(context);

            Assert.ThrowsAny<Exception>(() => resolver.Resolve(
                CreateTenant(InvoicingStrategy.SingleInvoiceTable),
                CreateDonation()));
            Assert.Equal(42, series.NextSequence);
        }

        /// <summary>
        /// More than one eligible profile is ambiguous and must not allocate a sequence.
        /// </summary>
        [Fact]
        public void Resolve_RejectsAmbiguousIssuerProfilesBeforeSequenceAllocation()
        {
            using var context = CreateContext();
            FiscalIssuerProfile firstProfile = PersistProfile(context, FiscalScopeKind.Tenant);
            FiscalDocumentSeries firstSeries = PersistSeries(context, firstProfile, 42);
            FiscalIssuerProfile secondProfile = PersistProfile(context, FiscalScopeKind.Tenant);
            FiscalDocumentSeries secondSeries = PersistSeries(context, secondProfile, 43);
            IFiscalScopeResolver resolver = CreateResolver(context);

            Assert.ThrowsAny<Exception>(() => resolver.Resolve(
                CreateTenant(InvoicingStrategy.SingleInvoiceTable),
                CreateDonation()));
            Assert.Equal(42, firstSeries.NextSequence);
            Assert.Equal(43, secondSeries.NextSequence);
        }

        /// <summary>
        /// A valid profile without an active series cannot form a fiscal scope.
        /// </summary>
        [Fact]
        public void Resolve_RejectsAbsentSeriesBeforeSequenceAllocation()
        {
            using var context = CreateContext();
            PersistProfile(context, FiscalScopeKind.Tenant);
            IFiscalScopeResolver resolver = CreateResolver(context);

            Assert.ThrowsAny<Exception>(() => resolver.Resolve(
                CreateTenant(InvoicingStrategy.SingleInvoiceTable),
                CreateDonation()));
        }

        /// <summary>
        /// Draft and closed series cannot be selected or advanced.
        /// </summary>
        /// <param name="status">The unavailable series lifecycle state.</param>
        [Theory]
        [InlineData(FiscalSeriesStatus.Draft)]
        [InlineData(FiscalSeriesStatus.Closed)]
        public void Resolve_RejectsInactiveOrClosedSeriesBeforeSequenceAllocation(FiscalSeriesStatus status)
        {
            using var context = CreateContext();
            FiscalIssuerProfile profile = PersistProfile(context, FiscalScopeKind.Tenant);
            FiscalDocumentSeries series = PersistSeries(context, profile, 42, status);
            IFiscalScopeResolver resolver = CreateResolver(context);

            Assert.ThrowsAny<Exception>(() => resolver.Resolve(
                CreateTenant(InvoicingStrategy.SingleInvoiceTable),
                CreateDonation()));
            Assert.Equal(42, series.NextSequence);
        }

        /// <summary>
        /// More than one active series for an issuer is ambiguous and must fail before a
        /// sequence is reserved.
        /// </summary>
        [Fact]
        public void Resolve_RejectsAmbiguousSeriesBeforeSequenceAllocation()
        {
            using var context = CreateContext();
            FiscalIssuerProfile profile = PersistProfile(context, FiscalScopeKind.Tenant);
            FiscalDocumentSeries firstSeries = PersistSeries(context, profile, 42);
            FiscalDocumentSeries secondSeries = PersistSeries(context, profile, 43);
            IFiscalScopeResolver resolver = CreateResolver(context);

            Assert.ThrowsAny<Exception>(() => resolver.Resolve(
                CreateTenant(InvoicingStrategy.SingleInvoiceTable),
                CreateDonation()));
            Assert.Equal(42, firstSeries.NextSequence);
            Assert.Equal(43, secondSeries.NextSequence);
        }

        /// <summary>
        /// A series cannot be used before the AT communication evidence is recorded.
        /// </summary>
        [Fact]
        public void Resolve_RejectsSeriesWithoutAtCommunicationReferenceBeforeSequenceAllocation()
        {
            using var context = CreateContext();
            FiscalIssuerProfile profile = PersistProfile(context, FiscalScopeKind.Tenant);
            FiscalDocumentSeries series = PersistSeries(context, profile, 42, atCommunicationReference: null);
            IFiscalScopeResolver resolver = CreateResolver(context);

            Assert.ThrowsAny<Exception>(() => resolver.Resolve(
                CreateTenant(InvoicingStrategy.SingleInvoiceTable),
                CreateDonation()));
            Assert.Equal(42, series.NextSequence);
        }

        /// <summary>
        /// A fiscal series must use the certified software processing method.
        /// </summary>
        /// <param name="processingMethod">The non-certified processing method.</param>
        [Theory]
        [InlineData(FiscalProcessingMethod.TaxAuthorityPortal)]
        [InlineData(FiscalProcessingMethod.Manual)]
        public void Resolve_RejectsSeriesWithNonCertifiedProcessingMethodBeforeSequenceAllocation(
            FiscalProcessingMethod processingMethod)
        {
            using var context = CreateContext();
            FiscalIssuerProfile profile = PersistProfile(context, FiscalScopeKind.Tenant);
            FiscalDocumentSeries series = PersistSeries(context, profile, 42, processingMethod: processingMethod);
            IFiscalScopeResolver resolver = CreateResolver(context);

            Assert.ThrowsAny<Exception>(() => resolver.Resolve(
                CreateTenant(InvoicingStrategy.SingleInvoiceTable),
                CreateDonation()));
            Assert.Equal(42, series.NextSequence);
        }

        /// <summary>
        /// The resolved FoodBank scope comes from the persisted donation, not an untrusted
        /// navigation property supplied by a caller after the donation was loaded.
        /// </summary>
        [Fact]
        public void Resolve_UsesPersistedDonationFoodBankInsteadOfCallerMutatedNavigation()
        {
            using var context = CreateContext();
            Donation persistedDonation = PersistDonation(context, foodBankId: 20);
            FiscalIssuerProfile persistedFoodBankProfile = PersistProfile(context, FiscalScopeKind.FoodBank, 20);
            FiscalDocumentSeries persistedFoodBankSeries = PersistSeries(context, persistedFoodBankProfile, 42);
            FiscalIssuerProfile mutatedFoodBankProfile = PersistProfile(context, FiscalScopeKind.FoodBank, 21);
            PersistSeries(context, mutatedFoodBankProfile, 43);
            var callerDonation = new Donation
            {
                Id = persistedDonation.Id,
                FoodBank = new FoodBank { Id = 21, Name = "Banco Alimentar adulterado" },
            };

            FiscalScope scope = CreateResolver(context).Resolve(
                CreateTenant(InvoicingStrategy.MultipleTablesPerFoodBank),
                callerDonation);

            Assert.Equal(20, scope.FoodBankId);
            Assert.Equal(persistedFoodBankProfile.Id, scope.FiscalIssuerProfile.Id);
            Assert.Equal(persistedFoodBankSeries.Id, scope.FiscalDocumentSeries.Id);
        }

        /// <summary>
        /// An absent or unknown donation cannot be used to resolve a tenant-wide fiscal
        /// scope, even when a tenant-scoped profile and series are otherwise valid.
        /// </summary>
        /// <param name="donationId">The absent or unknown donation identifier.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(999999)]
        public void Resolve_RejectsAbsentOrUnknownPersistedDonationBeforeSequenceAllocation(int donationId)
        {
            using var context = CreateContext();
            FiscalIssuerProfile profile = PersistProfile(context, FiscalScopeKind.Tenant);
            FiscalDocumentSeries series = PersistSeries(context, profile, 42);
            IFiscalScopeResolver resolver = CreateResolver(context);

            Assert.ThrowsAny<Exception>(() => resolver.Resolve(
                CreateTenant(InvoicingStrategy.SingleInvoiceTable),
                new Donation { Id = donationId }));
            Assert.Equal(42, series.NextSequence);
        }

        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private static IFiscalScopeResolver CreateResolver(ApplicationDbContext context)
        {
            Type resolverType = typeof(IFiscalScopeResolver).Assembly.GetType(
                "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalScopeResolver");
            Assert.NotNull(resolverType);
            ConstructorInfo constructor = resolverType.GetConstructor(new[] { typeof(ApplicationDbContext) });
            Assert.NotNull(constructor);
            return Assert.IsAssignableFrom<IFiscalScopeResolver>(constructor.Invoke(new object[] { context }));
        }

        private static Tenant CreateTenant(InvoicingStrategy strategy)
        {
            return new Tenant
            {
                Id = 1,
                Name = "tenant-test",
                InvoicingStrategy = strategy,
            };
        }

        private static Donation CreateDonation(int? foodBankId = null)
        {
            return new Donation
            {
                FoodBank = foodBankId.HasValue
                    ? new FoodBank { Id = foodBankId.Value, Name = "Banco Alimentar de teste" }
                    : null,
            };
        }

        private static FiscalIssuerProfile PersistProfile(
            ApplicationDbContext context,
            FiscalScopeKind scopeKind,
            int? foodBankId = null,
            bool isActive = true,
            DateTimeOffset? validToUtc = null)
        {
            var profile = new FiscalIssuerProfile
            {
                ScopeKind = scopeKind,
                FoodBankId = foodBankId,
                LegalName = $"Emitente de teste {Guid.NewGuid()}",
                LegalAddress = "Rua de teste, 1, Lisboa",
                TaxRegistrationNumber = "501234567",
                SaftDocumentType = "RC",
                TaxRule = "M00",
                SoftwareCertificateNumber = "0",
                SigningKeyIdentifier = "test-key",
                SigningKeyVersion = "v1",
                IsActive = isActive,
                ValidFromUtc = DateTimeOffset.UtcNow.AddHours(-1),
                ValidToUtc = validToUtc,
            };
            context.FiscalIssuerProfiles.Add(profile);
            context.SaveChanges();
            return profile;
        }

        private static FiscalDocumentSeries PersistSeries(
            ApplicationDbContext context,
            FiscalIssuerProfile profile,
            int nextSequence,
            FiscalSeriesStatus status = FiscalSeriesStatus.Active,
            FiscalProcessingMethod processingMethod = FiscalProcessingMethod.CertifiedSoftware,
            string atCommunicationReference = "at-communication-test")
        {
            var series = new FiscalDocumentSeries
            {
                FiscalIssuerProfileId = profile.Id,
                SeriesCode = $"RC-{Guid.NewGuid():N}",
                SaftDocumentType = "RC",
                ProcessingMethod = processingMethod,
                FirstSequence = 1,
                NextSequence = nextSequence,
                AtcudValidationCode = $"AA{Guid.NewGuid():N}",
                Status = status,
                StartsAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
                ClosedAtUtc = status == FiscalSeriesStatus.Closed ? DateTimeOffset.UtcNow.AddMinutes(-1) : null,
                AtCommunicationReference = atCommunicationReference,
            };
            context.FiscalDocumentSeries.Add(series);
            context.SaveChanges();
            return series;
        }

        private static Donation PersistDonation(ApplicationDbContext context, int foodBankId)
        {
            var foodBank = new FoodBank
            {
                Id = foodBankId,
                Name = "Banco Alimentar persistido",
            };
            var donation = new Donation
            {
                DonationDate = DateTime.UtcNow,
                FoodBank = foodBank,
            };
            context.Donations.Add(donation);
            context.SaveChanges();
            return donation;
        }
    }
}
