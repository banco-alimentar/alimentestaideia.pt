// -----------------------------------------------------------------------
// <copyright file="FiscalDocumentKindTddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata;
    using Xunit;

    /// <summary>
    /// Defines TDD contracts for fiscal document scope and document-kind validation.
    /// </summary>
    public class FiscalDocumentKindTddTests
    {
        /// <summary>
        /// A fiscal document stores the strategy used for its issuance so a historical
        /// document cannot be reinterpreted when tenant configuration changes.
        /// </summary>
        [Fact]
        public void FiscalDocument_ModelPersistsInvoicingStrategy()
        {
            using var context = CreateContext();
            IProperty strategy = GetFiscalDocumentEntity(context).FindProperty("InvoicingStrategy");

            Assert.NotNull(strategy);
            Assert.Equal(typeof(InvoicingStrategy), strategy.ClrType);
        }

        /// <summary>
        /// The FoodBank boundary is nullable because it applies only to the
        /// MultipleTablesPerFoodBank strategy, but it must remain persisted on the document.
        /// </summary>
        [Fact]
        public void FiscalDocument_ModelPersistsNullableFoodBankId()
        {
            using var context = CreateContext();
            IProperty foodBankId = GetFiscalDocumentEntity(context).FindProperty("FoodBankId");

            Assert.NotNull(foodBankId);
            Assert.Equal(typeof(int?), foodBankId.ClrType);
            Assert.True(foodBankId.IsNullable);
        }

        /// <summary>
        /// An original document cannot refer to another fiscal document as its origin.
        /// </summary>
        [Fact]
        public void SaveChanges_RejectsOriginalDocumentWithOriginalReference()
        {
            using var context = CreateContext();
            context.FiscalDocuments.Add(CreateValidFiscalDocument(FiscalDocumentKind.Original, 100));

            Assert.ThrowsAny<Exception>(() => context.SaveChanges());
        }

        /// <summary>
        /// Corrections and credits must preserve a reference to the fiscal document they
        /// change; neither kind can be persisted as a second original.
        /// </summary>
        /// <param name="kind">The corrective document kind.</param>
        [Theory]
        [InlineData(FiscalDocumentKind.Correction)]
        [InlineData(FiscalDocumentKind.Credit)]
        public void SaveChanges_RejectsCorrectiveDocumentWithoutOriginalReference(FiscalDocumentKind kind)
        {
            using var context = CreateContext();
            context.FiscalDocuments.Add(CreateValidFiscalDocument(kind));

            Assert.ThrowsAny<Exception>(() => context.SaveChanges());
        }

        /// <summary>
        /// The one-original-per-donation unique index is restricted by document kind, not
        /// by whether an arbitrary correction happens to have an original reference.
        /// </summary>
        [Fact]
        public void FiscalDocument_OriginalDonationUniqueIndexIsRestrictedToOriginalKind()
        {
            using var context = CreateContext();
            IIndex originalDonationIndex = Assert.Single(
                GetFiscalDocumentEntity(context).GetIndexes(),
                index => index.IsUnique &&
                    index.Properties.Select(property => property.Name).SequenceEqual(new[] { "DonationId" }));

            Assert.Contains("Kind", originalDonationIndex.GetFilter(), StringComparison.Ordinal);
            Assert.DoesNotContain("OriginalFiscalDocumentId", originalDonationIndex.GetFilter(), StringComparison.Ordinal);
        }

        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private static IEntityType GetFiscalDocumentEntity(ApplicationDbContext context)
        {
            return Assert.IsAssignableFrom<IEntityType>(context.Model.FindEntityType(typeof(FiscalDocument)));
        }

        private static FiscalDocument CreateValidFiscalDocument(
            FiscalDocumentKind kind,
            int? originalFiscalDocumentId = null)
        {
            return new FiscalDocument
            {
                DonationId = 101,
                SeriesId = 102,
                FiscalIssuerProfileId = 103,
                OriginalFiscalDocumentId = originalFiscalDocumentId,
                Kind = kind,
                Status = FiscalDocumentStatus.Issued,
                Sequence = 1,
                DocumentNumber = "RC TEST 1",
                Atcud = "TEST-1",
                IssuedAtUtc = DateTimeOffset.UtcNow,
                Hash = "hash",
                HashControl = "1",
                SigningKeyVersion = "v1",
                QrPayload = "A:PT*B:501234567",
                SoftwareCertificateNumber = "0",
                IssuerLegalName = "Emitente de teste",
                IssuerTaxRegistrationNumber = "501234567",
                IssuerLegalAddress = "Rua de teste, 1, Lisboa",
                TaxRule = "M00",
                NetTotal = 10m,
                TaxTotal = 0m,
                GrossTotal = 10m,
            };
        }
    }
}
