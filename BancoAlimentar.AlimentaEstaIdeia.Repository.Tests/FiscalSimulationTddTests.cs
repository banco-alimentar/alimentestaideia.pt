// -----------------------------------------------------------------------
// <copyright file="FiscalSimulationTddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.EntityFrameworkCore.Metadata;
    using Microsoft.Extensions.Options;
    using Moq;
    using Xunit;

    /// <summary>
    /// Defines the development-only simulation boundary. Simulated evidence is intentionally a
    /// different aggregate and must never inherit fiscal identifiers, signatures or SAF-T output.
    /// </summary>
    public sealed class FiscalSimulationTddTests
    {
        private const string SimulationOptionsTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalSimulationOptions";
        private const string IssuanceModeTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalIssuanceMode";
        private const string ModeResolverInterfaceTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.IFiscalIssuanceModeResolver";
        private const string ModeResolverTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalIssuanceModeResolver";
        private const string SimulatedDocumentTypeName = "BancoAlimentar.AlimentaEstaIdeia.Model.SimulatedFiscalDocument";
        private const string SimulatedSequenceTypeName = "BancoAlimentar.AlimentaEstaIdeia.Model.SimulatedFiscalSequence";
        private const string SimulatedReceiptViewModelTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.SimulatedFiscalReceiptViewModel";
        private const string SimulatedReceiptViewModelFactoryTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.SimulatedFiscalReceiptViewModelFactory";
        private const string SimulatedQrBuilderInterfaceTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.ISimulatedReceiptQrPayloadBuilder";
        private const string SimulatedQrBuilderTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.SimulatedReceiptQrPayloadBuilder";
        private const string RequiredSimulationNotice = "SIMULAÇÃO — SEM VALIDADE FISCAL";

        /// <summary>
        /// A missing or false option leaves the real fiscal path selected. The resolver must use
        /// only host configuration and the host environment, not request, tenant or user input.
        /// </summary>
        [Fact]
        public void Resolve_MissingOrDisabledOptionUsesRealFiscalPath()
        {
            object missingOptionResolver = CreateModeResolver("Development", enabled: false);
            object disabledOptionResolver = CreateModeResolver("Development", enabled: false);

            Assert.Equal("Real", ResolveIssuanceMode(missingOptionResolver).ToString());
            Assert.Equal("Real", ResolveIssuanceMode(disabledOptionResolver).ToString());
        }

        /// <summary>
        /// Simulation has one explicit and case-sensitive activation condition: enabled in the
        /// exact Development environment. This prevents a similarly named deployment from
        /// issuing a non-fiscal artifact under an ambiguous policy.
        /// </summary>
        [Fact]
        public void Resolve_EnabledOptionUsesSimulationOnlyInExactDevelopment()
        {
            object resolver = CreateModeResolver("Development", enabled: true);

            Assert.Equal("Simulation", ResolveIssuanceMode(resolver).ToString());
        }

        /// <summary>
        /// An enabled simulation option outside Development is an unsafe deployment error, not a
        /// request-time switch and not a fallback to the real fiscal workflow.
        /// </summary>
        /// <param name="environmentName">The prohibited host environment name.</param>
        [Theory]
        [InlineData("Testing")]
        [InlineData("Staging")]
        [InlineData("PreProduction")]
        [InlineData("Production")]
        [InlineData("development")]
        [InlineData("UnknownEnvironment")]
        public void Resolve_EnabledOptionOutsideExactDevelopmentFailsClosed(string environmentName)
        {
            object resolver = CreateModeResolver(environmentName, enabled: true);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => ResolveIssuanceMode(resolver));

            Assert.Contains("Simulation", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Development", exception.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Simulated documents and counters are persisted separately from FiscalDocument and its
        /// AT series. A counter is unique within a synthetic scope and an issued simulation is
        /// idempotent per donation; together with the captured strategy and FoodBank boundary,
        /// this supports both invoice-table strategies without consuming an AT sequence.
        /// </summary>
        [Fact]
        public void SimulationPersistence_SeparatesCountersAndDocumentsForBothInvoicingStrategies()
        {
            Type simulatedDocumentType = GetRequiredModelType(SimulatedDocumentTypeName);
            Type simulatedSequenceType = GetRequiredModelType(SimulatedSequenceTypeName);
            using var context = CreateContext();

            Assert.Equal(
                typeof(DbSet<>).MakeGenericType(simulatedDocumentType),
                typeof(ApplicationDbContext).GetProperty("SimulatedFiscalDocuments")?.PropertyType);
            Assert.Equal(
                typeof(DbSet<>).MakeGenericType(simulatedSequenceType),
                typeof(ApplicationDbContext).GetProperty("SimulatedFiscalSequences")?.PropertyType);

            AssertRequiredProperties(
                simulatedDocumentType,
                "DonationId",
                "ScopeKey",
                "InvoicingStrategy",
                "FoodBankId",
                "Sequence",
                "SyntheticIdentifier",
                "LocalChecksum",
                "SimulationQrPayload",
                "ArtifactReference",
                "OriginEnvironment");
            AssertRequiredProperties(
                simulatedSequenceType,
                "ScopeKey",
                "InvoicingStrategy",
                "FoodBankId",
                "NextSequence");
            Assert.Equal(typeof(InvoicingStrategy), simulatedDocumentType.GetProperty("InvoicingStrategy")?.PropertyType);
            Assert.Equal(typeof(int?), simulatedDocumentType.GetProperty("FoodBankId")?.PropertyType);

            AssertUniqueIndex(context, simulatedDocumentType, "DonationId");
            AssertUniqueIndex(context, simulatedDocumentType, "ScopeKey", "Sequence");
            AssertUniqueIndex(context, simulatedSequenceType, "ScopeKey");
            IModel designTimeModel = context.GetService<IDesignTimeModel>().Model;
            Assert.Contains(
                designTimeModel.FindEntityType(simulatedDocumentType).GetCheckConstraints(),
                constraint => constraint.Name.Contains("InvoicingStrategy", StringComparison.Ordinal) &&
                    constraint.Sql.Contains("FoodBankId", StringComparison.Ordinal));
        }

        /// <summary>
        /// A simulated document must be incapable of carrying fiscal evidence. Its synthetic
        /// identifier and local checksum deliberately use different names from ATCUD and hash
        /// fields so it cannot be re-labelled as a certified receipt later.
        /// </summary>
        [Fact]
        public void SimulatedFiscalDocument_HasNoFiscalAtcudSigningOrAtCommunicationFields()
        {
            Type simulatedDocumentType = GetRequiredModelType(SimulatedDocumentTypeName);

            string[] forbiddenFiscalProperties =
            {
                "Atcud",
                "AtValidationCode",
                "FiscalHash",
                "Hash",
                "HashControl",
                "PreviousHash",
                "SigningKeyIdentifier",
                "SigningKeyVersion",
                "SigningAlgorithm",
                "SoftwareCertificateNumber",
                "AtCommunicationReference",
            };

            foreach (string propertyName in forbiddenFiscalProperties)
            {
                Assert.Null(simulatedDocumentType.GetProperty(propertyName));
            }
        }

        /// <summary>
        /// The simulation QR contract is private and versioned. It must render the mandatory
        /// warning through a dedicated receipt model and never use the AT QR grammar or label.
        /// </summary>
        [Fact]
        public void SimulatedReceipt_UsesMandatoryWarningAndPrivateVersionedQrPayload()
        {
            Type simulatedDocumentType = GetRequiredModelType(SimulatedDocumentTypeName);
            Type receiptViewModelType = GetRequiredRepositoryType(SimulatedReceiptViewModelTypeName);
            Type factoryType = GetRequiredRepositoryType(SimulatedReceiptViewModelFactoryTypeName);
            Type qrBuilderInterfaceType = GetRequiredRepositoryType(SimulatedQrBuilderInterfaceTypeName);
            Type qrBuilderType = GetRequiredRepositoryType(SimulatedQrBuilderTypeName);
            object simulatedDocument = CreateSimulatedDocument(simulatedDocumentType, "SIM-TENANT-7-1");

            Assert.True(qrBuilderInterfaceType.IsAssignableFrom(qrBuilderType));
            MethodInfo create = Assert.Single(
                factoryType.GetMethods(BindingFlags.Instance | BindingFlags.Public),
                method => method.Name == "Create" &&
                    method.ReturnType == receiptViewModelType &&
                    method.GetParameters().Select(parameter => parameter.ParameterType)
                        .SequenceEqual(new[] { simulatedDocumentType }));
            MethodInfo build = Assert.Single(
                qrBuilderInterfaceType.GetMethods(),
                method => method.Name == "Build" &&
                    method.ReturnType == typeof(string) &&
                    method.GetParameters().Select(parameter => parameter.ParameterType)
                        .SequenceEqual(new[] { simulatedDocumentType }));

            object receiptViewModel = create.Invoke(Activator.CreateInstance(factoryType), new[] { simulatedDocument });
            string notice = Assert.IsType<string>(receiptViewModelType.GetProperty("SimulationNotice")?.GetValue(receiptViewModel));
            string qrPayload = Assert.IsType<string>(build.Invoke(Activator.CreateInstance(qrBuilderType), new[] { simulatedDocument }));

            Assert.Equal(RequiredSimulationNotice, notice);
            Assert.StartsWith("AEI-SIM/v1", qrPayload, StringComparison.Ordinal);
            Assert.Contains("SIM-TENANT-7-1", qrPayload, StringComparison.Ordinal);
            Assert.DoesNotContain("ATCUD", qrPayload, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("A:", qrPayload, StringComparison.Ordinal);
        }

        /// <summary>
        /// The official SAF-T service exports only FiscalDocument records. Even if a simulation
        /// is persisted beside the tenant's real documents, its synthetic identifier cannot enter
        /// an official export.
        /// </summary>
        [Fact]
        public void SaftExport_ExcludesPersistedSimulationDocuments()
        {
            Type simulatedDocumentType = GetRequiredModelType(SimulatedDocumentTypeName);
            using var context = CreateContext();
            object simulatedDocument = CreateSimulatedDocument(simulatedDocumentType, "SIM-MUST-NOT-ENTER-SAFT");
            context.Add(simulatedDocument);
            context.SaveChanges();
            var service = new SaftExportService(context);

            string xml = service.Export(new SaftExportRequest
            {
                Tenant = new Tenant
                {
                    Id = 7,
                    InvoicingStrategy = InvoicingStrategy.SingleInvoiceTable,
                    Name = "Simulation test tenant",
                },
                FiscalIssuerProfileId = 1,
                StartDate = new DateOnly(2026, 1, 1),
                EndDate = new DateOnly(2026, 12, 31),
            });

            Assert.DoesNotContain("SIM-MUST-NOT-ENTER-SAFT", xml, StringComparison.Ordinal);
            Assert.DoesNotContain(RequiredSimulationNotice, xml, StringComparison.Ordinal);
        }

        private static object CreateModeResolver(string environmentName, bool enabled)
        {
            Type optionsType = GetRequiredRepositoryType(SimulationOptionsTypeName);
            Type resolverType = GetRequiredRepositoryType(ModeResolverTypeName);
            Type resolverInterfaceType = GetRequiredRepositoryType(ModeResolverInterfaceTypeName);
            object options = Activator.CreateInstance(optionsType);
            optionsType.GetProperty("Enabled")?.SetValue(options, enabled);
            Type optionsInterfaceType = typeof(IOptions<>).MakeGenericType(optionsType);
            object wrappedOptions = typeof(Options)
                .GetMethod(nameof(Options.Create), BindingFlags.Public | BindingFlags.Static)
                .MakeGenericMethod(optionsType)
                .Invoke(null, new[] { options });
            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(item => item.EnvironmentName).Returns(environmentName);
            ConstructorInfo constructor = resolverType.GetConstructor(
                new[] { optionsInterfaceType, typeof(IWebHostEnvironment) });

            Assert.True(resolverInterfaceType.IsAssignableFrom(resolverType));
            Assert.NotNull(constructor);
            return constructor.Invoke(new[] { wrappedOptions, environment.Object });
        }

        private static object ResolveIssuanceMode(object resolver)
        {
            Type modeType = GetRequiredRepositoryType(IssuanceModeTypeName);
            Type resolverInterfaceType = GetRequiredRepositoryType(ModeResolverInterfaceTypeName);
            MethodInfo resolve = Assert.Single(
                resolverInterfaceType.GetMethods(),
                method => method.Name == "Resolve" && method.ReturnType == modeType &&
                    method.GetParameters().Length == 0);
            try
            {
                return resolve.Invoke(resolver, Array.Empty<object>());
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
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

        private static object CreateSimulatedDocument(Type simulatedDocumentType, string syntheticIdentifier)
        {
            object simulatedDocument = Activator.CreateInstance(simulatedDocumentType);
            SetRequiredProperty(simulatedDocument, "DonationId", 7001);
            SetRequiredProperty(simulatedDocument, "ScopeKey", "tenant:7");
            SetRequiredProperty(simulatedDocument, "InvoicingStrategy", InvoicingStrategy.SingleInvoiceTable);
            SetRequiredProperty(simulatedDocument, "FoodBankId", null);
            SetRequiredProperty(simulatedDocument, "Sequence", 1);
            SetRequiredProperty(simulatedDocument, "SyntheticIdentifier", syntheticIdentifier);
            SetRequiredProperty(simulatedDocument, "LocalChecksum", "local-checksum");
            SetRequiredProperty(simulatedDocument, "SimulationQrPayload", "AEI-SIM/v1;id=" + syntheticIdentifier);
            SetRequiredProperty(simulatedDocument, "ArtifactReference", "simulation/development/tenant-7/" + syntheticIdentifier + ".pdf");
            SetRequiredProperty(simulatedDocument, "OriginEnvironment", "Development");
            return simulatedDocument;
        }

        private static void AssertRequiredProperties(Type type, params string[] propertyNames)
        {
            foreach (string propertyName in propertyNames)
            {
                Assert.NotNull(type.GetProperty(propertyName));
            }
        }

        private static void AssertUniqueIndex(ApplicationDbContext context, Type entityType, params string[] propertyNames)
        {
            Assert.Contains(
                context.Model.FindEntityType(entityType).GetIndexes(),
                index => index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual(propertyNames));
        }

        private static void SetRequiredProperty(object instance, string propertyName, object value)
        {
            PropertyInfo property = instance.GetType().GetProperty(propertyName);
            Assert.NotNull(property);
            property.SetValue(instance, value);
        }

        private static Type GetRequiredModelType(string typeName)
        {
            return Assert.IsAssignableFrom<Type>(typeof(ApplicationDbContext).Assembly.GetType(typeName));
        }

        private static Type GetRequiredRepositoryType(string typeName)
        {
            return Assert.IsAssignableFrom<Type>(typeof(IFiscalScopeResolver).Assembly.GetType(typeName));
        }
    }
}
