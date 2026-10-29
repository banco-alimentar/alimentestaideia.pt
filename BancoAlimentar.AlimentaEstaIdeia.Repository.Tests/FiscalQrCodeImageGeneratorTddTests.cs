// -----------------------------------------------------------------------
// <copyright file="FiscalQrCodeImageGeneratorTddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Buffers.Binary;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.ExceptionServices;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Xunit;

    /// <summary>
    /// Defines TDD contracts for rasterising an already-built fiscal QR payload.
    /// </summary>
    public class FiscalQrCodeImageGeneratorTddTests
    {
        private const string GeneratorInterfaceTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.IFiscalQrCodeImageGenerator";
        private const string GeneratorTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalQrCodeImageGenerator";

        /// <summary>
        /// The QR image boundary accepts only the final payload string and returns PNG bytes.
        /// QRCoder 1.6.0 is the repository dependency already used by the web application;
        /// this foundation does not generate PDFs or query fiscal data.
        /// </summary>
        [Fact]
        public void FiscalQrCodeImageGenerator_ExposesPayloadToPngContractUsingQRCoder()
        {
            Type generatorInterfaceType = GetRequiredRepositoryType(GeneratorInterfaceTypeName);
            Type generatorType = GetRequiredRepositoryType(GeneratorTypeName);
            MethodInfo generatePng = Assert.Single(
                generatorInterfaceType.GetMethods(),
                method => method.Name == "GeneratePng" &&
                    method.ReturnType == typeof(byte[]) &&
                    method.GetParameters().Select(parameter => parameter.ParameterType)
                        .SequenceEqual(new[] { typeof(string) }));

            Assert.True(generatorInterfaceType.IsAssignableFrom(generatorType));
            Assert.NotNull(generatorType.GetConstructor(Type.EmptyTypes));
            Assert.Contains(
                typeof(IFiscalSigningKeyProvider).Assembly.GetReferencedAssemblies(),
                assemblyName => assemblyName.Name == "QRCoder");
            Assert.Equal("GeneratePng", generatePng.Name);
        }

        /// <summary>
        /// Without a QR decoder dependency, inspect the portable PNG signature and IHDR image
        /// dimensions. The same official payload must rasterise deterministically into a
        /// nonempty square PNG.
        /// </summary>
        [Fact]
        public void GeneratePng_ProducesDeterministicNonemptySquarePngForFiscalPayload()
        {
            object generator = CreateGenerator();
            const string payload = "A:501234567*B:999999990*C:PT*D:RC*E:N*F:20260918*G:RC 2026/42*H:AA123-42*I1:PT*I7:25.50*I8:0.00*N:0.00*O:25.50*Q:0AkU*R:1234";

            byte[] firstPng = GeneratePng(generator, payload);
            byte[] repeatedPng = GeneratePng(generator, payload);
            (int width, int height) = ReadPngDimensions(firstPng);

            Assert.NotEmpty(firstPng);
            Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, firstPng.Take(8).ToArray());
            Assert.Equal(firstPng, repeatedPng);
            Assert.True(width > 0);
            Assert.Equal(width, height);
        }

        /// <summary>
        /// The image generator must fail closed for absent or malformed input instead of
        /// creating a QR image for a string that was not built by the fiscal payload builder.
        /// </summary>
        /// <param name="payload">The invalid payload.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not-a-fiscal-qr-payload")]
        [InlineData("A:501234567*B:999999990")]
        [InlineData("A:501234567**B:999999990*C:PT*D:RC*E:N*F:20260918*G:RC 2026/42*H:AA123-42*I1:PT*N:0.00*O:25.50*Q:0AkU*R:1234")]
        public void GeneratePng_RejectsEmptyOrNonconformingFiscalPayload(string payload)
        {
            object generator = CreateGenerator();

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => GeneratePng(generator, payload));

            Assert.Contains("QR", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        private static object CreateGenerator()
        {
            Type generatorType = GetRequiredRepositoryType(GeneratorTypeName);
            return Activator.CreateInstance(generatorType);
        }

        private static byte[] GeneratePng(object generator, string payload)
        {
            MethodInfo method = generator.GetType().GetMethod("GeneratePng", new[] { typeof(string) });
            Assert.NotNull(method);

            try
            {
                return Assert.IsType<byte[]>(method.Invoke(generator, new object[] { payload }));
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }

        private static Type GetRequiredRepositoryType(string typeName)
        {
            return Assert.IsAssignableFrom<Type>(typeof(IFiscalSigningKeyProvider).Assembly.GetType(typeName));
        }

        private static (int Width, int Height) ReadPngDimensions(byte[] pngBytes)
        {
            Assert.True(pngBytes.Length >= 24, "PNG must contain signature, IHDR length, chunk name and dimensions.");
            Assert.Equal(new byte[] { 73, 72, 68, 82 }, pngBytes.Skip(12).Take(4).ToArray());

            return (
                BinaryPrimitives.ReadInt32BigEndian(pngBytes.AsSpan(16, 4)),
                BinaryPrimitives.ReadInt32BigEndian(pngBytes.AsSpan(20, 4)));
        }
    }
}
