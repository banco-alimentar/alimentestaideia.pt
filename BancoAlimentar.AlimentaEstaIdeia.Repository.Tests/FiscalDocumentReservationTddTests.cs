// -----------------------------------------------------------------------
// <copyright file="FiscalDocumentReservationTddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.ExceptionServices;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata;
    using Xunit;

    /// <summary>
    /// Defines TDD contracts for reserving an original fiscal document sequence.
    /// </summary>
    public class FiscalDocumentReservationTddTests
    {
        private const string ReservationRequestTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalDocumentReservationRequest";
        private const string ReservationServiceInterfaceTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.IFiscalDocumentReservationService";
        private const string ReservationServiceTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalDocumentReservationService";

        /// <summary>
        /// The reservation API takes a donation, series and durable idempotency key, and
        /// returns the reserved fiscal document without involving PDF, QR or signing flows.
        /// </summary>
        [Fact]
        public void ReservationService_ExposesOriginalReservationContract()
        {
            Type requestType = GetRequiredRepositoryType(ReservationRequestTypeName);
            Type serviceInterfaceType = GetRequiredRepositoryType(ReservationServiceInterfaceTypeName);

            Assert.NotNull(requestType.GetProperty("DonationId"));
            Assert.NotNull(requestType.GetProperty("SeriesId"));
            Assert.NotNull(requestType.GetProperty("IdempotencyKey"));
            MethodInfo reserveOriginal = Assert.Single(
                serviceInterfaceType.GetMethods(),
                method => method.Name == "ReserveOriginal" &&
                    method.GetParameters().Select(parameter => parameter.ParameterType)
                        .SequenceEqual(new[] { requestType }));
            Assert.Equal(typeof(FiscalDocument), reserveOriginal.ReturnType);
        }

        /// <summary>
        /// Idempotency is a persisted fiscal-document invariant and is protected by a unique
        /// index in the tenant application database.
        /// </summary>
        [Fact]
        public void FiscalDocument_ModelPersistsUniqueIdempotencyKey()
        {
            using var context = CreateContext();
            IEntityType fiscalDocument = Assert.IsAssignableFrom<IEntityType>(
                context.Model.FindEntityType(typeof(FiscalDocument)));

            Assert.NotNull(fiscalDocument.FindProperty("IdempotencyKey"));
            Assert.Contains(
                fiscalDocument.GetIndexes(),
                index => index.IsUnique &&
                    index.Properties.Select(property => property.Name)
                        .SequenceEqual(new[] { "IdempotencyKey" }));
        }

        /// <summary>
        /// Reserving an active series returns its current sequence, advances it exactly once,
        /// and forms the ATCUD from the series validation code and reserved sequence.
        /// </summary>
        [Fact]
        public void ReserveOriginal_ReservesNextSequenceAndFormsAtcud()
        {
            using var context = CreateContext();
            Donation donation = PersistDonation(context);
            FiscalDocumentSeries series = PersistSeries(context, nextSequence: 42);

            FiscalDocument reserved = ReserveOriginal(
                context,
                donation.Id,
                series.Id,
                "reserve-next-sequence");

            Assert.Equal(42, reserved.Sequence);
            Assert.Equal($"{series.AtcudValidationCode}-42", reserved.Atcud);
            Assert.Equal(series.Id, reserved.SeriesId);
            Assert.Equal(donation.Id, reserved.DonationId);
            Assert.Equal(43, context.FiscalDocumentSeries.Single(item => item.Id == series.Id).NextSequence);
        }

        /// <summary>
        /// Repeating an original reservation with the same donation and idempotency key must
        /// return the first document without advancing the series a second time.
        /// </summary>
        [Fact]
        public void ReserveOriginal_RepeatsIdempotencyKeyWithoutConsumingAnotherSequence()
        {
            using var context = CreateContext();
            Donation donation = PersistDonation(context);
            FiscalDocumentSeries series = PersistSeries(context, nextSequence: 42);

            FiscalDocument first = ReserveOriginal(context, donation.Id, series.Id, "repeat-key");
            FiscalDocument repeated = ReserveOriginal(context, donation.Id, series.Id, "repeat-key");

            Assert.Equal(first.Id, repeated.Id);
            Assert.Equal(first.Sequence, repeated.Sequence);
            Assert.Equal(first.Atcud, repeated.Atcud);
            Assert.Equal(1, context.FiscalDocuments.Count(item => item.DonationId == donation.Id));
            Assert.Equal(43, context.FiscalDocumentSeries.Single(item => item.Id == series.Id).NextSequence);
        }

        /// <summary>
        /// An idempotency key belongs to one donation and cannot be replayed for another
        /// donation merely because both requests target an otherwise eligible series.
        /// </summary>
        [Fact]
        public void ReserveOriginal_RejectsIdempotencyKeyReusedForDifferentDonation()
        {
            using var context = CreateContext();
            Donation firstDonation = PersistDonation(context);
            Donation secondDonation = PersistDonation(context);
            FiscalDocumentSeries series = PersistSeries(context, nextSequence: 42);

            ReserveOriginal(context, firstDonation.Id, series.Id, "donation-bound-key");

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => ReserveOriginal(context, secondDonation.Id, series.Id, "donation-bound-key"));

            Assert.Contains("idempotency", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(1, context.FiscalDocuments.Count());
            Assert.Equal(43, context.FiscalDocumentSeries.Single(item => item.Id == series.Id).NextSequence);
        }

        /// <summary>
        /// An idempotency key belongs to one series and cannot return a document from a
        /// different series when a caller accidentally or maliciously changes the series id.
        /// </summary>
        [Fact]
        public void ReserveOriginal_RejectsIdempotencyKeyReusedForDifferentSeries()
        {
            using var context = CreateContext();
            Donation donation = PersistDonation(context);
            FiscalDocumentSeries firstSeries = PersistSeries(context, nextSequence: 42);
            FiscalDocumentSeries secondSeries = PersistSeries(context, nextSequence: 100);

            ReserveOriginal(context, donation.Id, firstSeries.Id, "series-bound-key");

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => ReserveOriginal(context, donation.Id, secondSeries.Id, "series-bound-key"));

            Assert.Contains("idempotency", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(1, context.FiscalDocuments.Count());
            Assert.Equal(43, context.FiscalDocumentSeries.Single(item => item.Id == firstSeries.Id).NextSequence);
            Assert.Equal(100, context.FiscalDocumentSeries.Single(item => item.Id == secondSeries.Id).NextSequence);
        }

        /// <summary>
        /// Reservation receives the fiscal scope previously resolved in the trusted tenant
        /// boundary, rather than relying only on caller-controlled donation and series ids.
        /// </summary>
        [Fact]
        public void ReservationRequest_ExposesTrustedFiscalScope()
        {
            Type requestType = GetRequiredRepositoryType(ReservationRequestTypeName);
            PropertyInfo fiscalScope = requestType.GetProperty("FiscalScope");

            Assert.NotNull(fiscalScope);
            Assert.Equal(typeof(FiscalScope), fiscalScope.PropertyType);
            Assert.NotNull(typeof(FiscalScope).GetProperty("TenantId"));
        }

        /// <summary>
        /// A request scope selected for one issuer and series cannot be used to reserve a
        /// sequence from another issuer or series in that tenant boundary.
        /// </summary>
        [Fact]
        public void ReserveOriginal_RejectsSeriesOutsideTrustedFiscalScope()
        {
            using var context = CreateContext();
            Donation donation = PersistDonation(context);
            FiscalDocumentSeries scopedSeries = PersistSeries(context, nextSequence: 42);
            FiscalDocumentSeries unrelatedSeries = PersistSeries(context, nextSequence: 100);
            FiscalIssuerProfile scopedProfile = context.FiscalIssuerProfiles
                .Single(item => item.Id == scopedSeries.FiscalIssuerProfileId);
            var fiscalScope = new FiscalScope
            {
                TenantId = 1,
                InvoicingStrategy = BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy.InvoicingStrategy.SingleInvoiceTable,
                FiscalIssuerProfile = scopedProfile,
                FiscalDocumentSeries = scopedSeries,
            };
            object reservationService = CreateReservationService(context);
            object request = CreateScopedReservationRequest(
                donation.Id,
                unrelatedSeries.Id,
                "scope-bound-key",
                fiscalScope);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => InvokeReserveOriginal(reservationService, request));

            Assert.Contains("scope", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(context.FiscalDocuments);
            Assert.Equal(42, context.FiscalDocumentSeries.Single(item => item.Id == scopedSeries.Id).NextSequence);
            Assert.Equal(100, context.FiscalDocumentSeries.Single(item => item.Id == unrelatedSeries.Id).NextSequence);
        }

        /// <summary>
        /// A caller cannot construct a detached scope with the identifiers of a real profile
        /// and series while substituting its fiscal configuration. The reservation must
        /// validate the supplied scope against the persisted issuer and series configuration.
        /// </summary>
        [Fact]
        public void ReserveOriginal_RejectsCallerForgedFiscalScopeConfiguration()
        {
            using var context = CreateContext();
            Donation donation = PersistDonation(context);
            FiscalDocumentSeries persistedSeries = PersistSeries(context, nextSequence: 42);
            FiscalIssuerProfile persistedProfile = context.FiscalIssuerProfiles
                .Single(item => item.Id == persistedSeries.FiscalIssuerProfileId);
            var forgedScope = new FiscalScope
            {
                TenantId = 1,
                InvoicingStrategy = BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy.InvoicingStrategy.SingleInvoiceTable,
                FiscalIssuerProfile = new FiscalIssuerProfile
                {
                    Id = persistedProfile.Id,
                    ScopeKind = FiscalScopeKind.FoodBank,
                    FoodBankId = 999,
                    IsActive = false,
                    LegalName = "Emitente forjado",
                    TaxRegistrationNumber = "999999990",
                },
                FiscalDocumentSeries = new FiscalDocumentSeries
                {
                    Id = persistedSeries.Id,
                    FiscalIssuerProfileId = persistedProfile.Id,
                    SeriesCode = "SERIE-FORJADA",
                    SaftDocumentType = "FT",
                    AtcudValidationCode = "FORJADO",
                    ProcessingMethod = FiscalProcessingMethod.Manual,
                    FirstSequence = 1,
                    NextSequence = 1,
                },
            };
            object reservationService = CreateReservationService(context);
            object request = CreateScopedReservationRequest(
                donation.Id,
                persistedSeries.Id,
                "forged-scope-key",
                forgedScope);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => InvokeReserveOriginal(reservationService, request));

            Assert.Contains("scope", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(context.FiscalDocuments);
            Assert.Equal(42, context.FiscalDocumentSeries.Single(item => item.Id == persistedSeries.Id).NextSequence);
        }

        /// <summary>
        /// Copying an otherwise valid resolved scope into a different tenant is a boundary
        /// violation. The reservation must reject the tampered tenant identity before it
        /// consumes a number from the tenant's fiscal series.
        /// </summary>
        [Fact]
        public void ReserveOriginal_RejectsClonedFiscalScopeWithTamperedTenant()
        {
            using var context = CreateContext();
            Donation donation = PersistDonation(context);
            FiscalDocumentSeries series = PersistSeries(context, nextSequence: 42);
            FiscalScope tamperedScope = CloneFiscalScope(context, series.Id, tenantId: 2);
            object reservationService = CreateReservationService(context);
            object request = CreateScopedReservationRequest(
                donation.Id,
                series.Id,
                "tenant-tampered-key",
                tamperedScope);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => InvokeReserveOriginal(reservationService, request));

            Assert.Contains("tenant", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(context.FiscalDocuments);
            Assert.Equal(42, context.FiscalDocumentSeries.Single(item => item.Id == series.Id).NextSequence);
        }

        /// <summary>
        /// A reservation cannot infer a tenant when there is no HTTP context. It must fail
        /// before allocating a sequence instead of trusting a caller-provided scope tenant.
        /// </summary>
        [Fact]
        public void ReserveOriginal_RejectsMissingHttpContextBeforeSequenceAllocation()
        {
            using var context = CreateContext();
            Donation donation = PersistDonation(context);
            FiscalDocumentSeries series = PersistSeries(context, nextSequence: 42);
            object reservationService = CreateReservationService(context, new HttpContextAccessor());
            object request = CreateReservationRequest(context, donation.Id, series.Id, "missing-http-context");

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => InvokeReserveOriginal(reservationService, request));

            Assert.Contains("tenant", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(context.FiscalDocuments);
            Assert.Equal(42, context.FiscalDocumentSeries.Single(item => item.Id == series.Id).NextSequence);
        }

        /// <summary>
        /// An HTTP context without a resolved tenant is not a trusted tenant boundary and
        /// must not permit fiscal sequence allocation.
        /// </summary>
        [Fact]
        public void ReserveOriginal_RejectsHttpContextWithoutTenantBeforeSequenceAllocation()
        {
            using var context = CreateContext();
            Donation donation = PersistDonation(context);
            FiscalDocumentSeries series = PersistSeries(context, nextSequence: 42);
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext(),
            };
            object reservationService = CreateReservationService(context, httpContextAccessor);
            object request = CreateReservationRequest(context, donation.Id, series.Id, "http-context-without-tenant");

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => InvokeReserveOriginal(reservationService, request));

            Assert.Contains("tenant", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(context.FiscalDocuments);
            Assert.Equal(42, context.FiscalDocumentSeries.Single(item => item.Id == series.Id).NextSequence);
        }

        /// <summary>
        /// The HTTP tenant's invoicing strategy is part of the trusted boundary. A
        /// tenant-wide caller cannot issue through an otherwise valid FoodBank scope.
        /// </summary>
        [Fact]
        public void ReserveOriginal_RejectsSingleInvoiceTenantForFoodBankScopedSeries()
        {
            using var context = CreateContext();
            Donation donation = PersistDonation(context, foodBankId: 20);
            FiscalDocumentSeries series = PersistSeries(
                context,
                nextSequence: 42,
                scopeKind: FiscalScopeKind.FoodBank,
                foodBankId: 20);
            object reservationService = CreateReservationService(
                context,
                CreateTenantHttpContextAccessor(tenantId: 1, InvoicingStrategy.SingleInvoiceTable));
            object request = CreateReservationRequest(context, donation.Id, series.Id, "single-tenant-foodbank-scope");

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => InvokeReserveOriginal(reservationService, request));

            Assert.Contains("strategy", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(context.FiscalDocuments);
            Assert.Equal(42, context.FiscalDocumentSeries.Single(item => item.Id == series.Id).NextSequence);
        }

        /// <summary>
        /// A FoodBank-strategy tenant cannot issue through an otherwise valid tenant-wide
        /// scope, even if both the HTTP tenant and scope carry the same tenant identity.
        /// </summary>
        [Fact]
        public void ReserveOriginal_RejectsFoodBankTenantForSingleInvoiceScopedSeries()
        {
            using var context = CreateContext();
            Donation donation = PersistDonation(context);
            FiscalDocumentSeries series = PersistSeries(context, nextSequence: 42);
            object reservationService = CreateReservationService(
                context,
                CreateTenantHttpContextAccessor(tenantId: 1, InvoicingStrategy.MultipleTablesPerFoodBank));
            object request = CreateReservationRequest(context, donation.Id, series.Id, "foodbank-tenant-single-scope");

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => InvokeReserveOriginal(reservationService, request));

            Assert.Contains("strategy", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(context.FiscalDocuments);
            Assert.Equal(42, context.FiscalDocumentSeries.Single(item => item.Id == series.Id).NextSequence);
        }

        /// <summary>
        /// A series cannot reserve a document unless its persisted issuer profile identifies
        /// the signing key that will later produce the legally required fiscal signature.
        /// </summary>
        /// <param name="signingKeyIdentifier">The invalid non-null signing key identifier.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ReserveOriginal_RejectsMissingSigningKeyIdentifierBeforeSequenceAllocation(
            string signingKeyIdentifier)
        {
            using var context = CreateContext();
            Donation donation = PersistDonation(context);
            FiscalDocumentSeries series = PersistSeries(
                context,
                nextSequence: 42,
                signingKeyIdentifier: signingKeyIdentifier);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => ReserveOriginal(context, donation.Id, series.Id, "blank-signing-key"));

            Assert.Contains("series", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(context.FiscalDocuments);
            Assert.Equal(42, context.FiscalDocumentSeries.Single(item => item.Id == series.Id).NextSequence);
        }

        /// <summary>
        /// When another caller completes an idempotent reservation while this reservation is
        /// being saved, the service reloads that matching document. A later request with the
        /// same key but a different donation remains an error rather than an unrelated replay.
        /// </summary>
        [Fact]
        public void ReserveOriginal_ReloadsTransientIdempotencyCollisionAndRejectsMismatchedReplay()
        {
            using var context = CreateCollisionContext();
            Donation donation = PersistDonation(context);
            Donation unrelatedDonation = PersistDonation(context);
            FiscalDocumentSeries series = PersistSeries(context, nextSequence: 42);
            context.ArmIdempotencyCollision();

            FiscalDocument reserved = ReserveOriginal(context, donation.Id, series.Id, "concurrent-key");

            Assert.NotNull(context.CollidingDocument);
            Assert.Equal(context.CollidingDocument.Id, reserved.Id);
            Assert.Equal(donation.Id, reserved.DonationId);
            Assert.Equal(series.Id, reserved.SeriesId);
            Assert.Equal("concurrent-key", reserved.IdempotencyKey);
            Assert.Equal(1, context.FiscalDocuments.Count());
            Assert.Equal(43, context.FiscalDocumentSeries.Single(item => item.Id == series.Id).NextSequence);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => ReserveOriginal(context, unrelatedDonation.Id, series.Id, "concurrent-key"));

            Assert.Contains("idempotency", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(1, context.FiscalDocuments.Count());
        }

        /// <summary>
        /// A row-version or sequence conflict caused by another idempotency key is retried
        /// after reloading the series. The retried reservation receives the next sequence,
        /// not the conflicting sequence and not the original persistence exception.
        /// </summary>
        [Fact]
        public void ReserveOriginal_RetriesSequenceConflictFromDifferentIdempotencyKey()
        {
            using var context = CreateSequenceCollisionContext();
            Donation donation = PersistDonation(context);
            Donation concurrentDonation = PersistDonation(context);
            FiscalDocumentSeries series = PersistSeries(context, nextSequence: 42);
            context.ArmSequenceCollision(concurrentDonation.Id);

            FiscalDocument reserved = ReserveOriginal(context, donation.Id, series.Id, "retry-sequence-key");

            Assert.NotNull(context.ConcurrentDocument);
            Assert.Equal("competing-idempotency-key", context.ConcurrentDocument.IdempotencyKey);
            Assert.Equal(concurrentDonation.Id, context.ConcurrentDocument.DonationId);
            Assert.Equal(42, context.ConcurrentDocument.Sequence);
            Assert.Equal("retry-sequence-key", reserved.IdempotencyKey);
            Assert.Equal(donation.Id, reserved.DonationId);
            Assert.Equal(43, reserved.Sequence);
            Assert.Equal($"{series.AtcudValidationCode}-43", reserved.Atcud);
            Assert.Equal(2, context.FiscalDocuments.Count());
            Assert.Equal(44, context.FiscalDocumentSeries.Single(item => item.Id == series.Id).NextSequence);
        }

        /// <summary>
        /// The sequence-collision test simulates a <see cref="DbUpdateException"/> without a
        /// relational provider. Retrying must not recursively enter another reservation attempt
        /// while the first attempt's transaction is still in scope; that would open a second
        /// transaction before the first one is disposed.
        /// </summary>
        [Fact]
        public void ReserveOriginal_RetryDoesNotRecursivelyEnterAnotherTransactionScope()
        {
            Type serviceType = GetRequiredRepositoryType(ReservationServiceTypeName);
            MethodInfo[] reservationMethods = serviceType.GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(method => method.Name == "ReserveOriginal")
                .ToArray();

            Assert.NotEmpty(reservationMethods);
            Assert.All(
                reservationMethods,
                method => Assert.DoesNotContain(
                    GetCalledMethods(method),
                    calledMethod => calledMethod.Module == method.Module &&
                        calledMethod.MetadataToken == method.MetadataToken));
        }

        /// <summary>
        /// The series row version is the optimistic-concurrency contract for relational
        /// providers: a conflicting sequence allocation surfaces as a retryable concurrency
        /// failure instead of silently allocating the same sequence twice.
        /// </summary>
        [Fact]
        public void FiscalDocumentSeries_ModelUsesRowVersionForRetryableReservationConflicts()
        {
            using var context = CreateContext();
            IEntityType seriesEntity = Assert.IsAssignableFrom<IEntityType>(
                context.Model.FindEntityType(typeof(FiscalDocumentSeries)));
            IProperty rowVersion = Assert.IsAssignableFrom<IProperty>(seriesEntity.FindProperty("RowVersion"));

            Assert.True(rowVersion.IsConcurrencyToken);
            Assert.Equal(ValueGenerated.OnAddOrUpdate, rowVersion.ValueGenerated);
        }

        /// <summary>
        /// Blank ATCUD validation codes must be rejected by the reservation service before a
        /// sequence or document is consumed. Null codes are separately prevented by the
        /// persisted required-property constraint.
        /// </summary>
        /// <param name="validationCode">The invalid non-null validation code.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ReserveOriginal_RejectsEmptyAtcudValidationCodeBeforeSequenceAllocation(string validationCode)
        {
            using var context = CreateContext();
            Donation donation = PersistDonation(context);
            FiscalDocumentSeries series = PersistSeries(context, nextSequence: 42, validationCode: validationCode);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => ReserveOriginal(context, donation.Id, series.Id, "blank-atcud-code"));

            Assert.Contains("series", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(context.FiscalDocuments);
            Assert.Equal(42, context.FiscalDocumentSeries.Single(item => item.Id == series.Id).NextSequence);
        }

        /// <summary>
        /// Draft and structurally invalid series cannot reserve any sequence or ATCUD.
        /// </summary>
        /// <param name="status">The series lifecycle state.</param>
        /// <param name="firstSequence">The first sequence allowed by the series.</param>
        /// <param name="nextSequence">The sequence currently available for reservation.</param>
        [Theory]
        [InlineData(FiscalSeriesStatus.Draft, 1, 42)]
        [InlineData(FiscalSeriesStatus.Active, 43, 42)]
        public void ReserveOriginal_RejectsInactiveOrInvalidSeriesBeforeSequenceAllocation(
            FiscalSeriesStatus status,
            int firstSequence,
            int nextSequence)
        {
            using var context = CreateContext();
            Donation donation = PersistDonation(context);
            FiscalDocumentSeries series = PersistSeries(context, nextSequence, status, firstSequence: firstSequence);
            object reservationService = CreateReservationService(context);
            object request = CreateReservationRequest(context, donation.Id, series.Id, "invalid-series");

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => InvokeReserveOriginal(reservationService, request));

            Assert.Contains("series", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(nextSequence, context.FiscalDocumentSeries.Single(item => item.Id == series.Id).NextSequence);
            Assert.Empty(context.FiscalDocuments);
        }

        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private static IdempotencyCollisionApplicationDbContext CreateCollisionContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new IdempotencyCollisionApplicationDbContext(options);
        }

        private static SequenceCollisionApplicationDbContext CreateSequenceCollisionContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new SequenceCollisionApplicationDbContext(options);
        }

        private static object CreateReservationRequest(
            ApplicationDbContext context,
            int donationId,
            int seriesId,
            string idempotencyKey)
        {
            return CreateScopedReservationRequest(
                donationId,
                seriesId,
                idempotencyKey,
                CreateFiscalScope(context, seriesId));
        }

        private static object CreateUnscopedReservationRequest(int donationId, int seriesId, string idempotencyKey)
        {
            Type requestType = GetRequiredRepositoryType(ReservationRequestTypeName);
            object request = Activator.CreateInstance(requestType);
            requestType.GetProperty("DonationId").SetValue(request, donationId);
            requestType.GetProperty("SeriesId").SetValue(request, seriesId);
            requestType.GetProperty("IdempotencyKey").SetValue(request, idempotencyKey);
            return request;
        }

        private static object CreateScopedReservationRequest(
            int donationId,
            int seriesId,
            string idempotencyKey,
            FiscalScope fiscalScope)
        {
            object request = CreateUnscopedReservationRequest(donationId, seriesId, idempotencyKey);
            PropertyInfo fiscalScopeProperty = request.GetType().GetProperty("FiscalScope");

            Assert.NotNull(fiscalScopeProperty);
            Assert.Equal(typeof(FiscalScope), fiscalScopeProperty.PropertyType);
            fiscalScopeProperty.SetValue(request, fiscalScope);
            return request;
        }

        private static FiscalScope CreateFiscalScope(ApplicationDbContext context, int seriesId)
        {
            FiscalDocumentSeries series = context.FiscalDocumentSeries
                .Include(item => item.FiscalIssuerProfile)
                .Single(item => item.Id == seriesId);
            FiscalIssuerProfile profile = Assert.IsAssignableFrom<FiscalIssuerProfile>(series.FiscalIssuerProfile);

            return new FiscalScope
            {
                TenantId = 1,
                InvoicingStrategy = profile.ScopeKind == FiscalScopeKind.Tenant
                    ? BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy.InvoicingStrategy.SingleInvoiceTable
                    : BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy.InvoicingStrategy.MultipleTablesPerFoodBank,
                FoodBankId = profile.FoodBankId,
                FiscalIssuerProfile = profile,
                FiscalDocumentSeries = series,
            };
        }

        private static FiscalScope CloneFiscalScope(ApplicationDbContext context, int seriesId, int tenantId)
        {
            FiscalScope source = CreateFiscalScope(context, seriesId);
            FiscalIssuerProfile sourceProfile = source.FiscalIssuerProfile;
            FiscalDocumentSeries sourceSeries = source.FiscalDocumentSeries;

            return new FiscalScope
            {
                TenantId = tenantId,
                InvoicingStrategy = source.InvoicingStrategy,
                FoodBankId = source.FoodBankId,
                FiscalIssuerProfile = new FiscalIssuerProfile
                {
                    Id = sourceProfile.Id,
                    ScopeKind = sourceProfile.ScopeKind,
                    FoodBankId = sourceProfile.FoodBankId,
                    LegalName = sourceProfile.LegalName,
                    LegalAddress = sourceProfile.LegalAddress,
                    TaxRegistrationNumber = sourceProfile.TaxRegistrationNumber,
                    SaftDocumentType = sourceProfile.SaftDocumentType,
                    TaxRule = sourceProfile.TaxRule,
                    SoftwareCertificateNumber = sourceProfile.SoftwareCertificateNumber,
                    SigningKeyIdentifier = sourceProfile.SigningKeyIdentifier,
                    SigningKeyVersion = sourceProfile.SigningKeyVersion,
                    IsActive = sourceProfile.IsActive,
                    ValidFromUtc = sourceProfile.ValidFromUtc,
                    ValidToUtc = sourceProfile.ValidToUtc,
                },
                FiscalDocumentSeries = new FiscalDocumentSeries
                {
                    Id = sourceSeries.Id,
                    FiscalIssuerProfileId = sourceSeries.FiscalIssuerProfileId,
                    SeriesCode = sourceSeries.SeriesCode,
                    SaftDocumentType = sourceSeries.SaftDocumentType,
                    ProcessingMethod = sourceSeries.ProcessingMethod,
                    FirstSequence = sourceSeries.FirstSequence,
                    NextSequence = sourceSeries.NextSequence,
                    LastSequence = sourceSeries.LastSequence,
                    AtcudValidationCode = sourceSeries.AtcudValidationCode,
                    Status = sourceSeries.Status,
                    StartsAtUtc = sourceSeries.StartsAtUtc,
                    ClosedAtUtc = sourceSeries.ClosedAtUtc,
                    AtCommunicationReference = sourceSeries.AtCommunicationReference,
                },
            };
        }

        private static object CreateReservationService(ApplicationDbContext context)
        {
            return CreateReservationService(context, CreateTenantHttpContextAccessor(tenantId: 1));
        }

        private static object CreateReservationService(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            Type serviceType = GetRequiredRepositoryType(ReservationServiceTypeName);
            ConstructorInfo constructor = serviceType.GetConstructor(
                new[] { typeof(ApplicationDbContext), typeof(IHttpContextAccessor) });
            Assert.NotNull(constructor);
            return constructor.Invoke(new object[] { context, httpContextAccessor });
        }

        private static IHttpContextAccessor CreateTenantHttpContextAccessor(
            int tenantId,
            InvoicingStrategy invoicingStrategy = InvoicingStrategy.SingleInvoiceTable)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.SetTenant(new BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Tenant
            {
                Id = tenantId,
                InvoicingStrategy = invoicingStrategy,
            });

            return new HttpContextAccessor
            {
                HttpContext = httpContext,
            };
        }

        private static Type GetRequiredRepositoryType(string typeName)
        {
            return Assert.IsAssignableFrom<Type>(typeof(IFiscalScopeResolver).Assembly.GetType(typeName));
        }

        private static FiscalDocument InvokeReserveOriginal(object reservationService, object request)
        {
            MethodInfo method = reservationService.GetType().GetMethod("ReserveOriginal");
            Assert.NotNull(method);

            try
            {
                return Assert.IsAssignableFrom<FiscalDocument>(method.Invoke(reservationService, new[] { request }));
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }

        private static FiscalDocument ReserveOriginal(
            ApplicationDbContext context,
            int donationId,
            int seriesId,
            string idempotencyKey)
        {
            return InvokeReserveOriginal(
                CreateReservationService(context),
                CreateReservationRequest(context, donationId, seriesId, idempotencyKey));
        }

        private static Donation PersistDonation(ApplicationDbContext context, int? foodBankId = null)
        {
            var donation = new Donation
            {
                DonationDate = DateTime.UtcNow,
                FoodBank = foodBankId.HasValue
                    ? new FoodBank
                    {
                        Id = foodBankId.Value,
                        Name = "Banco Alimentar de teste",
                    }
                    : null,
            };
            context.Donations.Add(donation);
            context.SaveChanges();
            return donation;
        }

        private static FiscalDocumentSeries PersistSeries(
            ApplicationDbContext context,
            int nextSequence,
            FiscalSeriesStatus status = FiscalSeriesStatus.Active,
            string validationCode = "AA123",
            int firstSequence = 1,
            string signingKeyIdentifier = "test-key",
            FiscalScopeKind scopeKind = FiscalScopeKind.Tenant,
            int? foodBankId = null)
        {
            var profile = new FiscalIssuerProfile
            {
                ScopeKind = scopeKind,
                FoodBankId = foodBankId,
                LegalName = "Emitente de teste",
                LegalAddress = "Rua de teste, 1, Lisboa",
                TaxRegistrationNumber = "501234567",
                SaftDocumentType = "RC",
                TaxRule = "M00",
                SoftwareCertificateNumber = "0",
                SigningKeyIdentifier = signingKeyIdentifier,
                SigningKeyVersion = "v1",
                IsActive = true,
                ValidFromUtc = DateTimeOffset.UtcNow.AddHours(-1),
            };
            context.FiscalIssuerProfiles.Add(profile);
            context.SaveChanges();

            var series = new FiscalDocumentSeries
            {
                FiscalIssuerProfileId = profile.Id,
                SeriesCode = $"RC-{Guid.NewGuid():N}",
                SaftDocumentType = "RC",
                ProcessingMethod = FiscalProcessingMethod.CertifiedSoftware,
                FirstSequence = firstSequence,
                NextSequence = nextSequence,
                AtcudValidationCode = validationCode,
                Status = status,
                StartsAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
                AtCommunicationReference = "at-communication-test",
            };
            context.FiscalDocumentSeries.Add(series);
            context.SaveChanges();
            return series;
        }

        private static IReadOnlyCollection<MethodBase> GetCalledMethods(MethodInfo method)
        {
            byte[] instructions = Assert.IsType<byte[]>(method.GetMethodBody().GetILAsByteArray());
            var calledMethods = new List<MethodBase>();
            int position = 0;

            while (position < instructions.Length)
            {
                OpCode opcode = ReadOpCode(instructions, ref position);
                if (opcode.OperandType == OperandType.InlineMethod)
                {
                    int metadataToken = BitConverter.ToInt32(instructions, position);
                    calledMethods.Add(method.Module.ResolveMethod(metadataToken));
                }

                position += GetOperandLength(opcode.OperandType, instructions, position);
            }

            return calledMethods;
        }

        private static OpCode ReadOpCode(byte[] instructions, ref int position)
        {
            short value = instructions[position++] == 0xfe
                ? (short)(0xfe00 | instructions[position++])
                : (short)instructions[position - 1];

            return typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(field => field.GetValue(null))
                .OfType<OpCode>()
                .Single(opcode => opcode.Value == value);
        }

        private static int GetOperandLength(OperandType operandType, byte[] instructions, int position)
        {
            return operandType switch
            {
                OperandType.InlineNone => 0,
                OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                OperandType.InlineVar => 2,
                OperandType.InlineBrTarget or OperandType.InlineField or OperandType.InlineI or OperandType.InlineMethod or
                    OperandType.InlineSig or OperandType.InlineString or OperandType.InlineTok or OperandType.InlineType or
                    OperandType.ShortInlineR => 4,
                OperandType.InlineI8 or OperandType.InlineR => 8,
                OperandType.InlineSwitch => 4 + (BitConverter.ToInt32(instructions, position) * 4),
                _ => throw new InvalidOperationException("Unsupported IL operand type."),
            };
        }

        private static FiscalDocument CloneFiscalDocument(FiscalDocument source, string idempotencyKey)
        {
            return new FiscalDocument
            {
                DonationId = source.DonationId,
                SeriesId = source.SeriesId,
                FiscalIssuerProfileId = source.FiscalIssuerProfileId,
                InvoicingStrategy = source.InvoicingStrategy,
                FoodBankId = source.FoodBankId,
                Kind = source.Kind,
                Status = source.Status,
                Sequence = source.Sequence,
                DocumentNumber = source.DocumentNumber,
                Atcud = source.Atcud,
                IdempotencyKey = idempotencyKey,
                IssuedAtUtc = source.IssuedAtUtc,
                Hash = source.Hash,
                HashControl = source.HashControl,
                SigningKeyVersion = source.SigningKeyVersion,
                QrPayload = source.QrPayload,
                SoftwareCertificateNumber = source.SoftwareCertificateNumber,
                IssuerLegalName = source.IssuerLegalName,
                IssuerTaxRegistrationNumber = source.IssuerTaxRegistrationNumber,
                IssuerLegalAddress = source.IssuerLegalAddress,
                TaxRule = source.TaxRule,
            };
        }

        private sealed class IdempotencyCollisionApplicationDbContext : ApplicationDbContext
        {
            private bool idempotencyCollisionArmed;

            public IdempotencyCollisionApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
                : base(options)
            {
            }

            public FiscalDocument CollidingDocument { get; private set; }

            public void ArmIdempotencyCollision()
            {
                this.idempotencyCollisionArmed = true;
            }

            public override int SaveChanges(bool acceptAllChangesOnSuccess)
            {
                if (!this.idempotencyCollisionArmed)
                {
                    return base.SaveChanges(acceptAllChangesOnSuccess);
                }

                this.idempotencyCollisionArmed = false;
                FiscalDocument pendingDocument = this.ChangeTracker.Entries<FiscalDocument>()
                    .Single(entry => entry.State == EntityState.Added).Entity;
                this.Entry(pendingDocument).State = EntityState.Detached;
                this.CollidingDocument = CloneFiscalDocument(pendingDocument, pendingDocument.IdempotencyKey);
                this.FiscalDocuments.Add(this.CollidingDocument);
                base.SaveChanges(acceptAllChangesOnSuccess);
                throw new DbUpdateException("Simulated concurrent idempotency reservation.");
            }
        }

        private sealed class SequenceCollisionApplicationDbContext : ApplicationDbContext
        {
            private int concurrentDonationId;
            private bool sequenceCollisionArmed;

            public SequenceCollisionApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
                : base(options)
            {
            }

            public FiscalDocument ConcurrentDocument { get; private set; }

            public void ArmSequenceCollision(int donationId)
            {
                this.concurrentDonationId = donationId;
                this.sequenceCollisionArmed = true;
            }

            public override int SaveChanges(bool acceptAllChangesOnSuccess)
            {
                if (!this.sequenceCollisionArmed)
                {
                    return base.SaveChanges(acceptAllChangesOnSuccess);
                }

                this.sequenceCollisionArmed = false;
                FiscalDocument pendingDocument = this.ChangeTracker.Entries<FiscalDocument>()
                    .Single(entry => entry.State == EntityState.Added).Entity;
                this.Entry(pendingDocument).State = EntityState.Detached;
                this.ConcurrentDocument = CloneFiscalDocument(pendingDocument, "competing-idempotency-key");
                this.ConcurrentDocument.DonationId = this.concurrentDonationId;
                this.FiscalDocuments.Add(this.ConcurrentDocument);
                base.SaveChanges(acceptAllChangesOnSuccess);
                throw new DbUpdateException("Simulated concurrent series sequence reservation.");
            }
        }
    }
}
