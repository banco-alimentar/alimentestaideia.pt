// -----------------------------------------------------------------------
// <copyright file="FiscalQrPayloadBuilderTddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.ExceptionServices;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Xunit;

    /// <summary>
    /// Defines TDD contracts for the persisted Portuguese fiscal QR payload.
    /// </summary>
    public class FiscalQrPayloadBuilderTddTests
    {
        private const string BuilderInterfaceTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.IFiscalQrPayloadBuilder";
        private const string BuilderTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalQrPayloadBuilder";
        private const string SnapshotTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalQrPayloadSnapshot";

        /// <summary>
        /// QR construction accepts one final fiscal snapshot and returns its textual payload.
        /// It has no tenant, donation or application-database dependency, because all values
        /// must already be captured in the issued fiscal document.
        /// </summary>
        [Fact]
        public void FiscalQrPayloadBuilder_ExposesSnapshotOnlyContract()
        {
            Type builderInterfaceType = GetRequiredRepositoryType(BuilderInterfaceTypeName);
            Type builderType = GetRequiredRepositoryType(BuilderTypeName);
            Type snapshotType = GetRequiredRepositoryType(SnapshotTypeName);
            MethodInfo build = Assert.Single(
                builderInterfaceType.GetMethods(),
                method => method.Name == "Build" &&
                    method.ReturnType == typeof(string) &&
                    method.GetParameters().Select(parameter => parameter.ParameterType)
                        .SequenceEqual(new[] { snapshotType }));

            Assert.True(builderInterfaceType.IsAssignableFrom(builderType));
            Assert.NotNull(builderType.GetConstructor(Type.EmptyTypes));
            Assert.Equal("Build", build.Name);
            Assert.DoesNotContain(
                builderType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public),
                field => field.FieldType.FullName == "BancoAlimentar.AlimentaEstaIdeia.Model.ApplicationDbContext" ||
                    field.FieldType.FullName == "BancoAlimentar.AlimentaEstaIdeia.Model.Donation" ||
                    field.FieldType.FullName == "BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Tenant");
            AssertRequiredProperties(
                snapshotType,
                "IssuerTaxRegistrationNumber",
                "BuyerTaxRegistrationNumber",
                "BuyerCountryCode",
                "DocumentType",
                "DocumentStatus",
                "InvoiceDate",
                "InvoiceIdentifier",
                "Atcud",
                "TaxCountryRegion",
                "TaxableBaseNormal",
                "TaxAmountNormal",
                "TaxPayable",
                "GrossTotal",
                "Hash",
                "HashControl",
                "SoftwareCertificateNumber");
            Assert.Equal(typeof(DateOnly), snapshotType.GetProperty("InvoiceDate")?.PropertyType);
        }

        /// <summary>
        /// Source: AT, "QR Code Technical Specifications", section 5.2, available at
        /// https://info.portaldasfinancas.gov.pt/pt/docs/Conteudos_1pagina/Documents/QR_Code_Technical_Specifications.pdf.
        /// Fields are ordered and joined with '*'. Q contains the 1st, 11th, 21st and 31st
        /// characters of Hash; HashControl remains a required fiscal snapshot even though Q
        /// itself contains the four-character Hash excerpt.
        /// </summary>
        [Fact]
        public void Build_ProducesDeterministicOfficialFiscalQrFieldOrder()
        {
            object builder = CreateBuilder();
            object snapshot = CreateCompleteSnapshot();

            string firstPayload = Build(builder, snapshot);
            string repeatedPayload = Build(builder, snapshot);

            const string expectedPayload = "A:501234567*B:999999990*C:PT*D:RC*E:N*F:20260918*G:RC 2026/42*H:AA123-42*I1:PT*I7:25.50*I8:0.00*N:0.00*O:25.50*Q:0AkU*R:1234";
            Assert.Equal(expectedPayload, firstPayload);
            Assert.Equal(firstPayload, repeatedPayload);
            Assert.DoesNotContain("Tenant", firstPayload, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Donation", firstPayload, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Every mandatory fiscal field must already exist in the immutable snapshot. The
        /// builder fails rather than querying current tenant or donation state to fill gaps.
        /// </summary>
        /// <param name="propertyName">The mandatory fiscal snapshot property to blank.</param>
        [Theory]
        [InlineData("IssuerTaxRegistrationNumber")]
        [InlineData("BuyerTaxRegistrationNumber")]
        [InlineData("BuyerCountryCode")]
        [InlineData("DocumentType")]
        [InlineData("DocumentStatus")]
        [InlineData("InvoiceIdentifier")]
        [InlineData("Atcud")]
        [InlineData("TaxCountryRegion")]
        [InlineData("Hash")]
        [InlineData("HashControl")]
        [InlineData("SoftwareCertificateNumber")]
        public void Build_RejectsIncompleteFiscalSnapshot(string propertyName)
        {
            object builder = CreateBuilder();
            object snapshot = CreateCompleteSnapshot();
            SetRequiredProperty(snapshot, propertyName, "   ");

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => Build(builder, snapshot));

            Assert.Contains("QR", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        private static void AssertRequiredProperties(Type type, params string[] propertyNames)
        {
            foreach (string propertyName in propertyNames)
            {
                Assert.NotNull(type.GetProperty(propertyName));
            }
        }

        private static string Build(object builder, object snapshot)
        {
            MethodInfo build = builder.GetType().GetMethod("Build");
            Assert.NotNull(build);

            try
            {
                return Assert.IsType<string>(build.Invoke(builder, new[] { snapshot }));
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }

        private static object CreateBuilder()
        {
            Type builderType = GetRequiredRepositoryType(BuilderTypeName);
            return Activator.CreateInstance(builderType);
        }

        private static object CreateCompleteSnapshot()
        {
            Type snapshotType = GetRequiredRepositoryType(SnapshotTypeName);
            object snapshot = Activator.CreateInstance(snapshotType);
            SetRequiredProperty(snapshot, "IssuerTaxRegistrationNumber", "501234567");
            SetRequiredProperty(snapshot, "BuyerTaxRegistrationNumber", "999999990");
            SetRequiredProperty(snapshot, "BuyerCountryCode", "PT");
            SetRequiredProperty(snapshot, "DocumentType", "RC");
            SetRequiredProperty(snapshot, "DocumentStatus", "N");
            SetRequiredProperty(snapshot, "InvoiceDate", new DateOnly(2026, 9, 18));
            SetRequiredProperty(snapshot, "InvoiceIdentifier", "RC 2026/42");
            SetRequiredProperty(snapshot, "Atcud", "AA123-42");
            SetRequiredProperty(snapshot, "TaxCountryRegion", "PT");
            SetRequiredProperty(snapshot, "TaxableBaseNormal", 25.50m);
            SetRequiredProperty(snapshot, "TaxAmountNormal", 0.00m);
            SetRequiredProperty(snapshot, "TaxPayable", 0.00m);
            SetRequiredProperty(snapshot, "GrossTotal", 25.50m);
            SetRequiredProperty(snapshot, "Hash", "0123456789ABCDEFGHIJklmnopqrstUV");
            SetRequiredProperty(snapshot, "HashControl", "1");
            SetRequiredProperty(snapshot, "SoftwareCertificateNumber", "1234");
            return snapshot;
        }

        private static Type GetRequiredRepositoryType(string typeName)
        {
            return Assert.IsAssignableFrom<Type>(typeof(IFiscalSigningKeyProvider).Assembly.GetType(typeName));
        }

        private static void SetRequiredProperty(object target, string propertyName, object value)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName);
            Assert.NotNull(property);
            property.SetValue(target, value);
        }
    }
}
