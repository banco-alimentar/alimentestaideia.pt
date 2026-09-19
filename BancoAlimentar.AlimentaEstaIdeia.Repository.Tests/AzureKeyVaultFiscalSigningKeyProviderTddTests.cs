// -----------------------------------------------------------------------
// <copyright file="AzureKeyVaultFiscalSigningKeyProviderTddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Xunit;

    /// <summary>
    /// Defines network-free TDD contracts for Azure Key Vault fiscal signing.
    /// </summary>
    public class AzureKeyVaultFiscalSigningKeyProviderTddTests
    {
        private const string AzureKeyVaultClientTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.IAzureKeyVaultSigningClient";
        private const string AzureKeyVaultRequestTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.AzureKeyVaultSigningRequest";
        private const string AzureKeyVaultProviderTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.AzureKeyVaultFiscalSigningKeyProvider";

        /// <summary>
        /// Key Vault remains behind a repository-owned abstraction so provider tests do not
        /// require a live vault or Azure credential. RS1 identifies RSA PKCS#1 v1.5 with SHA-1,
        /// the RSA signature scheme used for Portuguese SAF-T fiscal signatures.
        /// </summary>
        [Fact]
        public void AzureKeyVaultProvider_ExposesMockableRs1SigningClientContract()
        {
            Type clientType = GetRequiredRepositoryType(AzureKeyVaultClientTypeName);
            Type clientRequestType = GetRequiredRepositoryType(AzureKeyVaultRequestTypeName);
            Type providerType = GetRequiredRepositoryType(AzureKeyVaultProviderTypeName);

            Assert.True(typeof(IFiscalSigningKeyProvider).IsAssignableFrom(providerType));
            Assert.NotNull(providerType.GetConstructor(new[] { clientType }));
            AssertRequiredProperties(clientRequestType, "KeyIdentifier", "KeyVersion", "Payload", "Algorithm");
            MethodInfo sign = Assert.Single(
                clientType.GetMethods(),
                method => method.Name == "Sign" &&
                    method.GetParameters().Select(parameter => parameter.ParameterType)
                        .SequenceEqual(new[] { clientRequestType }) &&
                    method.ReturnType == typeof(byte[]));
            Assert.Equal("Sign", sign.Name);
        }

        /// <summary>
        /// The provider passes the UTF-8 bytes of the canonical legal payload unchanged to the
        /// Key Vault boundary, requests RS1, and returns the resulting RSA signature as Base64.
        /// The test client signs with a private test key and verifies with a public-only key.
        /// </summary>
        [Fact]
        public void Sign_SendsExactPayloadBytesAsRs1AndReturnsBase64SignatureWithKeyVersionControl()
        {
            Type clientType = GetRequiredRepositoryType(AzureKeyVaultClientTypeName);
            TestAzureKeyVaultSigningClient client = CreateTestClient(clientType, out object clientProxy);
            object provider = CreateProvider(clientProxy);
            const string canonicalPayload = "2026-09-18;2026-09-19T10:30:00;RC 2026/42;25.50;previous-hash";
            const string keyIdentifier = "https://example.vault.azure.net/keys/fiscal-signing";
            const string keyVersion = "3";

            FiscalSigningKeyResult result = Sign(
                provider,
                new FiscalSigningKeyRequest
                {
                    CanonicalPayload = canonicalPayload,
                    SigningKeyIdentifier = keyIdentifier,
                    SigningKeyVersion = keyVersion,
                    SigningAlgorithm = "RSA",
                });

            Assert.Equal(Encoding.UTF8.GetBytes(canonicalPayload), Assert.Single(client.Payloads));
            Assert.Equal(keyIdentifier, Assert.Single(client.KeyIdentifiers));
            Assert.Equal(keyVersion, Assert.Single(client.KeyVersions));
            Assert.Equal("RS1", Assert.Single(client.Algorithms));
            Assert.Equal(keyVersion, result.HashControl);
            Assert.True(client.Verify(canonicalPayload, result.Hash));
        }

        /// <summary>
        /// Missing key identity or version is rejected before any remote-signing attempt.
        /// </summary>
        /// <param name="keyIdentifier">The invalid key identifier.</param>
        /// <param name="keyVersion">The invalid key version.</param>
        [Theory]
        [InlineData("", "3")]
        [InlineData("   ", "3")]
        [InlineData("key", "")]
        [InlineData("key", "   ")]
        public void Sign_RejectsMissingKeyReferenceBeforeCallingClient(string keyIdentifier, string keyVersion)
        {
            Type clientType = GetRequiredRepositoryType(AzureKeyVaultClientTypeName);
            TestAzureKeyVaultSigningClient client = CreateTestClient(clientType, out object clientProxy);
            object provider = CreateProvider(clientProxy);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => Sign(
                    provider,
                    new FiscalSigningKeyRequest
                    {
                        CanonicalPayload = "legal-payload",
                        SigningKeyIdentifier = keyIdentifier,
                        SigningKeyVersion = keyVersion,
                        SigningAlgorithm = "RSA",
                    }));

            Assert.Contains("key", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(client.Payloads);
        }

        /// <summary>
        /// A failure reported by the remote signing boundary does not manufacture a hash or
        /// control code. The provider fails closed with a fiscal-signing error.
        /// </summary>
        [Fact]
        public void Sign_FailsClosedWhenKeyVaultClientFails()
        {
            Type clientType = GetRequiredRepositoryType(AzureKeyVaultClientTypeName);
            TestAzureKeyVaultSigningClient client = CreateTestClient(clientType, out object clientProxy);
            client.ExceptionToThrow = new InvalidOperationException("Key Vault signing operation failed.");
            object provider = CreateProvider(clientProxy);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => Sign(
                    provider,
                    new FiscalSigningKeyRequest
                    {
                        CanonicalPayload = "legal-payload",
                        SigningKeyIdentifier = "key",
                        SigningKeyVersion = "3",
                        SigningAlgorithm = "RSA",
                    }));

            Assert.Contains("sign", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Single(client.Payloads);
        }

        private static void AssertRequiredProperties(Type type, params string[] propertyNames)
        {
            foreach (string propertyName in propertyNames)
            {
                Assert.NotNull(type.GetProperty(propertyName));
            }
        }

        private static TestAzureKeyVaultSigningClient CreateTestClient(Type clientType, out object clientProxy)
        {
            clientProxy = DispatchProxy.Create(clientType, typeof(TestAzureKeyVaultSigningClient));
            return Assert.IsAssignableFrom<TestAzureKeyVaultSigningClient>(clientProxy);
        }

        private static object CreateProvider(object client)
        {
            Type providerType = GetRequiredRepositoryType(AzureKeyVaultProviderTypeName);
            Type clientType = GetRequiredRepositoryType(AzureKeyVaultClientTypeName);
            ConstructorInfo constructor = providerType.GetConstructor(new[] { clientType });
            Assert.NotNull(constructor);
            return constructor.Invoke(new[] { client });
        }

        private static Type GetRequiredRepositoryType(string typeName)
        {
            return Assert.IsAssignableFrom<Type>(typeof(IFiscalSigningKeyProvider).Assembly.GetType(typeName));
        }

        private static FiscalSigningKeyResult Sign(object provider, FiscalSigningKeyRequest request)
        {
            MethodInfo sign = provider.GetType().GetMethod("Sign", new[] { typeof(FiscalSigningKeyRequest) });
            Assert.NotNull(sign);

            try
            {
                return Assert.IsAssignableFrom<FiscalSigningKeyResult>(sign.Invoke(provider, new object[] { request }));
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

        private class TestAzureKeyVaultSigningClient : DispatchProxy
        {
            private readonly RSA privateKey = RSA.Create(2048);
            private readonly RSA publicKey;

            public TestAzureKeyVaultSigningClient()
            {
                this.publicKey = RSA.Create();
                this.publicKey.ImportParameters(this.privateKey.ExportParameters(includePrivateParameters: false));
            }

            public IList<string> Algorithms { get; } = new List<string>();

            public Exception ExceptionToThrow { get; set; }

            public IList<string> KeyIdentifiers { get; } = new List<string>();

            public IList<string> KeyVersions { get; } = new List<string>();

            public IList<byte[]> Payloads { get; } = new List<byte[]>();

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
                    throw new NotSupportedException($"Unexpected Key Vault client method: {targetMethod.Name}.");
                }

                object request = Assert.Single(args);
                byte[] payload = Assert.IsType<byte[]>(GetRequiredProperty(request, "Payload"));
                this.Payloads.Add(payload);
                this.KeyIdentifiers.Add(GetRequiredStringProperty(request, "KeyIdentifier"));
                this.KeyVersions.Add(GetRequiredStringProperty(request, "KeyVersion"));
                this.Algorithms.Add(GetRequiredStringProperty(request, "Algorithm"));

                if (this.ExceptionToThrow != null)
                {
                    ExceptionDispatchInfo.Capture(this.ExceptionToThrow).Throw();
                }

                return this.privateKey.SignData(payload, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
            }

            private static object GetRequiredProperty(object source, string propertyName)
            {
                PropertyInfo property = source.GetType().GetProperty(propertyName);
                Assert.NotNull(property);
                object value = property.GetValue(source);
                Assert.NotNull(value);
                return value;
            }
        }
    }
}
