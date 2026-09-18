// -----------------------------------------------------------------------
// <copyright file="FiscalFoundationValidationTddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata;
    using Xunit;

    /// <summary>
    /// Defines TDD coverage for fiscal issuer and series isolation rules.
    /// </summary>
    public class FiscalFoundationValidationTddTests
    {
        /// <summary>
        /// A document must be constrained to a series owned by the same issuer profile.
        /// This requires a composite foreign key rather than independent single-column
        /// foreign keys that permit a cross-issuer series reference.
        /// </summary>
        [Fact]
        public void FiscalDocument_ModelConstrainsSeriesToItsIssuerProfile()
        {
            using var context = CreateContext();
            IEntityType fiscalDocument = context.Model.FindEntityType(typeof(FiscalDocument));

            IForeignKey seriesConstraint = Assert.Single(
                fiscalDocument.GetForeignKeys(),
                foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(FiscalDocumentSeries) &&
                    foreignKey.Properties.Select(property => property.Name)
                        .SequenceEqual(new[] { "SeriesId", "FiscalIssuerProfileId" }));

            Assert.Equal(
                new[] { "Id", "FiscalIssuerProfileId" },
                seriesConstraint.PrincipalKey.Properties.Select(property => property.Name));
        }

        /// <summary>
        /// A FoodBank-scoped issuer is invalid until its FoodBank boundary is configured.
        /// </summary>
        [Fact]
        public void SaveChanges_RejectsFoodBankScopedIssuerWithoutFoodBankId()
        {
            using var context = CreateContext();
            context.FiscalIssuerProfiles.Add(CreateIssuerProfile(FiscalScopeKind.FoodBank));

            Assert.ThrowsAny<Exception>(() => context.SaveChanges());
        }

        /// <summary>
        /// A tenant-scoped issuer cannot silently become FoodBank-scoped through a supplied
        /// FoodBank identifier.
        /// </summary>
        [Fact]
        public void SaveChanges_RejectsTenantScopedIssuerWithFoodBankId()
        {
            using var context = CreateContext();
            var foodBank = new FoodBank
            {
                Id = 9001,
                Name = "Banco Alimentar de Teste",
            };
            context.FoodBanks.Add(foodBank);
            context.FiscalIssuerProfiles.Add(CreateIssuerProfile(FiscalScopeKind.Tenant, foodBank.Id));

            Assert.ThrowsAny<Exception>(() => context.SaveChanges());
        }

        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private static FiscalIssuerProfile CreateIssuerProfile(FiscalScopeKind scopeKind, int? foodBankId = null)
        {
            return new FiscalIssuerProfile
            {
                ScopeKind = scopeKind,
                FoodBankId = foodBankId,
                LegalName = "Emitente de teste",
                LegalAddress = "Rua de teste, 1, Lisboa",
                TaxRegistrationNumber = "501234567",
                SaftDocumentType = "RC",
                TaxRule = "M00",
                SoftwareCertificateNumber = "0",
                SigningKeyIdentifier = "test-key",
                SigningKeyVersion = "v1",
                IsActive = true,
                ValidFromUtc = DateTimeOffset.UtcNow,
            };
        }
    }
}
