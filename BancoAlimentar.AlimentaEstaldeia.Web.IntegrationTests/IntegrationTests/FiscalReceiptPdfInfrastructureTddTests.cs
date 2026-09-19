// -----------------------------------------------------------------------
// <copyright file="FiscalReceiptPdfInfrastructureTddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Pages;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services.Invoices;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Defines concrete Razor/PDF and Azure-blob infrastructure contracts for fiscal receipts.
    /// </summary>
    public sealed class FiscalReceiptPdfInfrastructureTddTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string BlobStoreTypeName = "BancoAlimentar.AlimentaEstaIdeia.Web.Services.Invoices.FiscalReceiptPdfBlobStore";
        private const string RazorRendererTypeName = "BancoAlimentar.AlimentaEstaIdeia.Web.Services.Invoices.RazorFiscalReceiptPdfRenderer";
        private readonly CustomWebApplicationFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="FiscalReceiptPdfInfrastructureTddTests"/> class.
        /// </summary>
        /// <param name="factory">The web integration host.</param>
        public FiscalReceiptPdfInfrastructureTddTests(CustomWebApplicationFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// The concrete renderer owns Razor-to-PDF conversion of the immutable fiscal receipt
        /// model. It uses the application's existing Razor render service rather than current
        /// donation, tenant or user data, and fulfils the synchronous renderer port consumed by
        /// <see cref="FiscalReceiptPdfService"/>.
        /// </summary>
        [Fact]
        public void RazorFiscalReceiptPdfRenderer_ImplementsFiscalRendererThroughRazorInfrastructure()
        {
            Type rendererType = GetRequiredWebType(RazorRendererTypeName);

            Assert.True(typeof(IFiscalReceiptPdfRenderer).IsAssignableFrom(rendererType));
            Assert.Contains(
                rendererType.GetConstructors().SelectMany(constructor => constructor.GetParameters()),
                parameter => parameter.ParameterType == typeof(IViewRenderService));
            Assert.DoesNotContain(
                rendererType.GetConstructors().SelectMany(constructor => constructor.GetParameters()),
                parameter => parameter.ParameterType.Name == "Donation" ||
                    parameter.ParameterType.Name == "WebUser" ||
                    parameter.ParameterType.Name == "Tenant");
        }

        /// <summary>
        /// The Azure-backed store follows existing application configuration conventions. Its
        /// interface permits only create/load: no overwrite or delete API can be used to replace
        /// an issued PDF, while <see cref="FiscalReceiptPdfService"/> verifies loaded bytes with
        /// the document's persisted SHA-256 checksum.
        /// </summary>
        [Fact]
        public void FiscalReceiptPdfBlobStore_UsesConfiguredAzureStorageAndExposesImmutablePort()
        {
            Type blobStoreType = GetRequiredWebType(BlobStoreTypeName);

            Assert.True(typeof(IFiscalReceiptPdfBlobStore).IsAssignableFrom(blobStoreType));
            Assert.Contains(
                blobStoreType.GetConstructors().SelectMany(constructor => constructor.GetParameters()),
                parameter => parameter.ParameterType == typeof(IConfiguration));
            Assert.DoesNotContain(
                blobStoreType.GetMethods(),
                method => method.Name.Contains("Delete", StringComparison.OrdinalIgnoreCase) ||
                    method.Name.Contains("Overwrite", StringComparison.OrdinalIgnoreCase));
            Assert.Equal(
                new[] { "Load", "Save" },
                typeof(IFiscalReceiptPdfBlobStore).GetMethods().Select(method => method.Name).OrderBy(name => name));
        }

        /// <summary>
        /// The production host resolves the complete immutable fiscal PDF delivery graph. This
        /// catches missing registrations before a paid fiscal document reaches the receipt route.
        /// </summary>
        [Fact]
        public void DependencyInjection_ResolvesConcreteFiscalPdfRendererBlobStoreAndDeliveryGraph()
        {
            using IServiceScope scope = this.factory.Services.CreateScope();
            IServiceProvider services = scope.ServiceProvider;

            Assert.IsAssignableFrom<IFiscalReceiptPdfRenderer>(services.GetRequiredService<IFiscalReceiptPdfRenderer>());
            Assert.IsAssignableFrom<IFiscalReceiptPdfBlobStore>(services.GetRequiredService<IFiscalReceiptPdfBlobStore>());
            Assert.NotNull(services.GetRequiredService<FiscalReceiptPdfService>());
            Assert.IsAssignableFrom<IFiscalReceiptDeliveryService>(services.GetRequiredService<IFiscalReceiptDeliveryService>());
        }

        private static Type GetRequiredWebType(string typeName)
        {
            return Assert.IsAssignableFrom<Type>(typeof(Program).Assembly.GetType(typeName));
        }
    }
}
