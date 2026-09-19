// -----------------------------------------------------------------------
// <copyright file="FiscalDocumentReservationTddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Linq;
    using System.Reflection;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
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
            object request = CreateReservationRequest(donation.Id, series.Id, "invalid-series");

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

        private static object CreateReservationRequest(int donationId, int seriesId, string idempotencyKey)
        {
            Type requestType = GetRequiredRepositoryType(ReservationRequestTypeName);
            object request = Activator.CreateInstance(requestType);
            requestType.GetProperty("DonationId").SetValue(request, donationId);
            requestType.GetProperty("SeriesId").SetValue(request, seriesId);
            requestType.GetProperty("IdempotencyKey").SetValue(request, idempotencyKey);
            return request;
        }

        private static object CreateReservationService(ApplicationDbContext context)
        {
            Type serviceType = GetRequiredRepositoryType(ReservationServiceTypeName);
            ConstructorInfo constructor = serviceType.GetConstructor(new[] { typeof(ApplicationDbContext) });
            Assert.NotNull(constructor);
            return constructor.Invoke(new object[] { context });
        }

        private static Type GetRequiredRepositoryType(string typeName)
        {
            return Assert.IsAssignableFrom<Type>(typeof(IFiscalScopeResolver).Assembly.GetType(typeName));
        }

        private static FiscalDocument InvokeReserveOriginal(object reservationService, object request)
        {
            MethodInfo method = reservationService.GetType().GetMethod("ReserveOriginal");
            Assert.NotNull(method);
            return Assert.IsAssignableFrom<FiscalDocument>(method.Invoke(reservationService, new[] { request }));
        }

        private static FiscalDocument ReserveOriginal(
            ApplicationDbContext context,
            int donationId,
            int seriesId,
            string idempotencyKey)
        {
            return InvokeReserveOriginal(
                CreateReservationService(context),
                CreateReservationRequest(donationId, seriesId, idempotencyKey));
        }

        private static Donation PersistDonation(ApplicationDbContext context)
        {
            var donation = new Donation
            {
                DonationDate = DateTime.UtcNow,
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
            int firstSequence = 1)
        {
            var profile = new FiscalIssuerProfile
            {
                ScopeKind = FiscalScopeKind.Tenant,
                LegalName = "Emitente de teste",
                LegalAddress = "Rua de teste, 1, Lisboa",
                TaxRegistrationNumber = "501234567",
                SaftDocumentType = "RC",
                TaxRule = "M00",
                SoftwareCertificateNumber = "0",
                SigningKeyIdentifier = "test-key",
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
    }
}
