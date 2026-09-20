// -----------------------------------------------------------------------
// <copyright file="FiscalEnvironmentGateTddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.ExceptionServices;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Moq;
    using Xunit;

    /// <summary>
    /// Defines the fail-closed environment policy for fiscal issuance.
    /// </summary>
    public class FiscalEnvironmentGateTddTests
    {
        private const string GateInterfaceTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.IFiscalEnvironmentGate";
        private const string GateTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalEnvironmentGate";
        private const string PreProductionSeriesModeKey = "Fiscal:EnvironmentGate:PreProductionSeriesMode";
        private const string ProductionSigningReadyKey = "Fiscal:EnvironmentGate:ProductionSigningReady";

        /// <summary>
        /// The gate must be registered as an explicit dependency, rather than inferred from a
        /// series name or an Azure resource. It receives the host environment and configuration
        /// using the application's standard configuration patterns.
        /// </summary>
        [Fact]
        public void FiscalEnvironmentGate_ExposesConfiguredHostEnvironmentContract()
        {
            Type gateInterface = GetRequiredRepositoryType(GateInterfaceTypeName);
            Type gateType = GetRequiredRepositoryType(GateTypeName);

            MethodInfo ensureIssuanceAllowed = Assert.Single(
                gateInterface.GetMethods(),
                method => method.Name == "EnsureIssuanceAllowed" &&
                    method.ReturnType == typeof(void) &&
                    method.GetParameters().Select(parameter => parameter.ParameterType)
                        .SequenceEqual(new[] { typeof(FiscalIssuerProfile), typeof(FiscalDocumentSeries) }));

            Assert.True(gateInterface.IsAssignableFrom(gateType));
            Assert.NotNull(gateType.GetConstructor(new[] { typeof(IConfiguration), typeof(IWebHostEnvironment) }));
            Assert.Equal("EnsureIssuanceAllowed", ensureIssuanceAllowed.Name);
        }

        /// <summary>
        /// Development and testing must use an AT test-series validation code. A production code
        /// would otherwise make it possible to issue a real fiscal number from a non-production
        /// deployment.
        /// </summary>
        [Theory]
        [InlineData("Development")]
        [InlineData("Testing")]
        public void EnsureIssuanceAllowed_NonProductionAcceptsOnlyAtTestValidationCodes(string environmentName)
        {
            object gate = CreateGate(environmentName);
            FiscalIssuerProfile profile = CreateProfile(softwareCertificateNumber: "0");
            FiscalDocumentSeries testSeries = CreateSeries("AA123456789");

            InvokeEnsureIssuanceAllowed(gate, profile, testSeries);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => InvokeEnsureIssuanceAllowed(gate, profile, CreateSeries("PT123456789")));

            Assert.Contains("test", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Production rejects AT test-series codes and does not accept the test software
        /// certificate number. A production signing provider must also be explicitly ready.
        /// </summary>
        [Theory]
        [InlineData("AA123456789", "1234", true, "test")]
        [InlineData("PT123456789", "0", true, "certificate")]
        [InlineData("PT123456789", "1234", false, "sign")]
        public void EnsureIssuanceAllowed_ProductionRejectsTestOrUnreadyConfiguration(
            string validationCode,
            string softwareCertificateNumber,
            bool productionSigningReady,
            string expectedReason)
        {
            object gate = CreateGate(
                "Production",
                preProductionSeriesMode: null,
                productionSigningReady: productionSigningReady);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => InvokeEnsureIssuanceAllowed(
                    gate,
                    CreateProfile(softwareCertificateNumber),
                    CreateSeries(validationCode)));

            Assert.Contains(expectedReason, exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Pre-production has no implicit behaviour: it must declare whether it uses AT test or
        /// production series. The chosen policy is applied as strictly as its corresponding
        /// environment.
        /// </summary>
        [Theory]
        [InlineData(null, "AA123456789", false)]
        [InlineData("Test", "AA123456789", true)]
        [InlineData("Test", "PT123456789", false)]
        [InlineData("Production", "PT123456789", true)]
        [InlineData("Production", "AA123456789", false)]
        public void EnsureIssuanceAllowed_PreProductionRequiresAndHonoursExplicitSeriesPolicy(
            string preProductionSeriesMode,
            string validationCode,
            bool shouldBeAllowed)
        {
            object gate = CreateGate(
                "Staging",
                preProductionSeriesMode,
                productionSigningReady: true);
            Action action = () => InvokeEnsureIssuanceAllowed(
                gate,
                CreateProfile("1234"),
                CreateSeries(validationCode));

            if (shouldBeAllowed)
            {
                action();
            }
            else
            {
                Assert.Throws<InvalidOperationException>(action);
            }
        }

        /// <summary>
        /// Scope resolution must fail before a caller can receive a scope containing a
        /// production series in a development deployment.
        /// </summary>
        [Fact]
        public void Resolve_DevelopmentProductionSeriesIsRejectedBeforeSequenceAllocation()
        {
            using var context = CreateContext();
            FiscalIssuerProfile profile = PersistProfile(context, "0");
            FiscalDocumentSeries series = PersistSeries(context, profile, "PT123456789", nextSequence: 42);
            IFiscalScopeResolver resolver = CreateResolver(context, CreateGate("Development"));
            Donation donation = PersistDonation(context);

            Assert.Throws<InvalidOperationException>(() => resolver.Resolve(CreateTenant(), donation));

            Assert.Equal(42, context.FiscalDocumentSeries.Single(item => item.Id == series.Id).NextSequence);
            Assert.Empty(context.FiscalDocuments);
        }

        /// <summary>
        /// Reservation must invoke the environment-aware scope resolver before it changes the
        /// series counter or creates a draft. This protects direct service callers as well as UI
        /// and payment entry points.
        /// </summary>
        [Fact]
        public void ReserveOriginal_ProductionTestSeriesIsRejectedBeforeSequenceAllocation()
        {
            using var context = CreateContext();
            FiscalIssuerProfile profile = PersistProfile(context, "1234");
            FiscalDocumentSeries series = PersistSeries(context, profile, "AA123456789", nextSequence: 42);
            Donation donation = PersistDonation(context);
            IFiscalScopeResolver resolver = CreateResolver(
                context,
                CreateGate("Production", productionSigningReady: true));
            var httpContext = new DefaultHttpContext();
            httpContext.SetTenant(CreateTenant());
            var reservationService = new FiscalDocumentReservationService(
                context,
                new HttpContextAccessor { HttpContext = httpContext },
                resolver);

            Assert.Throws<InvalidOperationException>(() => reservationService.ReserveOriginal(new FiscalDocumentReservationRequest
            {
                DonationId = donation.Id,
                SeriesId = series.Id,
                IdempotencyKey = "environment-gate-reservation",
                FiscalScope = CreateScope(profile, series),
            }));

            Assert.Equal(42, context.FiscalDocumentSeries.Single(item => item.Id == series.Id).NextSequence);
            Assert.Empty(context.FiscalDocuments);
        }

        /// <summary>
        /// Issuance must fail at scope resolution before asking the reservation service to
        /// allocate a number. This protects payment callbacks from creating fiscal drafts when
        /// the deployment does not have an allowed environment configuration.
        /// </summary>
        [Fact]
        public void IssueOriginal_ProductionTestSeriesIsRejectedBeforeReservationOrSequenceAllocation()
        {
            using var context = CreateContext();
            FiscalIssuerProfile profile = PersistProfile(context, "1234");
            FiscalDocumentSeries series = PersistSeries(context, profile, "AA123456789", nextSequence: 42);
            Donation donation = PersistPaidDonation(context);
            IFiscalScopeResolver resolver = CreateResolver(
                context,
                CreateGate("Production", productionSigningReady: true));
            TestReservationService reservationService = CreateReservationService(out IFiscalDocumentReservationService reservationProxy);
            var issuanceService = new FiscalDocumentIssuanceService(
                context,
                resolver,
                reservationProxy,
                CreateProxy<IFiscalSignatureService, ThrowingProxy>(),
                CreateProxy<IFiscalQrPayloadBuilder, ThrowingProxy>(),
                CreateProxy<IFiscalQrCodeImageGenerator, ThrowingProxy>(),
                new FiscalReceiptViewModelFactory(),
                new FiscalReceiptPdfService(
                    CreateProxy<IFiscalReceiptPdfRenderer, ThrowingProxy>(),
                    CreateProxy<IFiscalReceiptPdfBlobStore, ThrowingProxy>()));

            Assert.Throws<InvalidOperationException>(() => issuanceService.IssueOriginal(CreateTenant(), donation));

            Assert.Empty(reservationService.Requests);
            Assert.Equal(42, context.FiscalDocumentSeries.Single(item => item.Id == series.Id).NextSequence);
            Assert.Empty(context.FiscalDocuments);
        }

        private static object CreateGate(
            string environmentName,
            string preProductionSeriesMode = null,
            bool productionSigningReady = false)
        {
            var values = new Dictionary<string, string>
            {
                [ProductionSigningReadyKey] = productionSigningReady.ToString(),
            };
            if (preProductionSeriesMode != null)
            {
                values[PreProductionSeriesModeKey] = preProductionSeriesMode;
            }

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();
            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(item => item.EnvironmentName).Returns(environmentName);
            Type gateType = GetRequiredRepositoryType(GateTypeName);
            ConstructorInfo constructor = gateType.GetConstructor(
                new[] { typeof(IConfiguration), typeof(IWebHostEnvironment) });
            Assert.NotNull(constructor);

            return constructor.Invoke(new object[] { configuration, environment.Object });
        }

        private static IFiscalScopeResolver CreateResolver(ApplicationDbContext context, object gate)
        {
            Type gateInterface = GetRequiredRepositoryType(GateInterfaceTypeName);
            ConstructorInfo constructor = typeof(FiscalScopeResolver).GetConstructor(
                new[] { typeof(ApplicationDbContext), gateInterface });
            Assert.NotNull(constructor);
            return Assert.IsAssignableFrom<IFiscalScopeResolver>(constructor.Invoke(new[] { (object)context, gate }));
        }

        private static void InvokeEnsureIssuanceAllowed(
            object gate,
            FiscalIssuerProfile profile,
            FiscalDocumentSeries series)
        {
            MethodInfo method = gate.GetType().GetMethod(
                "EnsureIssuanceAllowed",
                new[] { typeof(FiscalIssuerProfile), typeof(FiscalDocumentSeries) });
            Assert.NotNull(method);
            try
            {
                method.Invoke(gate, new object[] { profile, series });
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }

        private static Type GetRequiredRepositoryType(string typeName)
        {
            return Assert.IsAssignableFrom<Type>(typeof(IFiscalScopeResolver).Assembly.GetType(typeName));
        }

        private static ApplicationDbContext CreateContext()
        {
            DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        private static Tenant CreateTenant()
        {
            return new Tenant
            {
                Id = 7,
                InvoicingStrategy = InvoicingStrategy.SingleInvoiceTable,
            };
        }

        private static FiscalIssuerProfile CreateProfile(string softwareCertificateNumber)
        {
            return new FiscalIssuerProfile
            {
                Id = 8,
                ScopeKind = FiscalScopeKind.Tenant,
                LegalName = "Emitente de teste",
                LegalAddress = "Rua de teste, 1, Lisboa",
                TaxRegistrationNumber = "501234567",
                SaftDocumentType = "RC",
                TaxRule = "M00",
                SoftwareCertificateNumber = softwareCertificateNumber,
                SigningKeyIdentifier = "fiscal-signing-key",
                SigningKeyVersion = "1",
                IsActive = true,
                ValidFromUtc = DateTimeOffset.UtcNow.AddHours(-1),
            };
        }

        private static FiscalDocumentSeries CreateSeries(string validationCode)
        {
            return new FiscalDocumentSeries
            {
                Id = 9,
                FiscalIssuerProfileId = 8,
                SeriesCode = "RC-2026",
                SaftDocumentType = "RC",
                ProcessingMethod = FiscalProcessingMethod.CertifiedSoftware,
                FirstSequence = 1,
                NextSequence = 42,
                AtcudValidationCode = validationCode,
                Status = FiscalSeriesStatus.Active,
                StartsAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
                AtCommunicationReference = "at-communication-test",
            };
        }

        private static FiscalIssuerProfile PersistProfile(ApplicationDbContext context, string softwareCertificateNumber)
        {
            FiscalIssuerProfile profile = CreateProfile(softwareCertificateNumber);
            context.FiscalIssuerProfiles.Add(profile);
            context.SaveChanges();
            return profile;
        }

        private static FiscalDocumentSeries PersistSeries(
            ApplicationDbContext context,
            FiscalIssuerProfile profile,
            string validationCode,
            int nextSequence)
        {
            FiscalDocumentSeries series = CreateSeries(validationCode);
            series.FiscalIssuerProfileId = profile.Id;
            series.NextSequence = nextSequence;
            context.FiscalDocumentSeries.Add(series);
            context.SaveChanges();
            return series;
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

        private static Donation PersistPaidDonation(ApplicationDbContext context)
        {
            var donation = new Donation
            {
                DonationDate = DateTime.UtcNow,
                PaymentStatus = PaymentStatus.Payed,
                ConfirmedPayment = new CreditCardPayment
                {
                    IdempotencyKey = "environment-gate-payment",
                    Created = DateTime.UtcNow,
                    Completed = DateTime.UtcNow,
                    Status = "paid",
                },
            };
            context.Donations.Add(donation);
            context.SaveChanges();
            return donation;
        }

        private static FiscalScope CreateScope(FiscalIssuerProfile profile, FiscalDocumentSeries series)
        {
            return new FiscalScope
            {
                TenantId = 7,
                InvoicingStrategy = InvoicingStrategy.SingleInvoiceTable,
                FiscalIssuerProfile = profile,
                FiscalDocumentSeries = series,
            };
        }

        private static TestReservationService CreateReservationService(out IFiscalDocumentReservationService proxy)
        {
            proxy = CreateProxy<IFiscalDocumentReservationService, TestReservationService>();
            return Assert.IsType<TestReservationService>(proxy);
        }

        private static TInterface CreateProxy<TInterface, TProxy>()
            where TInterface : class
            where TProxy : DispatchProxy
        {
            return DispatchProxy.Create<TInterface, TProxy>();
        }

        private class TestReservationService : DispatchProxy
        {
            public ICollection<FiscalDocumentReservationRequest> Requests { get; } = new List<FiscalDocumentReservationRequest>();

            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                this.Requests.Add(Assert.IsType<FiscalDocumentReservationRequest>(Assert.Single(args)));
                throw new InvalidOperationException("The environment gate must reject before reservation.");
            }
        }

        private class ThrowingProxy : DispatchProxy
        {
            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                throw new InvalidOperationException($"Unexpected fiscal dependency call: {targetMethod.Name}.");
            }
        }
    }
}
