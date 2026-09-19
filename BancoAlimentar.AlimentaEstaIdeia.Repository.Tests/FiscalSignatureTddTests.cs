// -----------------------------------------------------------------------
// <copyright file="FiscalSignatureTddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.ExceptionServices;
    using System.Security.Cryptography;
    using System.Text;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata;
    using Xunit;

    /// <summary>
    /// Defines TDD contracts for the Portuguese SAF-T sales-invoice signature chain.
    /// </summary>
    public class FiscalSignatureTddTests
    {
        private const string FiscalSignatureServiceTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalSignatureService";
        private const string FiscalSignatureServiceInterfaceTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.IFiscalSignatureService";
        private const string FiscalSigningKeyProviderInterfaceTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.IFiscalSigningKeyProvider";
        private const string FiscalSignatureRequestTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalSignatureRequest";
        private const string FiscalSigningKeyRequestTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalSigningKeyRequest";
        private const string FiscalSigningKeyResultTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalSigningKeyResult";
        private const string FiscalSignatureResultTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalSignatureResult";

        /// <summary>
        /// The service needs the fiscal database to discover the predecessor in the legally
        /// defined chain. The request carries a protected-key reference but never private key
        /// material.
        /// </summary>
        [Fact]
        public void FiscalSignatureService_ExposesPersistedSalesInvoiceSigningContract()
        {
            Type signatureRequestType = GetRequiredRepositoryType(FiscalSignatureRequestTypeName);
            Type signatureResultType = GetRequiredRepositoryType(FiscalSignatureResultTypeName);
            Type keyRequestType = GetRequiredRepositoryType(FiscalSigningKeyRequestTypeName);
            Type keyResultType = GetRequiredRepositoryType(FiscalSigningKeyResultTypeName);
            Type signatureServiceType = GetRequiredRepositoryType(FiscalSignatureServiceTypeName);
            Type signatureServiceInterfaceType = GetRequiredRepositoryType(FiscalSignatureServiceInterfaceTypeName);
            Type keyProviderInterfaceType = GetRequiredRepositoryType(FiscalSigningKeyProviderInterfaceTypeName);

            AssertRequiredProperties(
                signatureRequestType,
                "FiscalDocument",
                "SigningKeyIdentifier",
                "SigningKeyVersion",
                "SigningAlgorithm");
            AssertRequiredProperties(
                signatureResultType,
                "CanonicalPayload",
                "PreviousHash",
                "Hash",
                "HashControl",
                "SigningKeyIdentifier",
                "SigningKeyVersion",
                "SigningAlgorithm");
            AssertRequiredProperties(
                keyRequestType,
                "CanonicalPayload",
                "SigningKeyIdentifier",
                "SigningKeyVersion",
                "SigningAlgorithm");
            AssertRequiredProperties(keyResultType, "Hash", "HashControl");

            MethodInfo signDocument = Assert.Single(
                signatureServiceInterfaceType.GetMethods(),
                method => method.Name == "Sign" &&
                    method.GetParameters().Select(parameter => parameter.ParameterType)
                        .SequenceEqual(new[] { signatureRequestType }) &&
                    method.ReturnType == signatureResultType);
            MethodInfo signPayload = Assert.Single(
                keyProviderInterfaceType.GetMethods(),
                method => method.Name == "Sign" &&
                    method.GetParameters().Select(parameter => parameter.ParameterType)
                        .SequenceEqual(new[] { keyRequestType }) &&
                    method.ReturnType == keyResultType);

            Assert.NotNull(signatureServiceType.GetConstructor(
                new[] { typeof(ApplicationDbContext), keyProviderInterfaceType }));
            Assert.Equal("Sign", signDocument.Name);
            Assert.Equal("Sign", signPayload.Name);
        }

        /// <summary>
        /// Official test-vector source: Article 6(1) of Portaria n.º 363/2010,
        /// https://info.portaldasfinancas.gov.pt/pt/informacao_fiscal/legislacao/diplomas_legislativos/Documents/Portaria_363_2010.pdf.
        /// The exact message is InvoiceDate;SystemEntryDate;InvoiceNo;GrossTotal;PreviousHash.
        /// ATCUD is deliberately absent because it is not part of that statutory signature message.
        /// </summary>
        [Fact]
        public void Sign_UsesExactPortaria3632010PayloadAndBase64RsaSignature()
        {
            using var context = CreateContext();
            Type keyProviderType = GetRequiredRepositoryType(FiscalSigningKeyProviderInterfaceTypeName);
            TestFiscalSigningKeyProvider testProvider = CreateTestSigningKeyProvider(keyProviderType, out object provider);
            object signatureService = CreateSignatureService(context, provider);
            FiscalDocument document = CreateFiscalDocument(
                id: 42,
                seriesId: 7,
                sequence: 42,
                issuedAtUtc: new DateTimeOffset(2026, 9, 19, 10, 30, 0, TimeSpan.Zero),
                hash: string.Empty);
            SetRequiredProperty(document, "InvoiceDate", new DateOnly(2026, 9, 18));
            SetRequiredProperty(document, "SystemEntryDate", new DateTimeOffset(2026, 9, 19, 10, 30, 0, TimeSpan.Zero));

            object result = Sign(
                signatureService,
                CreateSignatureRequest(document, "key-reference", "1", "RSA"));

            const string expectedPayload = "2026-09-18;2026-09-19T10:30:00;RC 2026/42;25.50;";
            string signature = GetRequiredStringProperty(result, "Hash");

            Assert.Equal(expectedPayload, GetRequiredStringProperty(result, "CanonicalPayload"));
            Assert.Equal(expectedPayload, Assert.Single(testProvider.CanonicalPayloads));
            Assert.DoesNotContain(document.Atcud, expectedPayload, StringComparison.Ordinal);
            Assert.Equal("RSA", Assert.Single(testProvider.SigningAlgorithms));
            Assert.True(testProvider.Verify(expectedPayload, signature));
            Assert.Equal("1", GetRequiredStringProperty(result, "HashControl"));
            Assert.Equal("key-reference", GetRequiredStringProperty(result, "SigningKeyIdentifier"));
            Assert.Equal("1", GetRequiredStringProperty(result, "SigningKeyVersion"));
            Assert.Equal("RSA", GetRequiredStringProperty(result, "SigningAlgorithm"));
        }

        /// <summary>
        /// Article 6(1)(e) chains the preceding document of the same type and series. A
        /// caller-provided predecessor cannot replace the persisted document selected by those
        /// boundaries.
        /// </summary>
        [Fact]
        public void Sign_ResolvesPreviousHashFromLastSameSeriesAndFiscalYear()
        {
            using var context = CreateContext();
            Type keyProviderType = GetRequiredRepositoryType(FiscalSigningKeyProviderInterfaceTypeName);
            TestFiscalSigningKeyProvider testProvider = CreateTestSigningKeyProvider(keyProviderType, out object provider);
            object signatureService = CreateSignatureService(context, provider);
            FiscalDocument preceding = CreateFiscalDocument(
                id: 1,
                seriesId: 7,
                sequence: 41,
                issuedAtUtc: new DateTimeOffset(2026, 9, 18, 9, 0, 0, TimeSpan.Zero),
                hash: "same-series-2026-hash");
            FiscalDocument otherSeries = CreateFiscalDocument(
                id: 2,
                seriesId: 8,
                sequence: 99,
                issuedAtUtc: new DateTimeOffset(2026, 9, 18, 11, 0, 0, TimeSpan.Zero),
                hash: "other-series-hash");
            FiscalDocument previousYear = CreateFiscalDocument(
                id: 3,
                seriesId: 7,
                sequence: 98,
                issuedAtUtc: new DateTimeOffset(2025, 12, 31, 23, 59, 59, TimeSpan.Zero),
                hash: "previous-year-hash");
            FiscalDocument current = CreateFiscalDocument(
                id: 4,
                seriesId: 7,
                sequence: 42,
                issuedAtUtc: new DateTimeOffset(2026, 9, 19, 10, 30, 0, TimeSpan.Zero),
                hash: string.Empty);
            context.FiscalDocuments.AddRange(preceding, otherSeries, previousYear, current);
            context.SaveChanges();

            object result = Sign(
                signatureService,
                CreateSignatureRequest(current, "key-reference", "1", "RSA"));

            Assert.Equal("same-series-2026-hash", GetRequiredStringProperty(result, "PreviousHash"));
            Assert.EndsWith(";same-series-2026-hash", GetRequiredStringProperty(result, "CanonicalPayload"), StringComparison.Ordinal);
            Assert.Single(testProvider.CanonicalPayloads);
        }

        /// <summary>
        /// A series remains one chronological hash chain even at the fiscal-year boundary. The
        /// first document entered in a new year chains from the last signed document in the
        /// same series rather than resetting the predecessor.
        /// </summary>
        [Fact]
        public void Sign_UsesPreviousHashAcrossFiscalYearBoundaryForSameSeries()
        {
            using var context = CreateContext();
            Type keyProviderType = GetRequiredRepositoryType(FiscalSigningKeyProviderInterfaceTypeName);
            TestFiscalSigningKeyProvider testProvider = CreateTestSigningKeyProvider(keyProviderType, out object provider);
            object signatureService = CreateSignatureService(context, provider);
            FiscalDocument priorYear = CreateFiscalDocument(
                id: 1,
                seriesId: 7,
                sequence: 42,
                issuedAtUtc: new DateTimeOffset(2026, 12, 31, 23, 59, 59, TimeSpan.Zero),
                hash: "2026-hash");
            FiscalDocument firstNewYear = CreateFiscalDocument(
                id: 2,
                seriesId: 7,
                sequence: 1,
                issuedAtUtc: new DateTimeOffset(2027, 1, 1, 0, 0, 1, TimeSpan.Zero),
                hash: string.Empty);
            context.FiscalDocuments.AddRange(priorYear, firstNewYear);
            context.SaveChanges();

            object result = Sign(
                signatureService,
                CreateSignatureRequest(firstNewYear, "key-reference", "1", "RSA"));

            Assert.Equal("2026-hash", GetRequiredStringProperty(result, "PreviousHash"));
            Assert.EndsWith(";2026-hash", GetRequiredStringProperty(result, "CanonicalPayload"), StringComparison.Ordinal);
            Assert.Single(testProvider.CanonicalPayloads);
        }

        /// <summary>
        /// The signing-key identity, version and algorithm are immutable signature evidence,
        /// alongside the Base64 signature and the sequential HashControl key version.
        /// </summary>
        [Fact]
        public void FiscalDocument_ModelPersistsCompleteSigningKeySnapshot()
        {
            using var context = CreateContext();
            IEntityType fiscalDocument = Assert.IsAssignableFrom<IEntityType>(
                context.Model.FindEntityType(typeof(FiscalDocument)));

            AssertRequiredPersistedStringProperty(fiscalDocument, "Hash");
            AssertRequiredPersistedStringProperty(fiscalDocument, "HashControl");
            AssertRequiredPersistedStringProperty(fiscalDocument, "SigningKeyIdentifier");
            AssertRequiredPersistedStringProperty(fiscalDocument, "SigningKeyVersion");
            AssertRequiredPersistedStringProperty(fiscalDocument, "SigningAlgorithm");
            AssertRequiredPersistedProperty(fiscalDocument, "InvoiceDate", typeof(DateOnly));
            AssertRequiredPersistedProperty(fiscalDocument, "SystemEntryDate", typeof(DateTimeOffset));
            Assert.True(Assert.IsAssignableFrom<IProperty>(fiscalDocument.FindProperty("PreviousHash")).IsNullable);
        }

        /// <summary>
        /// The application model must expose the two independent SAF-T date snapshots used in
        /// the statutory signature message: the invoice calendar date and the system-entry
        /// timestamp. A single issuance instant cannot express both snapshots reliably.
        /// </summary>
        [Fact]
        public void FiscalDocument_ExposesSeparateInvoiceDateAndSystemEntryDateSnapshots()
        {
            Assert.Equal(typeof(DateOnly), typeof(FiscalDocument).GetProperty("InvoiceDate")?.PropertyType);
            Assert.Equal(typeof(DateTimeOffset), typeof(FiscalDocument).GetProperty("SystemEntryDate")?.PropertyType);
        }

        /// <summary>
        /// Payment idempotency is introduced additively. The model has a nullable string
        /// property and an incremental application migration adds it to Payments, preserving
        /// historical payment records rather than rewriting the initial schema.
        /// </summary>
        [Fact]
        public void Payments_IdempotencyKeyIsModeledAndAddedByIncrementalMigration()
        {
            using var context = CreateContext();
            IEntityType payments = Assert.IsAssignableFrom<IEntityType>(
                context.Model.FindEntityType(typeof(BasePayment)));
            IProperty idempotencyKey = Assert.IsAssignableFrom<IProperty>(payments.FindProperty("IdempotencyKey"));
            string migrationsDirectory = Path.Combine(GetRepositoryRoot(), "BancoAlimentar.AlimentaEstaIdeia.Web", "Migrations");
            string[] migrationFiles = Directory.GetFiles(migrationsDirectory, "*.cs")
                .Where(path => !path.EndsWith(".Designer.cs", StringComparison.Ordinal) &&
                    !path.EndsWith("ModelSnapshot.cs", StringComparison.Ordinal))
                .ToArray();

            Assert.Equal(typeof(string), idempotencyKey.ClrType);
            Assert.True(idempotencyKey.IsNullable);
            Assert.Contains(
                migrationFiles,
                path => !Path.GetFileName(path).Contains("Initial", StringComparison.OrdinalIgnoreCase) &&
                    File.ReadAllText(path).Contains("AddColumn<string>", StringComparison.Ordinal) &&
                    File.ReadAllText(path).Contains("name: \"IdempotencyKey\"", StringComparison.Ordinal) &&
                    File.ReadAllText(path).Contains("table: \"Payments\"", StringComparison.Ordinal));
        }

        /// <summary>
        /// A blank key reference fails before signing and a provider error never yields a
        /// partial signature result. Both paths are fail-closed.
        /// </summary>
        [Fact]
        public void Sign_FailsClosedForMissingKeyAndProviderException()
        {
            using var context = CreateContext();
            Type keyProviderType = GetRequiredRepositoryType(FiscalSigningKeyProviderInterfaceTypeName);
            TestFiscalSigningKeyProvider testProvider = CreateTestSigningKeyProvider(keyProviderType, out object provider);
            object signatureService = CreateSignatureService(context, provider);
            FiscalDocument document = CreateFiscalDocument(
                id: 42,
                seriesId: 7,
                sequence: 42,
                issuedAtUtc: new DateTimeOffset(2026, 9, 19, 10, 30, 0, TimeSpan.Zero),
                hash: string.Empty);

            InvalidOperationException missingKeyException = Assert.Throws<InvalidOperationException>(
                () => Sign(signatureService, CreateSignatureRequest(document, "   ", "1", "RSA")));

            Assert.Contains("key", missingKeyException.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(testProvider.CanonicalPayloads);

            testProvider.ExceptionToThrow = new CryptographicException("Protected signing key unavailable.");

            CryptographicException providerException = Assert.Throws<CryptographicException>(
                () => Sign(signatureService, CreateSignatureRequest(document, "key-reference", "1", "RSA")));

            Assert.Contains("unavailable", providerException.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Single(testProvider.CanonicalPayloads);
            Assert.Equal(string.Empty, document.Hash);
            Assert.Equal(string.Empty, document.HashControl);
        }

        /// <summary>
        /// HashControl is the sequential private-key version printed and exported with the
        /// signature. Values other than a positive decimal sequence are not legal evidence.
        /// </summary>
        /// <param name="hashControl">The malformed provider HashControl value.</param>
        [Theory]
        [InlineData("")]
        [InlineData("0")]
        [InlineData("v1")]
        [InlineData("-1")]
        public void Sign_RejectsNonSequentialHashControl(string hashControl)
        {
            using var context = CreateContext();
            Type keyProviderType = GetRequiredRepositoryType(FiscalSigningKeyProviderInterfaceTypeName);
            TestFiscalSigningKeyProvider testProvider = CreateTestSigningKeyProvider(keyProviderType, out object provider);
            testProvider.HashControl = hashControl;
            object signatureService = CreateSignatureService(context, provider);
            FiscalDocument document = CreateFiscalDocument(
                id: 42,
                seriesId: 7,
                sequence: 42,
                issuedAtUtc: new DateTimeOffset(2026, 9, 19, 10, 30, 0, TimeSpan.Zero),
                hash: string.Empty);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => Sign(signatureService, CreateSignatureRequest(document, "key-reference", "1", "RSA")));

            Assert.Contains("control", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Single(testProvider.CanonicalPayloads);
            Assert.Equal(string.Empty, document.Hash);
            Assert.Equal(string.Empty, document.HashControl);
        }

        private static void AssertRequiredProperties(Type type, params string[] propertyNames)
        {
            foreach (string propertyName in propertyNames)
            {
                Assert.NotNull(type.GetProperty(propertyName));
            }
        }

        private static void AssertRequiredPersistedStringProperty(IEntityType entityType, string propertyName)
        {
            IProperty property = Assert.IsAssignableFrom<IProperty>(entityType.FindProperty(propertyName));
            Assert.Equal(typeof(string), property.ClrType);
            Assert.False(property.IsNullable);
        }

        private static void AssertRequiredPersistedProperty(IEntityType entityType, string propertyName, Type propertyType)
        {
            IProperty property = Assert.IsAssignableFrom<IProperty>(entityType.FindProperty(propertyName));
            Assert.Equal(propertyType, property.ClrType);
            Assert.False(property.IsNullable);
        }

        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private static TestFiscalSigningKeyProvider CreateTestSigningKeyProvider(Type keyProviderType, out object provider)
        {
            provider = DispatchProxy.Create(keyProviderType, typeof(TestFiscalSigningKeyProvider));
            return Assert.IsAssignableFrom<TestFiscalSigningKeyProvider>(provider);
        }

        private static object CreateSignatureService(ApplicationDbContext context, object provider)
        {
            Type serviceType = GetRequiredRepositoryType(FiscalSignatureServiceTypeName);
            Type providerType = GetRequiredRepositoryType(FiscalSigningKeyProviderInterfaceTypeName);
            ConstructorInfo constructor = serviceType.GetConstructor(new[] { typeof(ApplicationDbContext), providerType });
            Assert.NotNull(constructor);
            return constructor.Invoke(new[] { context, provider });
        }

        private static object CreateSignatureRequest(
            FiscalDocument fiscalDocument,
            string signingKeyIdentifier,
            string signingKeyVersion,
            string signingAlgorithm)
        {
            Type requestType = GetRequiredRepositoryType(FiscalSignatureRequestTypeName);
            object request = Activator.CreateInstance(requestType);
            SetRequiredProperty(request, "FiscalDocument", fiscalDocument);
            SetRequiredProperty(request, "SigningKeyIdentifier", signingKeyIdentifier);
            SetRequiredProperty(request, "SigningKeyVersion", signingKeyVersion);
            SetRequiredProperty(request, "SigningAlgorithm", signingAlgorithm);
            return request;
        }

        private static FiscalDocument CreateFiscalDocument(
            int id,
            int seriesId,
            int sequence,
            DateTimeOffset issuedAtUtc,
            string hash)
        {
            return new FiscalDocument
            {
                Id = id,
                SeriesId = seriesId,
                FiscalIssuerProfileId = 3,
                Kind = FiscalDocumentKind.Original,
                Sequence = sequence,
                DocumentNumber = $"RC {issuedAtUtc.Year}/{sequence}",
                Atcud = $"AA123-{sequence}",
                IssuedAtUtc = issuedAtUtc,
                Hash = hash,
                HashControl = string.Empty,
                SigningKeyVersion = "1",
                SoftwareCertificateNumber = "1234",
                IssuerTaxRegistrationNumber = "501234567",
                TaxRule = "M00",
                GrossTotal = 25.50m,
            };
        }

        private static string GetRepositoryRoot()
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "BancoAlimentar.AlimentaEstaIdeia.Web.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Repository root was not found for migration verification.");
        }

        private static object Sign(object signatureService, object request)
        {
            MethodInfo sign = signatureService.GetType().GetMethod("Sign");
            Assert.NotNull(sign);

            try
            {
                return sign.Invoke(signatureService, new[] { request });
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }

        private static string GetRequiredStringProperty(object source, string propertyName)
        {
            PropertyInfo property = source.GetType().GetProperty(propertyName);
            Assert.NotNull(property);
            return Assert.IsType<string>(property.GetValue(source));
        }

        private static Type GetRequiredRepositoryType(string typeName)
        {
            return Assert.IsAssignableFrom<Type>(typeof(IFiscalScopeResolver).Assembly.GetType(typeName));
        }

        private static void SetRequiredProperty(object target, string propertyName, object value)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName);
            Assert.NotNull(property);
            property.SetValue(target, value);
        }

        private class TestFiscalSigningKeyProvider : DispatchProxy
        {
            private readonly RSA privateKey = RSA.Create(2048);
            private readonly RSA publicKey;

            public TestFiscalSigningKeyProvider()
            {
                this.publicKey = RSA.Create();
                this.publicKey.ImportParameters(this.privateKey.ExportParameters(includePrivateParameters: false));
            }

            public IList<string> CanonicalPayloads { get; } = new List<string>();

            public IList<string> SigningAlgorithms { get; } = new List<string>();

            public Exception ExceptionToThrow { get; set; }

            public string HashControl { get; set; } = "1";

            public bool Verify(string payload, string signature)
            {
                return this.publicKey.VerifyData(
                    Encoding.UTF8.GetBytes(payload),
                    Convert.FromBase64String(signature),
                    HashAlgorithmName.SHA1,
                    RSASignaturePadding.Pkcs1);
            }

            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                if (targetMethod.Name != "Sign")
                {
                    throw new NotSupportedException($"Unexpected signing provider method: {targetMethod.Name}.");
                }

                object signingRequest = Assert.Single(args);
                string canonicalPayload = GetRequiredStringProperty(signingRequest, "CanonicalPayload");
                this.CanonicalPayloads.Add(canonicalPayload);
                this.SigningAlgorithms.Add(GetRequiredStringProperty(signingRequest, "SigningAlgorithm"));

                if (this.ExceptionToThrow != null)
                {
                    ExceptionDispatchInfo.Capture(this.ExceptionToThrow).Throw();
                }

                byte[] signature = this.privateKey.SignData(
                    Encoding.UTF8.GetBytes(canonicalPayload),
                    HashAlgorithmName.SHA1,
                    RSASignaturePadding.Pkcs1);
                object signingResult = Activator.CreateInstance(targetMethod.ReturnType);
                SetRequiredProperty(signingResult, "Hash", Convert.ToBase64String(signature));
                SetRequiredProperty(signingResult, "HashControl", this.HashControl);
                return signingResult;
            }
        }
    }
}
