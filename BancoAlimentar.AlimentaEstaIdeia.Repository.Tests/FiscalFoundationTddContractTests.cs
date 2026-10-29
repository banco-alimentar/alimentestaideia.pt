// -----------------------------------------------------------------------
// <copyright file="FiscalFoundationTddContractTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Linq;
    using System.Reflection;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata;
    using Xunit;

    /// <summary>
    /// Defines TDD contracts for the fiscal-document foundation. These tests deliberately
    /// use reflection while the public fiscal types are being introduced so they compile
    /// before the production implementation exists.
    /// </summary>
    public class FiscalFoundationTddContractTests
    {
        private const string FiscalDocumentTypeName = "BancoAlimentar.AlimentaEstaIdeia.Model.FiscalDocument";
        private const string FiscalDocumentSeriesTypeName = "BancoAlimentar.AlimentaEstaIdeia.Model.FiscalDocumentSeries";
        private const string FiscalIssuerProfileTypeName = "BancoAlimentar.AlimentaEstaIdeia.Model.FiscalIssuerProfile";
        private const string FiscalScopeResolverTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.IFiscalScopeResolver";

        /// <summary>
        /// A fiscal document is a separate persisted entity rather than an overloaded
        /// legacy receipt. It must retain the ATCUD and its series sequence.
        /// </summary>
        [Fact]
        public void FiscalDocument_IsPersistedWithAtcudAndSeriesSequence()
        {
            Type fiscalDocumentType = GetRequiredModelType(FiscalDocumentTypeName);
            Assert.NotNull(typeof(ApplicationDbContext).GetProperty("FiscalDocuments"));
            Assert.NotNull(FindProperty(fiscalDocumentType, "Atcud"));
            Assert.NotNull(FindProperty(fiscalDocumentType, "SeriesId"));
            Assert.NotNull(FindProperty(fiscalDocumentType, "Sequence"));
        }

        /// <summary>
        /// The model protects the idempotency rule: one original fiscal document per
        /// donation. Corrections must use an explicit original-document relationship,
        /// rather than create another original for the donation.
        /// </summary>
        [Fact]
        public void FiscalDocument_ModelHasUniqueOriginalDocumentPerDonationConstraint()
        {
            using var context = CreateContext();
            IEntityType fiscalDocumentEntity = GetRequiredEntityType(context, FiscalDocumentTypeName);

            IIndex originalDocumentIndex = Assert.Single(
                fiscalDocumentEntity.GetIndexes(),
                index =>
                    index.IsUnique &&
                    index.Properties.Select(property => property.Name).SequenceEqual(new[] { "DonationId" }) &&
                    index.GetFilter() != null &&
                    index.GetFilter().Contains("Original", StringComparison.Ordinal));

            Assert.True(originalDocumentIndex.IsUnique);
        }

        /// <summary>
        /// The model makes a fiscal number unique only inside its document series.
        /// </summary>
        [Fact]
        public void FiscalDocument_ModelHasUniqueSeriesAndSequenceConstraint()
        {
            using var context = CreateContext();
            IEntityType fiscalDocumentEntity = GetRequiredEntityType(context, FiscalDocumentTypeName);

            Assert.Contains(
                fiscalDocumentEntity.GetIndexes(),
                index => index.IsUnique &&
                    index.Properties.Select(property => property.Name).SequenceEqual(new[] { "SeriesId", "Sequence" }));
        }

        /// <summary>
        /// The persisted ATCUD is unique and is built from the series validation code and
        /// sequence, in the form validation-code-sequence. The service implementation may
        /// encapsulate formatting, but the persisted model must expose all three values.
        /// </summary>
        [Fact]
        public void FiscalDocument_StoresUniqueAtcudDerivedFromSeriesValidationCodeAndSequence()
        {
            using var context = CreateContext();
            Type fiscalDocumentSeriesType = GetRequiredModelType(FiscalDocumentSeriesTypeName);
            IEntityType fiscalDocumentEntity = GetRequiredEntityType(context, FiscalDocumentTypeName);

            Assert.NotNull(FindProperty(fiscalDocumentSeriesType, "AtcudValidationCode"));
            Assert.NotNull(FindProperty(GetRequiredModelType(FiscalDocumentTypeName), "Atcud"));
            Assert.NotNull(FindProperty(GetRequiredModelType(FiscalDocumentTypeName), "Sequence"));
            Assert.Contains(
                fiscalDocumentEntity.GetIndexes(),
                index => index.IsUnique &&
                    index.Properties.Select(property => property.Name).SequenceEqual(new[] { "Atcud" }));
        }

        /// <summary>
        /// Fiscal scope resolution accepts the current tenant and donation, and exposes the
        /// strategy plus the optional FoodBank boundary required for both invoicing modes.
        /// </summary>
        [Fact]
        public void FiscalScopeResolver_ExposesTenantAndFoodBankScopeForBothInvoicingStrategies()
        {
            Type resolverType = GetRequiredRepositoryType(FiscalScopeResolverTypeName);
            MethodInfo resolveMethod = Assert.Single(
                resolverType.GetMethods(),
                method =>
                method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(new[] { typeof(Tenant), typeof(Donation) }));

            Type scopeType = UnwrapAsyncResult(resolveMethod.ReturnType);
            Assert.NotNull(FindProperty(scopeType, "TenantId"));
            Assert.NotNull(FindProperty(scopeType, "InvoicingStrategy"));
            Assert.NotNull(FindProperty(scopeType, "FoodBankId"));
            Assert.Equal(InvoicingStrategy.SingleInvoiceTable, (InvoicingStrategy)0);
            Assert.Equal(InvoicingStrategy.MultipleTablesPerFoodBank, (InvoicingStrategy)1);
        }

        /// <summary>
        /// A fiscal issuer profile exists independently of presentation configuration and
        /// provides the legal issuer identity required by a resolved scope.
        /// </summary>
        [Fact]
        public void FiscalIssuerProfile_PersistsTheIssuerTaxRegistrationNumber()
        {
            Type fiscalIssuerProfileType = GetRequiredModelType(FiscalIssuerProfileTypeName);

            Assert.NotNull(FindProperty(fiscalIssuerProfileType, "TaxRegistrationNumber"));
        }

        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private static IEntityType GetRequiredEntityType(ApplicationDbContext context, string typeName)
        {
            Type type = GetRequiredModelType(typeName);
            return Assert.IsAssignableFrom<IEntityType>(context.Model.FindEntityType(type));
        }

        private static Type GetRequiredModelType(string typeName)
        {
            return Assert.IsAssignableFrom<Type>(typeof(ApplicationDbContext).Assembly.GetType(typeName));
        }

        private static Type GetRequiredRepositoryType(string typeName)
        {
            return Assert.IsAssignableFrom<Type>(typeof(InvoiceRepository).Assembly.GetType(typeName));
        }

        private static PropertyInfo FindProperty(Type type, string propertyName)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .SingleOrDefault(property => string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase));
        }

        private static Type UnwrapAsyncResult(Type type)
        {
            return type.IsGenericType &&
                (type.GetGenericTypeDefinition() == typeof(System.Threading.Tasks.Task<>) ||
                type.GetGenericTypeDefinition() == typeof(System.Threading.Tasks.ValueTask<>))
                ? type.GetGenericArguments().Single()
                : type;
        }
    }
}
