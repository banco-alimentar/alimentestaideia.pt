// -----------------------------------------------------------------------
// <copyright file="FiscalSignatureTddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    using System.Security.Cryptography;
    using System.Text;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata;
    using Xunit;

    /// <summary>
    /// Defines TDD contracts for canonical fiscal signatures and their hash chain.
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
        /// The signature boundary accepts the immutable document, predecessor hash and a
        /// versioned key reference. Its provider receives only a canonical payload and the
        /// non-secret key reference, never private key material.
        /// </summary>
        [Fact]
        public void FiscalSignatureService_ExposesCanonicalHashChainContract()
        {
            Type signatureRequestType = GetRequiredRepositoryType(FiscalSignatureRequestTypeName);
            Type signatureResultType = GetRequiredRepositoryType(FiscalSignatureResultTypeName);
            Type keyRequestType = GetRequiredRepositoryType(FiscalSigningKeyRequestTypeName);
            Type keyResultType = GetRequiredRepositoryType(FiscalSigningKeyResultTypeName);
            Type signatureServiceInterfaceType = GetRequiredRepositoryType(FiscalSignatureServiceInterfaceTypeName);
            Type keyProviderInterfaceType = GetRequiredRepositoryType(FiscalSigningKeyProviderInterfaceTypeName);

            Assert.NotNull(signatureRequestType.GetProperty("FiscalDocument"));
            Assert.NotNull(signatureRequestType.GetProperty("PreviousHash"));
            Assert.NotNull(signatureRequestType.GetProperty("SigningKeyIdentifier"));
            Assert.NotNull(signatureRequestType.GetProperty("SigningKeyVersion"));
            Assert.NotNull(keyRequestType.GetProperty("CanonicalPayload"));
            Assert.NotNull(keyRequestType.GetProperty("SigningKeyIdentifier"));
            Assert.NotNull(keyRequestType.GetProperty("SigningKeyVersion"));
            Assert.NotNull(keyResultType.GetProperty("Hash"));
            Assert.NotNull(keyResultType.GetProperty("HashControl"));
            Assert.NotNull(signatureResultType.GetProperty("CanonicalPayload"));
            Assert.NotNull(signatureResultType.GetProperty("PreviousHash"));
            Assert.NotNull(signatureResultType.GetProperty("Hash"));
            Assert.NotNull(signatureResultType.GetProperty("HashControl"));
            Assert.NotNull(signatureResultType.GetProperty("SigningKeyVersion"));

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

            Assert.Equal("Sign", signDocument.Name);
            Assert.Equal("Sign", signPayload.Name);
        }

        /// <summary>
        /// Equivalent immutable fiscal snapshots produce the same canonical payload. The prior
        /// document hash must be included in that payload and copied to the signing result,
        /// creating an observable document hash chain.
        /// </summary>
        [Fact]
        public void Sign_BuildsDeterministicCanonicalPayloadAndChainsPreviousHash()
        {
            Type keyProviderType = GetRequiredRepositoryType(FiscalSigningKeyProviderInterfaceTypeName);
            var testProvider = CreateTestSigningKeyProvider(keyProviderType, out object provider);
            object signatureService = CreateSignatureService(provider);
            FiscalDocument firstDocument = CreateFiscalDocument();
            FiscalDocument identicalDocument = CreateFiscalDocument();
            const string previousHash = "previous-fiscal-hash";

            object firstResult = Sign(
                signatureService,
                CreateSignatureRequest(firstDocument, previousHash, "test-signing-key", "v1"));
            object secondResult = Sign(
                signatureService,
                CreateSignatureRequest(identicalDocument, previousHash, "test-signing-key", "v1"));

            string firstPayload = GetRequiredStringProperty(firstResult, "CanonicalPayload");
            string secondPayload = GetRequiredStringProperty(secondResult, "CanonicalPayload");

            Assert.Equal(firstPayload, secondPayload);
            Assert.Equal(firstPayload, testProvider.CanonicalPayloads[0]);
            Assert.Equal(secondPayload, testProvider.CanonicalPayloads[1]);
            Assert.Contains(previousHash, firstPayload, StringComparison.Ordinal);
            Assert.Contains(firstDocument.DocumentNumber, firstPayload, StringComparison.Ordinal);
            Assert.Contains(firstDocument.Atcud, firstPayload, StringComparison.Ordinal);
            Assert.Equal(previousHash, GetRequiredStringProperty(firstResult, "PreviousHash"));
            Assert.Equal(testProvider.Hashes[0], GetRequiredStringProperty(firstResult, "Hash"));
            Assert.Equal("1", GetRequiredStringProperty(firstResult, "HashControl"));
            Assert.Equal("v1", GetRequiredStringProperty(firstResult, "SigningKeyVersion"));
        }

        /// <summary>
        /// The hash, control and key version are mandatory persisted evidence once a fiscal
        /// document is issued. The predecessor remains nullable only for the first document in
        /// a series chain.
        /// </summary>
        [Fact]
        public void FiscalDocument_ModelRequiresSignatureEvidenceAndAllowsFirstPreviousHash()
        {
            using var context = CreateContext();
            IEntityType fiscalDocument = Assert.IsAssignableFrom<IEntityType>(
                context.Model.FindEntityType(typeof(FiscalDocument)));

            Assert.False(Assert.IsAssignableFrom<IProperty>(fiscalDocument.FindProperty("Hash")).IsNullable);
            Assert.False(Assert.IsAssignableFrom<IProperty>(fiscalDocument.FindProperty("HashControl")).IsNullable);
            Assert.False(Assert.IsAssignableFrom<IProperty>(fiscalDocument.FindProperty("SigningKeyVersion")).IsNullable);
            Assert.True(Assert.IsAssignableFrom<IProperty>(fiscalDocument.FindProperty("PreviousHash")).IsNullable);
        }

        /// <summary>
        /// Missing signing-key configuration fails closed before any provider invocation or
        /// mutation of the reserved document's persisted signature evidence.
        /// </summary>
        [Fact]
        public void Sign_RejectsMissingSigningKeyBeforeProviderInvocation()
        {
            Type keyProviderType = GetRequiredRepositoryType(FiscalSigningKeyProviderInterfaceTypeName);
            var testProvider = CreateTestSigningKeyProvider(keyProviderType, out object provider);
            object signatureService = CreateSignatureService(provider);
            FiscalDocument document = CreateFiscalDocument();

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => Sign(
                    signatureService,
                    CreateSignatureRequest(document, null, "   ", "v1")));

            Assert.Contains("key", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(testProvider.CanonicalPayloads);
            Assert.Equal(string.Empty, document.Hash);
            Assert.Equal(string.Empty, document.HashControl);
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

        private static object CreateSignatureService(object provider)
        {
            Type serviceType = GetRequiredRepositoryType(FiscalSignatureServiceTypeName);
            Type providerType = GetRequiredRepositoryType(FiscalSigningKeyProviderInterfaceTypeName);
            ConstructorInfo constructor = serviceType.GetConstructor(new[] { providerType });
            Assert.NotNull(constructor);
            return constructor.Invoke(new[] { provider });
        }

        private static object CreateSignatureRequest(
            FiscalDocument fiscalDocument,
            string previousHash,
            string signingKeyIdentifier,
            string signingKeyVersion)
        {
            Type requestType = GetRequiredRepositoryType(FiscalSignatureRequestTypeName);
            object request = Activator.CreateInstance(requestType);
            requestType.GetProperty("FiscalDocument").SetValue(request, fiscalDocument);
            requestType.GetProperty("PreviousHash").SetValue(request, previousHash);
            requestType.GetProperty("SigningKeyIdentifier").SetValue(request, signingKeyIdentifier);
            requestType.GetProperty("SigningKeyVersion").SetValue(request, signingKeyVersion);
            return request;
        }

        private static FiscalDocument CreateFiscalDocument()
        {
            return new FiscalDocument
            {
                Id = 42,
                SeriesId = 7,
                FiscalIssuerProfileId = 3,
                Kind = FiscalDocumentKind.Original,
                Sequence = 42,
                DocumentNumber = "RC 2026/42",
                Atcud = "AA123-42",
                IssuedAtUtc = new DateTimeOffset(2026, 9, 19, 10, 30, 0, TimeSpan.Zero),
                Hash = string.Empty,
                HashControl = string.Empty,
                SigningKeyVersion = "v1",
                SoftwareCertificateNumber = "1234",
                IssuerTaxRegistrationNumber = "501234567",
                TaxRule = "M00",
                GrossTotal = 25.50m,
            };
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

        private sealed class TestFiscalSigningKeyProvider : DispatchProxy
        {
            public IList<string> CanonicalPayloads { get; } = new List<string>();

            public IList<string> Hashes { get; } = new List<string>();

            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                if (targetMethod.Name != "Sign")
                {
                    throw new NotSupportedException($"Unexpected signing provider method: {targetMethod.Name}.");
                }

                object signingRequest = Assert.Single(args);
                string canonicalPayload = GetRequiredStringProperty(signingRequest, "CanonicalPayload");
                string hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalPayload)));
                this.CanonicalPayloads.Add(canonicalPayload);
                this.Hashes.Add(hash);

                object signingResult = Activator.CreateInstance(targetMethod.ReturnType);
                targetMethod.ReturnType.GetProperty("Hash").SetValue(signingResult, hash);
                targetMethod.ReturnType.GetProperty("HashControl").SetValue(signingResult, "1");
                return signingResult;
            }
        }
    }
}
