// -----------------------------------------------------------------------
// <copyright file="FiscalDocumentCancellationCorrectionTddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    using System.Security.Claims;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using Xunit;

    /// <summary>
    /// Defines TDD contracts for auditable fiscal-document cancellation and correction.
    /// </summary>
    public class FiscalDocumentCancellationCorrectionTddTests
    {
        private const string CancellationRequestTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalDocumentCancellationRequest";
        private const string CancellationServiceInterfaceTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.IFiscalDocumentCancellationService";
        private const string CancellationServiceTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalDocumentCancellationService";
        private const string CorrectionRequestTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalDocumentCorrectionRequest";
        private const string CorrectionServiceInterfaceTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.IFiscalDocumentCorrectionService";
        private const string CorrectionServiceTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalDocumentCorrectionService";

        /// <summary>
        /// Cancellation receives a document id and statutory motive only. Its tenant and actor
        /// must come from trusted HTTP context, rather than caller-controlled request fields.
        /// </summary>
        [Fact]
        public void FiscalDocumentCancellationService_ExposesTrustedActorCancellationContract()
        {
            Type requestType = GetRequiredRepositoryType(CancellationRequestTypeName);
            Type serviceInterfaceType = GetRequiredRepositoryType(CancellationServiceInterfaceTypeName);
            Type serviceType = GetRequiredRepositoryType(CancellationServiceTypeName);

            AssertRequiredProperties(requestType, "FiscalDocumentId", "Motive");
            Assert.Equal(typeof(int), requestType.GetProperty("FiscalDocumentId")?.PropertyType);
            Assert.Equal(typeof(string), requestType.GetProperty("Motive")?.PropertyType);
            Assert.Null(requestType.GetProperty("ActorId"));
            Assert.Null(requestType.GetProperty("Tenant"));
            Assert.Single(
                serviceInterfaceType.GetMethods(),
                method => method.Name == "Cancel" &&
                    method.ReturnType == typeof(FiscalDocument) &&
                    method.GetParameters().Select(parameter => parameter.ParameterType)
                        .SequenceEqual(new[] { requestType }));
            Assert.True(serviceInterfaceType.IsAssignableFrom(serviceType));
            Assert.NotNull(serviceType.GetConstructor(new[] { typeof(ApplicationDbContext), typeof(IHttpContextAccessor) }));
        }

        /// <summary>
        /// Only an issued original can be cancelled. The original fiscal identity and immutable
        /// render/signature evidence stay byte-for-byte intact; cancellation appends one actor
        /// and motive-bearing event and changes only the lifecycle state.
        /// </summary>
        [Fact]
        public void Cancel_IssuedOriginalAppendsTrustedActorEventWithoutChangingFiscalEvidence()
        {
            using var context = CreateContext();
            FiscalDocument original = PersistIssuedOriginal(context, donationId: 501, fiscalIssuerProfileId: 10);
            IHttpContextAccessor httpContextAccessor = CreateTrustedHttpContextAccessor(tenantId: 7, actorId: "authorised-operator");
            object service = CreateCancellationService(context, httpContextAccessor);
            object request = CreateRequest(CancellationRequestTypeName, original.Id, "Customer cancellation request");
            string documentNumber = original.DocumentNumber;
            string atcud = original.Atcud;
            string hash = original.Hash;
            string hashControl = original.HashControl;
            string qrPayload = original.QrPayload;
            byte[] qrPng = original.QrPng.ToArray();
            Guid? pdfBlobName = original.PdfBlobName;
            string pdfChecksum = original.PdfChecksum;
            List<FiscalDocumentLine> originalLines = context.FiscalDocumentLines
                .Where(line => line.FiscalDocumentId == original.Id)
                .OrderBy(line => line.LineNumber)
                .ToList();

            FiscalDocument cancelled = Cancel(service, request);

            Assert.Equal(original.Id, cancelled.Id);
            Assert.Equal(FiscalDocumentStatus.Cancelled, cancelled.Status);
            Assert.Equal(documentNumber, cancelled.DocumentNumber);
            Assert.Equal(atcud, cancelled.Atcud);
            Assert.Equal(hash, cancelled.Hash);
            Assert.Equal(hashControl, cancelled.HashControl);
            Assert.Equal(qrPayload, cancelled.QrPayload);
            Assert.Equal(qrPng, cancelled.QrPng);
            Assert.Equal(pdfBlobName, cancelled.PdfBlobName);
            Assert.Equal(pdfChecksum, cancelled.PdfChecksum);
            Assert.Equal(originalLines.Select(line => line.LineNumber), context.FiscalDocumentLines
                .Where(line => line.FiscalDocumentId == original.Id)
                .OrderBy(line => line.LineNumber)
                .Select(line => line.LineNumber));
            FiscalDocumentEvent cancellationEvent = Assert.Single(context.FiscalDocumentEvents);
            Assert.Equal(original.Id, cancellationEvent.FiscalDocumentId);
            Assert.Equal("Cancelled", cancellationEvent.EventType);
            Assert.Equal("authorised-operator", cancellationEvent.ActorId);
            Assert.Equal("Customer cancellation request", cancellationEvent.Reason);
        }

        /// <summary>
        /// Draft, already-cancelled or non-original documents are not cancellable. A missing
        /// authenticated actor or motive fails before a status mutation or audit-event write.
        /// </summary>
        [Theory]
        [InlineData(FiscalDocumentStatus.Draft, FiscalDocumentKind.Original, true, "Valid motive")]
        [InlineData(FiscalDocumentStatus.Cancelled, FiscalDocumentKind.Original, true, "Valid motive")]
        [InlineData(FiscalDocumentStatus.Issued, FiscalDocumentKind.Correction, true, "Valid motive")]
        [InlineData(FiscalDocumentStatus.Issued, FiscalDocumentKind.Original, false, "Valid motive")]
        [InlineData(FiscalDocumentStatus.Issued, FiscalDocumentKind.Original, true, " ")]
        public void Cancel_RejectsInvalidStateOrUntrustedActorOrMotiveWithoutWritingEvent(
            FiscalDocumentStatus status,
            FiscalDocumentKind kind,
            bool hasActor,
            string motive)
        {
            using var context = CreateContext();
            FiscalDocument document = PersistIssuedOriginal(context, donationId: 502, fiscalIssuerProfileId: 10);
            FiscalDocument original = kind == FiscalDocumentKind.Original
                ? null
                : PersistIssuedOriginal(context, donationId: 502, fiscalIssuerProfileId: 10);
            document.Status = status;
            document.Kind = kind;
            if (kind != FiscalDocumentKind.Original)
            {
                document.OriginalFiscalDocumentId = original.Id;
            }

            context.SaveChanges();
            IHttpContextAccessor httpContextAccessor = CreateTrustedHttpContextAccessor(
                tenantId: 7,
                actorId: hasActor ? "authorised-operator" : null);
            object service = CreateCancellationService(context, httpContextAccessor);
            object request = CreateRequest(CancellationRequestTypeName, document.Id, motive);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => Cancel(service, request));

            Assert.False(string.IsNullOrWhiteSpace(exception.Message));
            Assert.Equal(status, context.FiscalDocuments.Single(item => item.Id == document.Id).Status);
            Assert.Empty(context.FiscalDocumentEvents);
        }

        /// <summary>
        /// A correction has no caller-controlled tenant, issuer, series or donation scope. It
        /// must derive all boundaries from the persisted original and trusted current tenant,
        /// then run the distinct reservation, signature, QR and immutable-PDF pipeline to make
        /// a separately identified linked document. It is not a mutation of the original.
        /// </summary>
        [Fact]
        public void FiscalDocumentCorrectionService_ExposesInternallyScopedNewFiscalDocumentContract()
        {
            Type requestType = GetRequiredRepositoryType(CorrectionRequestTypeName);
            Type serviceInterfaceType = GetRequiredRepositoryType(CorrectionServiceInterfaceTypeName);
            Type serviceType = GetRequiredRepositoryType(CorrectionServiceTypeName);

            AssertRequiredProperties(requestType, "OriginalFiscalDocumentId", "IdempotencyKey", "Motive");
            Assert.Equal(typeof(int), requestType.GetProperty("OriginalFiscalDocumentId")?.PropertyType);
            Assert.Equal(typeof(string), requestType.GetProperty("IdempotencyKey")?.PropertyType);
            Assert.Equal(typeof(string), requestType.GetProperty("Motive")?.PropertyType);
            Assert.Null(requestType.GetProperty("Tenant"));
            Assert.Null(requestType.GetProperty("DonationId"));
            Assert.Null(requestType.GetProperty("SeriesId"));
            Assert.Null(requestType.GetProperty("FiscalScope"));
            Assert.Single(
                serviceInterfaceType.GetMethods(),
                method => method.Name == "CreateCorrection" &&
                    method.ReturnType == typeof(FiscalDocument) &&
                    method.GetParameters().Select(parameter => parameter.ParameterType)
                        .SequenceEqual(new[] { requestType }));
            Assert.True(serviceInterfaceType.IsAssignableFrom(serviceType));
            Assert.NotNull(serviceType.GetConstructor(new[]
            {
                typeof(ApplicationDbContext),
                typeof(IHttpContextAccessor),
                typeof(IFiscalScopeResolver),
                typeof(IFiscalDocumentReservationService),
                typeof(IFiscalSignatureService),
                typeof(IFiscalQrPayloadBuilder),
                typeof(IFiscalQrCodeImageGenerator),
                typeof(FiscalReceiptViewModelFactory),
                typeof(FiscalReceiptPdfService),
            }));
        }

        /// <summary>
        /// The correction service must validate a trusted tenant against the original's captured
        /// tenant scope and accept at most one correction for that original. These rules prevent
        /// a caller from crossing tenant/food-bank scope or repeatedly reversing a document.
        /// </summary>
        [Fact]
        public void CorrectionContract_DerivesScopeAndRejectsCrossScopeOrRepeatCorrection()
        {
            Type requestType = GetRequiredRepositoryType(CorrectionRequestTypeName);
            Type serviceType = GetRequiredRepositoryType(CorrectionServiceTypeName);

            Assert.Null(requestType.GetProperty("Tenant"));
            Assert.Null(requestType.GetProperty("FiscalIssuerProfileId"));
            Assert.Null(requestType.GetProperty("FoodBankId"));
            Assert.Contains(
                serviceType.GetConstructors().SelectMany(constructor => constructor.GetParameters()),
                parameter => parameter.ParameterType == typeof(IHttpContextAccessor));
            Assert.Contains(
                serviceType.GetConstructors().SelectMany(constructor => constructor.GetParameters()),
                parameter => parameter.ParameterType == typeof(IFiscalScopeResolver));
            Assert.Contains(
                serviceType.GetConstructors().SelectMany(constructor => constructor.GetParameters()),
                parameter => parameter.ParameterType == typeof(IFiscalDocumentReservationService));
        }

        private static void AssertRequiredProperties(Type type, params string[] propertyNames)
        {
            foreach (string propertyName in propertyNames)
            {
                Assert.NotNull(type.GetProperty(propertyName));
            }
        }

        private static FiscalDocument Cancel(object service, object request)
        {
            Type requestType = GetRequiredRepositoryType(CancellationRequestTypeName);
            MethodInfo method = service.GetType().GetMethod("Cancel", new[] { requestType });
            Assert.NotNull(method);

            try
            {
                return Assert.IsType<FiscalDocument>(method.Invoke(service, new[] { request }));
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }

        private static object CreateCancellationService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            Type serviceType = GetRequiredRepositoryType(CancellationServiceTypeName);
            ConstructorInfo constructor = serviceType.GetConstructor(new[] { typeof(ApplicationDbContext), typeof(IHttpContextAccessor) });
            Assert.NotNull(constructor);
            return constructor.Invoke(new object[] { context, httpContextAccessor });
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

        private static object CreateRequest(string typeName, int fiscalDocumentId, string motive)
        {
            Type requestType = GetRequiredRepositoryType(typeName);
            object request = Activator.CreateInstance(requestType);
            SetRequiredProperty(request, typeName == CancellationRequestTypeName ? "FiscalDocumentId" : "OriginalFiscalDocumentId", fiscalDocumentId);
            SetRequiredProperty(request, "Motive", motive);
            if (typeName == CorrectionRequestTypeName)
            {
                SetRequiredProperty(request, "IdempotencyKey", $"correction-{fiscalDocumentId}");
            }

            return request;
        }

        private static IHttpContextAccessor CreateTrustedHttpContextAccessor(int tenantId, string actorId)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.SetTenant(new Tenant
            {
                Id = tenantId,
                InvoicingStrategy = InvoicingStrategy.SingleInvoiceTable,
                Name = "Fiscal tenant",
            });
            if (!string.IsNullOrWhiteSpace(actorId))
            {
                httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, actorId) },
                    "FiscalTest"));
            }

            return new HttpContextAccessor { HttpContext = httpContext };
        }

        private static Type GetRequiredRepositoryType(string typeName)
        {
            return Assert.IsAssignableFrom<Type>(typeof(IFiscalScopeResolver).Assembly.GetType(typeName));
        }

        private static FiscalDocument PersistIssuedOriginal(ApplicationDbContext context, int donationId, int fiscalIssuerProfileId)
        {
            int id = context.FiscalDocuments.Count() + 100;
            var document = new FiscalDocument
            {
                Id = id,
                DonationId = donationId,
                SeriesId = 10,
                FiscalIssuerProfileId = fiscalIssuerProfileId,
                InvoicingStrategy = InvoicingStrategy.SingleInvoiceTable,
                Kind = FiscalDocumentKind.Original,
                Status = FiscalDocumentStatus.Issued,
                Sequence = id,
                DocumentNumber = $"RC 2026/{id}",
                Atcud = $"VALIDATION-{id}",
                IssuedAtUtc = new DateTimeOffset(2026, 9, 19, 10, 0, 0, TimeSpan.Zero),
                InvoiceDate = new DateOnly(2026, 9, 19),
                SystemEntryDate = new DateTimeOffset(2026, 9, 19, 10, 0, 0, TimeSpan.Zero),
                Hash = $"signature-{id}",
                HashControl = "3",
                SigningKeyIdentifier = "fiscal-key",
                SigningKeyVersion = "3",
                SigningAlgorithm = "RSA",
                QrPayload = $"A:501234567*H:VALIDATION-{id}",
                QrPng = new byte[] { 137, 80, 78, 71 },
                SoftwareCertificateNumber = "1234",
                IssuerLegalName = "Fiscal issuer snapshot",
                IssuerTaxRegistrationNumber = "501234567",
                IssuerLegalAddress = "Rua fiscal, 1",
                TaxRule = "NOR",
                NetTotal = 10m,
                TaxTotal = 2.30m,
                GrossTotal = 12.30m,
                PdfBlobName = Guid.NewGuid(),
                PdfChecksum = "immutable-pdf-checksum",
                PdfTemplateVersion = "fiscal-v1",
            };
            context.FiscalDocuments.Add(document);
            context.SaveChanges();
            context.FiscalDocumentLines.Add(new FiscalDocumentLine
            {
                FiscalDocumentId = document.Id,
                LineNumber = 1,
                ProductCode = "DONATION",
                Description = "Fiscal donation line",
                Quantity = 1m,
                UnitPrice = 10m,
                TaxRate = 23m,
                NetTotal = 10m,
                TaxTotal = 2.30m,
                GrossTotal = 12.30m,
            });
            context.SaveChanges();
            return document;
        }

        private static void SetRequiredProperty(object instance, string propertyName, object value)
        {
            PropertyInfo property = instance.GetType().GetProperty(propertyName);
            Assert.NotNull(property);
            property.SetValue(instance, value);
        }
    }
}
