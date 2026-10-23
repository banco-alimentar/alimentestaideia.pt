// -----------------------------------------------------------------------
// <copyright file="FiscalReceiptDeliveryRoutingTddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage;
    using Xunit;

    /// <summary>
    /// Defines the web boundary for immutable fiscal-receipt PDF delivery.
    /// </summary>
    public class FiscalReceiptDeliveryRoutingTddTests
    {
        private const string DeliveryServiceTypeName = "BancoAlimentar.AlimentaEstaIdeia.Web.Services.Invoices.FiscalReceiptDeliveryService";
        private const string DeliveryServiceInterfaceTypeName = "BancoAlimentar.AlimentaEstaIdeia.Web.Services.Invoices.IFiscalReceiptDeliveryService";
        private const string PdfServiceTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalReceiptPdfService";
        private const string PdfArtifactTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalReceiptPdfArtifact";
        private const string ViewModelFactoryTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalReceiptViewModelFactory";
        private const string ViewModelTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalReceiptViewModel";

        /// <summary>
        /// Fiscal delivery accepts a loaded fiscal document only. It constructs the immutable
        /// view model and sends it to the fiscal PDF service; it must not receive mutable
        /// donation, user, food-bank, tenant or configuration state.
        /// </summary>
        [Fact]
        public void FiscalReceiptDeliveryService_UsesOnlyFiscalDocumentAndImmutablePdfPipeline()
        {
            Type deliveryServiceType = GetRequiredWebType(DeliveryServiceTypeName);
            Type deliveryServiceInterfaceType = GetRequiredWebType(DeliveryServiceInterfaceTypeName);
            Type pdfServiceType = GetRequiredRepositoryType(PdfServiceTypeName);
            Type pdfArtifactType = GetRequiredRepositoryType(PdfArtifactTypeName);
            Type viewModelFactoryType = GetRequiredRepositoryType(ViewModelFactoryTypeName);

            Assert.True(deliveryServiceInterfaceType.IsAssignableFrom(deliveryServiceType));
            Assert.NotNull(deliveryServiceType.GetConstructor(new[] { viewModelFactoryType, pdfServiceType }));
            Assert.Single(
                deliveryServiceInterfaceType.GetMethods(),
                method => method.Name == "GetOrCreate" &&
                    method.ReturnType == pdfArtifactType &&
                    method.GetParameters().Select(parameter => parameter.ParameterType)
                        .SequenceEqual(new[] { typeof(FiscalDocument) }));
            AssertNoMutableRenderDependency(deliveryServiceType);
        }

        /// <summary>
        /// The download page receives the fiscal-delivery boundary and has an internal fiscal
        /// branch that receives a loaded FiscalDocument. The branch must route through that
        /// boundary rather than rebuilding the artefact from the current donation graph.
        /// </summary>
        [Fact]
        public void GenerateInvoiceEntryPoint_HasSeparateFiscalDocumentDeliveryBranch()
        {
            Type deliveryServiceInterfaceType = GetRequiredWebType(DeliveryServiceInterfaceTypeName);
            MethodInfo fiscalBranch = Assert.Single(
                typeof(GenerateInvoiceModel).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic),
                method => method.Name == "GenerateFiscalReceiptAsync" &&
                    method.ReturnType == typeof(Task<byte[]>) &&
                    method.GetParameters().Select(parameter => parameter.ParameterType)
                        .SequenceEqual(new[] { typeof(FiscalDocument) }));

            Assert.Contains(
                typeof(GenerateInvoiceModel).GetConstructors()
                    .SelectMany(constructor => constructor.GetParameters()),
                parameter => parameter.ParameterType == deliveryServiceInterfaceType);
            Assert.Equal("GenerateFiscalReceiptAsync", fiscalBranch.Name);
        }

        /// <summary>
        /// The fiscal Razor renderer receives all legally relevant values from the immutable
        /// fiscal view model: issuer and buyer snapshots, identifier/ATCUD, QR, hash control
        /// and certificate, totals, and cancellation/correction presentation.
        /// </summary>
        [Fact]
        public void FiscalReceiptTemplateModel_ContainsAllPersistedFiscalPresentationFields()
        {
            Type viewModelType = GetRequiredRepositoryType(ViewModelTypeName);

            AssertRequiredProperties(
                viewModelType,
                "IssuerName",
                "IssuerTaxRegistrationNumber",
                "IssuerLegalAddress",
                "BuyerName",
                "BuyerTaxRegistrationNumber",
                "DocumentIdentifier",
                "Atcud",
                "QrPng",
                "HashControl",
                "SoftwareCertificateNumber",
                "NetTotal",
                "TaxTotal",
                "GrossTotal",
                "IsCancelled",
                "CancellationReason",
                "IsCorrection",
                "OriginalDocumentIdentifier");
        }

        /// <summary>
        /// The existing download method remains the legacy Invoice path. It retains its public
        /// donation-id/tenant signature and has no FiscalDocument input, so issuing fiscal
        /// documents cannot change a legacy invoice into a fiscal re-render.
        /// </summary>
        [Fact]
        public void GenerateInvoiceEntryPoint_PreservesLegacyInvoiceMethodContract()
        {
            MethodInfo legacyMethod = Assert.Single(
                typeof(GenerateInvoiceModel).GetMethods(BindingFlags.Instance | BindingFlags.Public),
                method => method.Name == "GenerateInvoiceInternalAsync" &&
                    method.GetParameters().Select(parameter => parameter.ParameterType)
                        .SequenceEqual(new[] { typeof(string), typeof(Tenant), typeof(bool) }));

            Assert.DoesNotContain(
                legacyMethod.GetParameters(),
                parameter => parameter.ParameterType == typeof(FiscalDocument));
        }

        private static void AssertNoMutableRenderDependency(Type serviceType)
        {
            string[] prohibitedTypeNames =
            {
                "BancoAlimentar.AlimentaEstaIdeia.Model.ApplicationDbContext",
                "BancoAlimentar.AlimentaEstaIdeia.Model.Donation",
                "BancoAlimentar.AlimentaEstaIdeia.Model.Identity.WebUser",
                "BancoAlimentar.AlimentaEstaIdeia.Model.FoodBank",
                "BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Tenant",
                "Microsoft.Extensions.Configuration.IConfiguration",
                "BancoAlimentar.AlimentaEstaIdeia.Web.Pages.IViewRenderService",
            };

            Assert.DoesNotContain(
                serviceType.GetConstructors().SelectMany(constructor => constructor.GetParameters()),
                parameter => prohibitedTypeNames.Contains(parameter.ParameterType.FullName, StringComparer.Ordinal));
            Assert.DoesNotContain(
                serviceType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public),
                field => prohibitedTypeNames.Contains(field.FieldType.FullName, StringComparer.Ordinal));
        }

        private static void AssertRequiredProperties(Type type, params string[] propertyNames)
        {
            foreach (string propertyName in propertyNames)
            {
                Assert.NotNull(type.GetProperty(propertyName));
            }
        }

        private static Type GetRequiredRepositoryType(string typeName)
        {
            return Assert.IsAssignableFrom<Type>(typeof(IFiscalSigningKeyProvider).Assembly.GetType(typeName));
        }

        private static Type GetRequiredWebType(string typeName)
        {
            return Assert.IsAssignableFrom<Type>(typeof(GenerateInvoiceModel).Assembly.GetType(typeName));
        }
    }
}
