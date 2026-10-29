// -----------------------------------------------------------------------
// <copyright file="FiscalDocumentIssuanceTddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using Microsoft.EntityFrameworkCore;
    using Xunit;

    /// <summary>
    /// Defines the transactional orchestration contract for issuing a paid donation's fiscal document.
    /// </summary>
    public class FiscalDocumentIssuanceTddTests
    {
        private const string IssuanceServiceInterfaceTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.IFiscalDocumentIssuanceService";
        private const string IssuanceServiceTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalDocumentIssuanceService";

        /// <summary>
        /// Fiscal issuance receives the already trusted tenant and paid, persisted donation. It
        /// coordinates scope resolution, idempotent reservation, signing, QR construction and
        /// immutable PDF delivery without putting those fiscal rules in payment handlers.
        /// </summary>
        [Fact]
        public void FiscalDocumentIssuanceService_ExposesPaidDonationOrchestrationContract()
        {
            Type serviceInterfaceType = GetRequiredRepositoryType(IssuanceServiceInterfaceTypeName);
            Type serviceType = GetRequiredRepositoryType(IssuanceServiceTypeName);

            MethodInfo issueOriginal = Assert.Single(
                serviceInterfaceType.GetMethods(),
                method => method.Name == "IssueOriginal" &&
                    method.ReturnType == typeof(FiscalDocument) &&
                    method.GetParameters().Select(parameter => parameter.ParameterType)
                        .SequenceEqual(new[] { typeof(Tenant), typeof(Donation) }));

            Assert.True(serviceInterfaceType.IsAssignableFrom(serviceType));
            Assert.NotNull(serviceType.GetConstructor(new[]
            {
                typeof(ApplicationDbContext),
                typeof(IFiscalScopeResolver),
                typeof(IFiscalDocumentReservationService),
                typeof(IFiscalSignatureService),
                typeof(IFiscalQrPayloadBuilder),
                typeof(IFiscalQrCodeImageGenerator),
                typeof(FiscalReceiptViewModelFactory),
                typeof(FiscalReceiptPdfService),
            }));
            Assert.Equal("IssueOriginal", issueOriginal.Name);
        }

        /// <summary>
        /// One paid donation is resolved within the trusted tenant, reserved with the payment's
        /// durable key, signed, QR encoded and rendered only after all final fiscal evidence is
        /// present. The persisted document is the complete immutable result.
        /// </summary>
        [Fact]
        public void IssueOriginal_ResolvesReservesSignsEncodesAndPersistsFinalFiscalSnapshot()
        {
            using var context = CreateContext();
            Tenant tenant = CreateTenant();
            Donation donation = PersistPaidDonation(context, "payment-confirmation-42");
            FiscalScope fiscalScope = CreateFiscalScope();
            FiscalDocument reservedDocument = CreateReservedDocument(donation.Id, fiscalScope);
            TestScopeResolver scopeResolver = CreateScopeResolver(fiscalScope, out object scopeResolverProxy);
            TestReservationService reservationService = CreateReservationService(
                request => PersistReservedDocument(context, reservedDocument, request),
                out object reservationServiceProxy);
            TestSignatureService signatureService = CreateSignatureService(out object signatureServiceProxy);
            TestQrPayloadBuilder qrPayloadBuilder = CreateQrPayloadBuilder(out object qrPayloadBuilderProxy);
            TestQrCodeImageGenerator qrCodeImageGenerator = CreateQrCodeImageGenerator(out object qrCodeImageGeneratorProxy);
            TestPdfRenderer pdfRenderer = CreatePdfRenderer(out object pdfRendererProxy);
            TestPdfBlobStore pdfBlobStore = CreatePdfBlobStore(out object pdfBlobStoreProxy);
            object issuanceService = CreateIssuanceService(
                context,
                scopeResolverProxy,
                reservationServiceProxy,
                signatureServiceProxy,
                qrPayloadBuilderProxy,
                qrCodeImageGeneratorProxy,
                pdfRendererProxy,
                pdfBlobStoreProxy);

            FiscalDocument issued = IssueOriginal(issuanceService, tenant, donation);

            Assert.Equal(donation.Id, scopeResolver.DonationIds.Single());
            Assert.Equal(tenant.Id, scopeResolver.TenantIds.Single());
            FiscalDocumentReservationRequest reservationRequest = Assert.Single(reservationService.Requests);
            Assert.Equal(donation.Id, reservationRequest.DonationId);
            Assert.Equal(fiscalScope.FiscalDocumentSeries.Id, reservationRequest.SeriesId);
            Assert.Equal("payment-confirmation-42", reservationRequest.IdempotencyKey);
            Assert.Same(fiscalScope, reservationRequest.FiscalScope);
            Assert.Single(signatureService.Requests);
            Assert.Equal("fiscal-signature", issued.Hash);
            Assert.Equal("previous-fiscal-signature", issued.PreviousHash);
            Assert.Equal("3", issued.HashControl);
            Assert.Equal("A:501234567*H:VALIDATION-42", issued.QrPayload);
            Assert.Equal(TestQrCodeImageGenerator.PngBytes, issued.QrPng);
            Assert.Equal(FiscalDocumentStatus.Issued, issued.Status);
            Assert.NotNull(issued.PdfBlobName);
            Assert.False(string.IsNullOrWhiteSpace(issued.PdfChecksum));
            Assert.Single(qrPayloadBuilder.Snapshots);
            Assert.Equal("fiscal-signature", qrPayloadBuilder.Snapshots.Single().Hash);
            Assert.Equal("3", qrPayloadBuilder.Snapshots.Single().HashControl);
            Assert.Single(qrCodeImageGenerator.Payloads);
            Assert.Single(pdfRenderer.ViewModels);
            Assert.Equal("A:501234567*H:VALIDATION-42", pdfRenderer.ViewModels.Single().QrPayload);
            Assert.Equal("3", pdfRenderer.ViewModels.Single().HashControl);
            Assert.Single(pdfBlobStore.SavedBlobs);

            context.ChangeTracker.Clear();
            FiscalDocument persisted = context.FiscalDocuments.Single(document => document.Id == issued.Id);
            Assert.Equal(FiscalDocumentStatus.Issued, persisted.Status);
            Assert.Equal("fiscal-signature", persisted.Hash);
            Assert.Equal("A:501234567*H:VALIDATION-42", persisted.QrPayload);
            Assert.NotNull(persisted.PdfBlobName);
        }

        /// <summary>
        /// A webhook, thank-you e-mail and later download can all request issuance for the same
        /// paid donation. They receive the same original fiscal document and do not repeat
        /// signing, QR generation or the immutable PDF write.
        /// </summary>
        [Fact]
        public void IssueOriginal_RepeatedPaymentWebhookAndEmailTriggersReturnSameDocumentWithoutRepeatingArtefacts()
        {
            using var context = CreateContext();
            Tenant tenant = CreateTenant();
            Donation donation = PersistPaidDonation(context, "provider-payment-99");
            FiscalScope fiscalScope = CreateFiscalScope();
            FiscalDocument reservedDocument = CreateReservedDocument(donation.Id, fiscalScope);
            TestScopeResolver scopeResolver = CreateScopeResolver(fiscalScope, out object scopeResolverProxy);
            TestReservationService reservationService = CreateReservationService(
                request => PersistReservedDocument(context, reservedDocument, request),
                out object reservationServiceProxy);
            TestSignatureService signatureService = CreateSignatureService(out object signatureServiceProxy);
            TestQrPayloadBuilder qrPayloadBuilder = CreateQrPayloadBuilder(out object qrPayloadBuilderProxy);
            TestQrCodeImageGenerator qrCodeImageGenerator = CreateQrCodeImageGenerator(out object qrCodeImageGeneratorProxy);
            TestPdfRenderer pdfRenderer = CreatePdfRenderer(out object pdfRendererProxy);
            TestPdfBlobStore pdfBlobStore = CreatePdfBlobStore(out object pdfBlobStoreProxy);
            object issuanceService = CreateIssuanceService(
                context,
                scopeResolverProxy,
                reservationServiceProxy,
                signatureServiceProxy,
                qrPayloadBuilderProxy,
                qrCodeImageGeneratorProxy,
                pdfRendererProxy,
                pdfBlobStoreProxy);

            FiscalDocument paymentIssued = IssueOriginal(issuanceService, tenant, donation);
            FiscalDocument webhookIssued = IssueOriginal(issuanceService, tenant, donation);
            FiscalDocument emailIssued = IssueOriginal(issuanceService, tenant, donation);

            Assert.Equal(paymentIssued.Id, webhookIssued.Id);
            Assert.Equal(paymentIssued.Id, emailIssued.Id);
            Assert.Single(reservationService.Requests);
            Assert.Single(signatureService.Requests);
            Assert.Single(qrPayloadBuilder.Snapshots);
            Assert.Single(qrCodeImageGenerator.Payloads);
            Assert.Single(pdfRenderer.ViewModels);
            Assert.Single(pdfBlobStore.SavedBlobs);
        }

        /// <summary>
        /// If the production signing operation is unavailable, issuance fails closed. It must
        /// not expose an issued document or continue into QR/PDF creation with unsigned fiscal
        /// content.
        /// </summary>
        [Fact]
        public void IssueOriginal_FailsClosedWhenProductionSigningIsUnavailable()
        {
            using var context = CreateContext();
            Tenant tenant = CreateTenant();
            Donation donation = PersistPaidDonation(context, "payment-signing-unavailable");
            FiscalScope fiscalScope = CreateFiscalScope();
            FiscalDocument reservedDocument = CreateReservedDocument(donation.Id, fiscalScope);
            TestScopeResolver scopeResolver = CreateScopeResolver(fiscalScope, out object scopeResolverProxy);
            TestReservationService reservationService = CreateReservationService(
                request => PersistReservedDocument(context, reservedDocument, request),
                out object reservationServiceProxy);
            TestSignatureService signatureService = CreateSignatureService(out object signatureServiceProxy);
            signatureService.ExceptionToThrow = new InvalidOperationException("Production signing key is unavailable.");
            TestQrPayloadBuilder qrPayloadBuilder = CreateQrPayloadBuilder(out object qrPayloadBuilderProxy);
            TestQrCodeImageGenerator qrCodeImageGenerator = CreateQrCodeImageGenerator(out object qrCodeImageGeneratorProxy);
            TestPdfRenderer pdfRenderer = CreatePdfRenderer(out object pdfRendererProxy);
            TestPdfBlobStore pdfBlobStore = CreatePdfBlobStore(out object pdfBlobStoreProxy);
            object issuanceService = CreateIssuanceService(
                context,
                scopeResolverProxy,
                reservationServiceProxy,
                signatureServiceProxy,
                qrPayloadBuilderProxy,
                qrCodeImageGeneratorProxy,
                pdfRendererProxy,
                pdfBlobStoreProxy);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => IssueOriginal(issuanceService, tenant, donation));

            Assert.Contains("unavailable", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Single(reservationService.Requests);
            Assert.Single(signatureService.Requests);
            Assert.Empty(qrPayloadBuilder.Snapshots);
            Assert.Empty(qrCodeImageGenerator.Payloads);
            Assert.Empty(pdfRenderer.ViewModels);
            Assert.Empty(pdfBlobStore.SavedBlobs);
            Assert.Equal(FiscalDocumentStatus.Draft, reservedDocument.Status);
            Assert.Equal(string.Empty, reservedDocument.QrPayload);
            Assert.Empty(reservedDocument.QrPng);
            Assert.Null(reservedDocument.PdfBlobName);
        }

        private static object CreateIssuanceService(
            ApplicationDbContext context,
            object scopeResolver,
            object reservationService,
            object signatureService,
            object qrPayloadBuilder,
            object qrCodeImageGenerator,
            object pdfRenderer,
            object pdfBlobStore)
        {
            Type serviceType = GetRequiredRepositoryType(IssuanceServiceTypeName);
            ConstructorInfo constructor = serviceType.GetConstructor(new[]
            {
                typeof(ApplicationDbContext),
                typeof(IFiscalScopeResolver),
                typeof(IFiscalDocumentReservationService),
                typeof(IFiscalSignatureService),
                typeof(IFiscalQrPayloadBuilder),
                typeof(IFiscalQrCodeImageGenerator),
                typeof(FiscalReceiptViewModelFactory),
                typeof(FiscalReceiptPdfService),
            });
            Assert.NotNull(constructor);
            var pdfService = new FiscalReceiptPdfService(
                Assert.IsAssignableFrom<IFiscalReceiptPdfRenderer>(pdfRenderer),
                Assert.IsAssignableFrom<IFiscalReceiptPdfBlobStore>(pdfBlobStore));

            return constructor.Invoke(new object[]
            {
                context,
                scopeResolver,
                reservationService,
                signatureService,
                qrPayloadBuilder,
                qrCodeImageGenerator,
                new FiscalReceiptViewModelFactory(),
                pdfService,
            });
        }

        private static TestPdfBlobStore CreatePdfBlobStore(out object proxy)
        {
            proxy = DispatchProxy.Create(typeof(IFiscalReceiptPdfBlobStore), typeof(TestPdfBlobStore));
            return Assert.IsAssignableFrom<TestPdfBlobStore>(proxy);
        }

        private static TestPdfRenderer CreatePdfRenderer(out object proxy)
        {
            proxy = DispatchProxy.Create(typeof(IFiscalReceiptPdfRenderer), typeof(TestPdfRenderer));
            return Assert.IsAssignableFrom<TestPdfRenderer>(proxy);
        }

        private static TestQrCodeImageGenerator CreateQrCodeImageGenerator(out object proxy)
        {
            proxy = DispatchProxy.Create(typeof(IFiscalQrCodeImageGenerator), typeof(TestQrCodeImageGenerator));
            return Assert.IsAssignableFrom<TestQrCodeImageGenerator>(proxy);
        }

        private static TestQrPayloadBuilder CreateQrPayloadBuilder(out object proxy)
        {
            proxy = DispatchProxy.Create(typeof(IFiscalQrPayloadBuilder), typeof(TestQrPayloadBuilder));
            return Assert.IsAssignableFrom<TestQrPayloadBuilder>(proxy);
        }

        private static TestReservationService CreateReservationService(
            Func<FiscalDocumentReservationRequest, FiscalDocument> reserve,
            out object proxy)
        {
            proxy = DispatchProxy.Create(typeof(IFiscalDocumentReservationService), typeof(TestReservationService));
            TestReservationService testService = Assert.IsAssignableFrom<TestReservationService>(proxy);
            testService.Reserve = reserve;
            return testService;
        }

        private static TestScopeResolver CreateScopeResolver(FiscalScope fiscalScope, out object proxy)
        {
            proxy = DispatchProxy.Create(typeof(IFiscalScopeResolver), typeof(TestScopeResolver));
            TestScopeResolver testResolver = Assert.IsAssignableFrom<TestScopeResolver>(proxy);
            testResolver.FiscalScope = fiscalScope;
            return testResolver;
        }

        private static TestSignatureService CreateSignatureService(out object proxy)
        {
            proxy = DispatchProxy.Create(typeof(IFiscalSignatureService), typeof(TestSignatureService));
            return Assert.IsAssignableFrom<TestSignatureService>(proxy);
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
                Name = "Fiscal tenant",
            };
        }

        private static FiscalScope CreateFiscalScope()
        {
            var profile = new FiscalIssuerProfile
            {
                Id = 12,
                ScopeKind = FiscalScopeKind.Tenant,
                IsActive = true,
                LegalName = "Emitente fiscal",
                LegalAddress = "Rua Fiscal, 1",
                TaxRegistrationNumber = "501234567",
                SoftwareCertificateNumber = "1234",
                SigningKeyIdentifier = "production-key",
                SigningKeyVersion = "3",
            };
            var series = new FiscalDocumentSeries
            {
                Id = 17,
                FiscalIssuerProfileId = profile.Id,
                FiscalIssuerProfile = profile,
                SeriesCode = "RC 2026",
                SaftDocumentType = "RC",
                AtcudValidationCode = "VALIDATION",
                Status = FiscalSeriesStatus.Active,
                ProcessingMethod = FiscalProcessingMethod.CertifiedSoftware,
            };
            return new FiscalScope
            {
                TenantId = 7,
                InvoicingStrategy = InvoicingStrategy.SingleInvoiceTable,
                FiscalIssuerProfile = profile,
                FiscalDocumentSeries = series,
            };
        }

        private static FiscalDocument CreateReservedDocument(int donationId, FiscalScope fiscalScope)
        {
            return new FiscalDocument
            {
                DonationId = donationId,
                SeriesId = fiscalScope.FiscalDocumentSeries.Id,
                FiscalIssuerProfileId = fiscalScope.FiscalIssuerProfile.Id,
                InvoicingStrategy = fiscalScope.InvoicingStrategy,
                Kind = FiscalDocumentKind.Original,
                Status = FiscalDocumentStatus.Draft,
                Sequence = 42,
                DocumentNumber = "RC 2026/42",
                Atcud = "VALIDATION-42",
                IssuedAtUtc = new DateTimeOffset(2026, 9, 19, 10, 30, 0, TimeSpan.Zero),
                InvoiceDate = new DateOnly(2026, 9, 19),
                SystemEntryDate = new DateTimeOffset(2026, 9, 19, 10, 30, 0, TimeSpan.Zero),
                SigningKeyIdentifier = "production-key",
                SigningKeyVersion = "3",
                SigningAlgorithm = "RSA",
                SoftwareCertificateNumber = "1234",
                IssuerLegalName = "Emitente fiscal",
                IssuerTaxRegistrationNumber = "501234567",
                IssuerLegalAddress = "Rua Fiscal, 1",
                DonorName = "Doador histórico",
                DonorTaxRegistrationNumber = "123456789",
                NetTotal = 20.00m,
                TaxTotal = 0.00m,
                GrossTotal = 20.00m,
                Hash = string.Empty,
                HashControl = string.Empty,
                QrPayload = string.Empty,
                QrPng = Array.Empty<byte>(),
                PdfTemplateVersion = "fiscal-v1",
                Lines = new List<FiscalDocumentLine>
                {
                    new FiscalDocumentLine
                    {
                        LineNumber = 1,
                        ProductCode = "DONATION",
                        Description = "Donativo",
                        Quantity = 1.00m,
                        UnitPrice = 20.00m,
                        TaxRate = 0.00m,
                        NetTotal = 20.00m,
                        TaxTotal = 0.00m,
                        GrossTotal = 20.00m,
                    },
                },
            };
        }

        private static Donation PersistPaidDonation(ApplicationDbContext context, string paymentIdempotencyKey)
        {
            var payment = new CreditCardPayment
            {
                IdempotencyKey = paymentIdempotencyKey,
                Created = new DateTime(2026, 9, 19, 10, 0, 0, DateTimeKind.Utc),
                Completed = new DateTime(2026, 9, 19, 10, 30, 0, DateTimeKind.Utc),
                Status = "paid",
            };
            var donation = new Donation
            {
                PublicId = Guid.NewGuid(),
                DonationAmount = 20.00d,
                DonationDate = new DateTime(2026, 9, 19, 9, 0, 0, DateTimeKind.Utc),
                PaymentStatus = PaymentStatus.Payed,
                Nif = "123456789",
                ConfirmedPayment = payment,
            };
            context.Donations.Add(donation);
            context.SaveChanges();
            return donation;
        }

        private static FiscalDocument PersistReservedDocument(
            ApplicationDbContext context,
            FiscalDocument document,
            FiscalDocumentReservationRequest request)
        {
            document.IdempotencyKey = request.IdempotencyKey;
            context.FiscalDocuments.Add(document);
            context.SaveChanges();
            return document;
        }

        private static Type GetRequiredRepositoryType(string typeName)
        {
            return Assert.IsAssignableFrom<Type>(typeof(IFiscalScopeResolver).Assembly.GetType(typeName));
        }

        private static FiscalDocument IssueOriginal(object service, Tenant tenant, Donation donation)
        {
            MethodInfo issue = service.GetType().GetMethod("IssueOriginal", new[] { typeof(Tenant), typeof(Donation) });
            Assert.NotNull(issue);
            try
            {
                return Assert.IsAssignableFrom<FiscalDocument>(issue.Invoke(service, new object[] { tenant, donation }));
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }

        private class TestScopeResolver : DispatchProxy
        {
            public ICollection<int> DonationIds { get; } = new List<int>();

            public FiscalScope FiscalScope { get; set; }

            public ICollection<int> TenantIds { get; } = new List<int>();

            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                if (targetMethod.Name != "Resolve")
                {
                    throw new NotSupportedException($"Unexpected fiscal-scope method: {targetMethod.Name}.");
                }

                Tenant tenant = Assert.IsType<Tenant>(args[0]);
                Donation donation = Assert.IsType<Donation>(args[1]);
                this.TenantIds.Add(tenant.Id);
                this.DonationIds.Add(donation.Id);
                return this.FiscalScope;
            }
        }

        private class TestReservationService : DispatchProxy
        {
            public Func<FiscalDocumentReservationRequest, FiscalDocument> Reserve { get; set; }

            public ICollection<FiscalDocumentReservationRequest> Requests { get; } = new List<FiscalDocumentReservationRequest>();

            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                if (targetMethod.Name != "ReserveOriginal")
                {
                    throw new NotSupportedException($"Unexpected fiscal-reservation method: {targetMethod.Name}.");
                }

                FiscalDocumentReservationRequest request = Assert.IsType<FiscalDocumentReservationRequest>(Assert.Single(args));
                this.Requests.Add(request);
                return this.Reserve(request);
            }
        }

        private class TestSignatureService : DispatchProxy
        {
            public Exception ExceptionToThrow { get; set; }

            public ICollection<FiscalSignatureRequest> Requests { get; } = new List<FiscalSignatureRequest>();

            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                if (targetMethod.Name != "Sign")
                {
                    throw new NotSupportedException($"Unexpected fiscal-signature method: {targetMethod.Name}.");
                }

                FiscalSignatureRequest request = Assert.IsType<FiscalSignatureRequest>(Assert.Single(args));
                this.Requests.Add(request);
                if (this.ExceptionToThrow != null)
                {
                    ExceptionDispatchInfo.Capture(this.ExceptionToThrow).Throw();
                }

                return new FiscalSignatureResult
                {
                    CanonicalPayload = "2026-09-19;2026-09-19T10:30:00;RC 2026/42;20.00;previous-fiscal-signature",
                    PreviousHash = "previous-fiscal-signature",
                    Hash = "fiscal-signature",
                    HashControl = "3",
                    SigningKeyIdentifier = request.SigningKeyIdentifier,
                    SigningKeyVersion = request.SigningKeyVersion,
                    SigningAlgorithm = request.SigningAlgorithm,
                };
            }
        }

        private class TestQrPayloadBuilder : DispatchProxy
        {
            public ICollection<FiscalQrPayloadSnapshot> Snapshots { get; } = new List<FiscalQrPayloadSnapshot>();

            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                if (targetMethod.Name != "Build")
                {
                    throw new NotSupportedException($"Unexpected fiscal-QR payload method: {targetMethod.Name}.");
                }

                FiscalQrPayloadSnapshot snapshot = Assert.IsType<FiscalQrPayloadSnapshot>(Assert.Single(args));
                this.Snapshots.Add(snapshot);
                return "A:501234567*H:VALIDATION-42";
            }
        }

        private class TestQrCodeImageGenerator : DispatchProxy
        {
            public static readonly byte[] PngBytes = { 137, 80, 78, 71 };

            public ICollection<string> Payloads { get; } = new List<string>();

            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                if (targetMethod.Name != "GeneratePng")
                {
                    throw new NotSupportedException($"Unexpected fiscal-QR image method: {targetMethod.Name}.");
                }

                string payload = Assert.IsType<string>(Assert.Single(args));
                this.Payloads.Add(payload);
                return PngBytes;
            }
        }

        private class TestPdfRenderer : DispatchProxy
        {
            public ICollection<FiscalReceiptViewModel> ViewModels { get; } = new List<FiscalReceiptViewModel>();

            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                if (targetMethod.Name != "Render")
                {
                    throw new NotSupportedException($"Unexpected fiscal-PDF renderer method: {targetMethod.Name}.");
                }

                FiscalReceiptViewModel viewModel = Assert.IsType<FiscalReceiptViewModel>(Assert.Single(args));
                this.ViewModels.Add(viewModel);
                return new byte[] { 37, 80, 68, 70, 45, 49, 46, 55 };
            }
        }

        private class TestPdfBlobStore : DispatchProxy
        {
            private readonly IDictionary<Guid, byte[]> blobs = new Dictionary<Guid, byte[]>();

            public ICollection<Guid> SavedBlobs { get; } = new List<Guid>();

            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                if (targetMethod.Name == "Save")
                {
                    Guid blobName = Assert.IsType<Guid>(args[0]);
                    byte[] content = Assert.IsType<byte[]>(args[1]);
                    this.blobs.Add(blobName, content);
                    this.SavedBlobs.Add(blobName);
                    return null;
                }

                if (targetMethod.Name == "Load")
                {
                    Guid blobName = Assert.IsType<Guid>(Assert.Single(args));
                    return this.blobs.TryGetValue(blobName, out byte[] content) ? content : null;
                }

                throw new NotSupportedException($"Unexpected fiscal-PDF blob method: {targetMethod.Name}.");
            }
        }
    }
}
