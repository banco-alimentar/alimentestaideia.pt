// -----------------------------------------------------------------------
// <copyright file="FiscalReceiptViewModelFactoryTddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Xunit;

    /// <summary>
    /// Defines TDD contracts for a receipt render model based only on fiscal snapshots.
    /// </summary>
    public class FiscalReceiptViewModelFactoryTddTests
    {
        private const string FactoryTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalReceiptViewModelFactory";
        private const string LineViewModelTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalReceiptLineViewModel";
        private const string ViewModelTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalReceiptViewModel";

        /// <summary>
        /// The immutable receipt model is constructed from one loaded fiscal document. It does
        /// not take a Donation, WebUser, FoodBank, tenant, configuration or application context
        /// that could change a reprint after the fiscal document was issued.
        /// </summary>
        [Fact]
        public void FiscalReceiptViewModelFactory_ExposesDocumentOnlyRenderContract()
        {
            Type factoryType = GetRequiredRepositoryType(FactoryTypeName);
            Type viewModelType = GetRequiredRepositoryType(ViewModelTypeName);
            Type lineViewModelType = GetRequiredRepositoryType(LineViewModelTypeName);
            MethodInfo create = Assert.Single(
                factoryType.GetMethods(BindingFlags.Instance | BindingFlags.Public),
                method => method.Name == "Create" &&
                    method.ReturnType == viewModelType &&
                    method.GetParameters().Select(parameter => parameter.ParameterType)
                        .SequenceEqual(new[] { typeof(FiscalDocument) }));

            Assert.NotNull(factoryType.GetConstructor(Type.EmptyTypes));
            Assert.Equal("Create", create.Name);
            AssertNoCurrentEntityDependency(factoryType);
            AssertRequiredProperties(
                viewModelType,
                "IssuerName",
                "IssuerTaxRegistrationNumber",
                "IssuerLegalAddress",
                "BuyerName",
                "BuyerTaxRegistrationNumber",
                "Lines",
                "NetTotal",
                "TaxTotal",
                "GrossTotal",
                "DocumentIdentifier",
                "Atcud",
                "HashControl",
                "SoftwareCertificateNumber",
                "QrPayload",
                "QrPng",
                "IsCancelled",
                "CancellationReason",
                "IsCorrection",
                "OriginalDocumentIdentifier");
            AssertRequiredProperties(
                lineViewModelType,
                "LineNumber",
                "ProductCode",
                "Description",
                "Quantity",
                "UnitPrice",
                "TaxRate",
                "TaxExemptionReason",
                "NetTotal",
                "TaxTotal",
                "GrossTotal");
            Assert.Equal(typeof(byte[]), typeof(FiscalDocument).GetProperty("QrPng")?.PropertyType);
        }

        /// <summary>
        /// Issuer, buyer, lines, totals, certificate, signature control and QR values are copied
        /// from the final fiscal document snapshot. Lines retain their persisted order and are
        /// not reconstructed from current donation items.
        /// </summary>
        [Fact]
        public void Create_MapsOnlyPersistedFiscalSnapshotsLinesTotalsAndQr()
        {
            FiscalDocument document = CreateFiscalDocument();
            object factory = CreateFactory();

            object viewModel = Create(factory, document);
            IList lines = Assert.IsAssignableFrom<IList>(GetRequiredProperty(viewModel, "Lines"));
            object firstLine = Assert.Single(lines.Cast<object>());

            Assert.Equal("Federação de teste", GetRequiredStringProperty(viewModel, "IssuerName"));
            Assert.Equal("501234567", GetRequiredStringProperty(viewModel, "IssuerTaxRegistrationNumber"));
            Assert.Equal("Rua Fiscal, 1, Lisboa", GetRequiredStringProperty(viewModel, "IssuerLegalAddress"));
            Assert.Equal("Doador histórico", GetRequiredStringProperty(viewModel, "BuyerName"));
            Assert.Equal("123456789", GetRequiredStringProperty(viewModel, "BuyerTaxRegistrationNumber"));
            Assert.Equal("RC 2026/42", GetRequiredStringProperty(viewModel, "DocumentIdentifier"));
            Assert.Equal("AA123-42", GetRequiredStringProperty(viewModel, "Atcud"));
            Assert.Equal("3", GetRequiredStringProperty(viewModel, "HashControl"));
            Assert.Equal("1234", GetRequiredStringProperty(viewModel, "SoftwareCertificateNumber"));
            Assert.Equal("A:501234567*H:AA123-42", GetRequiredStringProperty(viewModel, "QrPayload"));
            Assert.Equal(new byte[] { 137, 80, 78, 71 }, Assert.IsType<byte[]>(GetRequiredProperty(viewModel, "QrPng")));
            Assert.Equal(20.00m, GetRequiredDecimalProperty(viewModel, "NetTotal"));
            Assert.Equal(5.00m, GetRequiredDecimalProperty(viewModel, "TaxTotal"));
            Assert.Equal(25.00m, GetRequiredDecimalProperty(viewModel, "GrossTotal"));
            Assert.Equal(1, GetRequiredIntProperty(firstLine, "LineNumber"));
            Assert.Equal("DONATION", GetRequiredStringProperty(firstLine, "ProductCode"));
            Assert.Equal("Donativo fiscal", GetRequiredStringProperty(firstLine, "Description"));
            Assert.False(GetRequiredBooleanProperty(viewModel, "IsCancelled"));
            Assert.False(GetRequiredBooleanProperty(viewModel, "IsCorrection"));
        }

        /// <summary>
        /// Cancellation and correction presentation derives only from the stored lifecycle
        /// state, append-only event and original fiscal-document navigation already loaded with
        /// the document; no current entity configuration is consulted.
        /// </summary>
        [Fact]
        public void Create_MapsPersistedCancellationAndCorrectionPresentation()
        {
            FiscalDocument document = CreateFiscalDocument();
            document.Status = FiscalDocumentStatus.Cancelled;
            document.Kind = FiscalDocumentKind.Correction;
            document.OriginalFiscalDocumentId = 10;
            document.OriginalFiscalDocument = new FiscalDocument
            {
                Id = 10,
                DocumentNumber = "RC 2026/41",
            };
            document.Events = new List<FiscalDocumentEvent>
            {
                new FiscalDocumentEvent
                {
                    EventType = "Cancelled",
                    Reason = "Erro de emissão comunicado pelo emitente.",
                    OccurredAtUtc = new DateTimeOffset(2026, 9, 20, 10, 0, 0, TimeSpan.Zero),
                },
            };

            object viewModel = Create(CreateFactory(), document);

            Assert.True(GetRequiredBooleanProperty(viewModel, "IsCancelled"));
            Assert.Equal("Erro de emissão comunicado pelo emitente.", GetRequiredStringProperty(viewModel, "CancellationReason"));
            Assert.True(GetRequiredBooleanProperty(viewModel, "IsCorrection"));
            Assert.Equal("RC 2026/41", GetRequiredStringProperty(viewModel, "OriginalDocumentIdentifier"));
        }

        private static void AssertNoCurrentEntityDependency(Type factoryType)
        {
            string[] prohibitedTypeNames =
            {
                "BancoAlimentar.AlimentaEstaIdeia.Model.ApplicationDbContext",
                "BancoAlimentar.AlimentaEstaIdeia.Model.Donation",
                "BancoAlimentar.AlimentaEstaIdeia.Model.Identity.WebUser",
                "BancoAlimentar.AlimentaEstaIdeia.Model.FoodBank",
                "BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Tenant",
                "Microsoft.Extensions.Configuration.IConfiguration",
            };

            Assert.DoesNotContain(
                factoryType.GetConstructors().SelectMany(constructor => constructor.GetParameters()),
                parameter => prohibitedTypeNames.Contains(parameter.ParameterType.FullName, StringComparer.Ordinal));
            Assert.DoesNotContain(
                factoryType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public),
                field => prohibitedTypeNames.Contains(field.FieldType.FullName, StringComparer.Ordinal));
        }

        private static void AssertRequiredProperties(Type type, params string[] propertyNames)
        {
            foreach (string propertyName in propertyNames)
            {
                Assert.NotNull(type.GetProperty(propertyName));
            }
        }

        private static object Create(object factory, FiscalDocument document)
        {
            MethodInfo create = factory.GetType().GetMethod("Create", new[] { typeof(FiscalDocument) });
            Assert.NotNull(create);
            object viewModel = create.Invoke(factory, new object[] { document });
            Assert.NotNull(viewModel);
            return viewModel;
        }

        private static object CreateFactory()
        {
            Type factoryType = GetRequiredRepositoryType(FactoryTypeName);
            return Activator.CreateInstance(factoryType);
        }

        private static FiscalDocument CreateFiscalDocument()
        {
            var document = new FiscalDocument
            {
                Id = 42,
                DocumentNumber = "RC 2026/42",
                Atcud = "AA123-42",
                InvoiceDate = new DateOnly(2026, 9, 18),
                HashControl = "3",
                SoftwareCertificateNumber = "1234",
                IssuerLegalName = "Federação de teste",
                IssuerTaxRegistrationNumber = "501234567",
                IssuerLegalAddress = "Rua Fiscal, 1, Lisboa",
                DonorName = "Doador histórico",
                DonorTaxRegistrationNumber = "123456789",
                QrPayload = "A:501234567*H:AA123-42",
                NetTotal = 20.00m,
                TaxTotal = 5.00m,
                GrossTotal = 25.00m,
                Kind = FiscalDocumentKind.Original,
                Status = FiscalDocumentStatus.Issued,
                Lines = new List<FiscalDocumentLine>
                {
                    new FiscalDocumentLine
                    {
                        LineNumber = 1,
                        ProductCode = "DONATION",
                        Description = "Donativo fiscal",
                        Quantity = 1.000m,
                        UnitPrice = 20.00m,
                        TaxRate = 25.00m,
                        TaxExemptionReason = string.Empty,
                        NetTotal = 20.00m,
                        TaxTotal = 5.00m,
                        GrossTotal = 25.00m,
                    },
                },
            };
            SetRequiredProperty(document, "QrPng", new byte[] { 137, 80, 78, 71 });
            return document;
        }

        private static Type GetRequiredRepositoryType(string typeName)
        {
            return Assert.IsAssignableFrom<Type>(typeof(IFiscalSigningKeyProvider).Assembly.GetType(typeName));
        }

        private static bool GetRequiredBooleanProperty(object source, string propertyName)
        {
            return Assert.IsType<bool>(GetRequiredProperty(source, propertyName));
        }

        private static decimal GetRequiredDecimalProperty(object source, string propertyName)
        {
            return Assert.IsType<decimal>(GetRequiredProperty(source, propertyName));
        }

        private static int GetRequiredIntProperty(object source, string propertyName)
        {
            return Assert.IsType<int>(GetRequiredProperty(source, propertyName));
        }

        private static object GetRequiredProperty(object source, string propertyName)
        {
            PropertyInfo property = source.GetType().GetProperty(propertyName);
            Assert.NotNull(property);
            object value = property.GetValue(source);
            Assert.NotNull(value);
            return value;
        }

        private static string GetRequiredStringProperty(object source, string propertyName)
        {
            return Assert.IsType<string>(GetRequiredProperty(source, propertyName));
        }

        private static void SetRequiredProperty(object target, string propertyName, object value)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName);
            Assert.NotNull(property);
            property.SetValue(target, value);
        }
    }
}
