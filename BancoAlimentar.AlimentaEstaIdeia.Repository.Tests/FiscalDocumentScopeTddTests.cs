// -----------------------------------------------------------------------
// <copyright file="FiscalDocumentScopeTddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using Microsoft.EntityFrameworkCore;
    using Xunit;

    /// <summary>
    /// Defines TDD coverage for the fiscal document scope captured at issuance.
    /// </summary>
    public class FiscalDocumentScopeTddTests
    {
        /// <summary>
        /// A document emitted through the tenant-wide strategy has no FoodBank boundary.
        /// </summary>
        [Fact]
        public void SaveChanges_RejectsSingleInvoiceTableDocumentWithFoodBankId()
        {
            using var context = CreateContext();
            context.FiscalDocuments.Add(CreateFiscalDocument(
                1,
                FiscalDocumentKind.Original,
                InvoicingStrategy.SingleInvoiceTable,
                foodBankId: 10));

            Assert.ThrowsAny<Exception>(() => context.SaveChanges());
        }

        /// <summary>
        /// A document emitted through the FoodBank strategy must capture the FoodBank that
        /// forms its fiscal boundary.
        /// </summary>
        [Fact]
        public void SaveChanges_RejectsMultipleTablesPerFoodBankDocumentWithoutFoodBankId()
        {
            using var context = CreateContext();
            context.FiscalDocuments.Add(CreateFiscalDocument(
                1,
                FiscalDocumentKind.Original,
                InvoicingStrategy.MultipleTablesPerFoodBank));

            Assert.ThrowsAny<Exception>(() => context.SaveChanges());
        }

        /// <summary>
        /// Corrections and credits can reference only an original document, not another
        /// corrective document.
        /// </summary>
        /// <param name="kind">The corrective document kind being tested.</param>
        [Theory]
        [InlineData(FiscalDocumentKind.Correction)]
        [InlineData(FiscalDocumentKind.Credit)]
        public void SaveChanges_RejectsCorrectiveDocumentWhoseTargetIsNotOriginal(FiscalDocumentKind kind)
        {
            using var context = CreateContext();
            FiscalDocument original = PersistOriginal(context, 1);
            var correction = CreateFiscalDocument(
                2,
                FiscalDocumentKind.Correction,
                InvoicingStrategy.SingleInvoiceTable,
                originalFiscalDocumentId: original.Id);
            correction.DonationId = original.DonationId;
            correction.FiscalIssuerProfileId = original.FiscalIssuerProfileId;
            context.FiscalDocuments.Add(correction);
            context.SaveChanges();

            context.FiscalDocuments.Add(CreateFiscalDocument(
                3,
                kind,
                InvoicingStrategy.SingleInvoiceTable,
                originalFiscalDocumentId: correction.Id));

            Assert.ThrowsAny<Exception>(() => context.SaveChanges());
        }

        /// <summary>
        /// Corrections and credits cannot cross a donation boundary, even when the selected
        /// target is an original document.
        /// </summary>
        /// <param name="kind">The corrective document kind being tested.</param>
        [Theory]
        [InlineData(FiscalDocumentKind.Correction)]
        [InlineData(FiscalDocumentKind.Credit)]
        public void SaveChanges_RejectsCorrectiveDocumentWithDifferentDonationThanOriginal(FiscalDocumentKind kind)
        {
            using var context = CreateContext();
            FiscalDocument original = PersistOriginal(context, 1);
            FiscalDocument candidate = CreateFiscalDocument(
                2,
                kind,
                InvoicingStrategy.SingleInvoiceTable,
                originalFiscalDocumentId: original.Id);
            candidate.DonationId = original.DonationId + 1;
            candidate.FiscalIssuerProfileId = original.FiscalIssuerProfileId;
            context.FiscalDocuments.Add(candidate);

            Assert.ThrowsAny<Exception>(() => context.SaveChanges());
        }

        /// <summary>
        /// Corrections and credits cannot cross the issuer profile captured by their target
        /// original document.
        /// </summary>
        /// <param name="kind">The corrective document kind being tested.</param>
        [Theory]
        [InlineData(FiscalDocumentKind.Correction)]
        [InlineData(FiscalDocumentKind.Credit)]
        public void SaveChanges_RejectsCorrectiveDocumentWithDifferentIssuerThanOriginal(FiscalDocumentKind kind)
        {
            using var context = CreateContext();
            FiscalDocument original = PersistOriginal(context, 1);
            FiscalDocument candidate = CreateFiscalDocument(
                2,
                kind,
                InvoicingStrategy.SingleInvoiceTable,
                originalFiscalDocumentId: original.Id);
            candidate.DonationId = original.DonationId;
            candidate.FiscalIssuerProfileId = original.FiscalIssuerProfileId + 1;
            context.FiscalDocuments.Add(candidate);

            Assert.ThrowsAny<Exception>(() => context.SaveChanges());
        }

        /// <summary>
        /// Corrections and credits cannot cross the invoicing strategy captured by their
        /// target original document.
        /// </summary>
        /// <param name="kind">The corrective document kind being tested.</param>
        [Theory]
        [InlineData(FiscalDocumentKind.Correction)]
        [InlineData(FiscalDocumentKind.Credit)]
        public void SaveChanges_RejectsCorrectiveDocumentWithDifferentInvoicingStrategyThanOriginal(FiscalDocumentKind kind)
        {
            using var context = CreateContext();
            FiscalDocument original = PersistOriginal(context, 1);
            FiscalDocument candidate = CreateFiscalDocument(
                2,
                kind,
                InvoicingStrategy.MultipleTablesPerFoodBank,
                foodBankId: 100,
                originalFiscalDocumentId: original.Id);
            candidate.DonationId = original.DonationId;
            candidate.FiscalIssuerProfileId = original.FiscalIssuerProfileId;
            context.FiscalDocuments.Add(candidate);

            Assert.ThrowsAny<Exception>(() => context.SaveChanges());
        }

        /// <summary>
        /// Corrections and credits cannot cross the persisted FoodBank boundary of their
        /// target original document.
        /// </summary>
        /// <param name="kind">The corrective document kind being tested.</param>
        [Theory]
        [InlineData(FiscalDocumentKind.Correction)]
        [InlineData(FiscalDocumentKind.Credit)]
        public void SaveChanges_RejectsCorrectiveDocumentWithDifferentFoodBankScopeThanOriginal(FiscalDocumentKind kind)
        {
            using var context = CreateContext();
            FiscalDocument original = PersistOriginal(
                context,
                1,
                InvoicingStrategy.MultipleTablesPerFoodBank,
                foodBankId: 100);
            FiscalDocument candidate = CreateFiscalDocument(
                2,
                kind,
                InvoicingStrategy.MultipleTablesPerFoodBank,
                foodBankId: 101,
                originalFiscalDocumentId: original.Id);
            candidate.DonationId = original.DonationId;
            candidate.FiscalIssuerProfileId = original.FiscalIssuerProfileId;
            context.FiscalDocuments.Add(candidate);

            Assert.ThrowsAny<Exception>(() => context.SaveChanges());
        }

        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private static FiscalDocument PersistOriginal(
            ApplicationDbContext context,
            int uniqueValue,
            InvoicingStrategy strategy = InvoicingStrategy.SingleInvoiceTable,
            int? foodBankId = null)
        {
            FiscalDocument original = CreateFiscalDocument(
                uniqueValue,
                FiscalDocumentKind.Original,
                strategy,
                foodBankId);
            context.FiscalDocuments.Add(original);
            context.SaveChanges();
            return original;
        }

        private static FiscalDocument CreateFiscalDocument(
            int uniqueValue,
            FiscalDocumentKind kind,
            InvoicingStrategy strategy,
            int? foodBankId = null,
            int? originalFiscalDocumentId = null)
        {
            return new FiscalDocument
            {
                DonationId = 1000 + uniqueValue,
                SeriesId = 2000 + uniqueValue,
                FiscalIssuerProfileId = 3000 + uniqueValue,
                InvoicingStrategy = strategy,
                FoodBankId = foodBankId,
                OriginalFiscalDocumentId = originalFiscalDocumentId,
                Kind = kind,
                Status = FiscalDocumentStatus.Issued,
                Sequence = uniqueValue,
                DocumentNumber = $"RC TEST {uniqueValue}",
                Atcud = $"TEST-{uniqueValue}",
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
