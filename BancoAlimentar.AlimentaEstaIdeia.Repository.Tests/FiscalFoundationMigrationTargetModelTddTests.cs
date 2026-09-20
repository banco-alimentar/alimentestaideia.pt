// -----------------------------------------------------------------------
// <copyright file="FiscalFoundationMigrationTargetModelTddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Migrations;
    using Microsoft.EntityFrameworkCore.Metadata;
    using Xunit;

    /// <summary>
    /// Protects the target model used when tenant databases apply the fiscal foundation
    /// migration.
    /// </summary>
    public class FiscalFoundationMigrationTargetModelTddTests
    {
        /// <summary>
        /// The foundation migration target model must declare the CLR-backed idempotency
        /// property before creating its filtered unique index. Otherwise EF cannot generate
        /// the migration SQL for a tenant database.
        /// </summary>
        [Fact]
        public void FiscalFoundationMigration_TargetModelBuildsTypedIdempotencyKeyIndex()
        {
            var migration = new FiscalDocumentFoundation();

            IEntityType fiscalDocument = Assert.IsAssignableFrom<IEntityType>(
                migration.TargetModel.FindEntityType(typeof(FiscalDocument)));
            IProperty idempotencyKey = Assert.IsAssignableFrom<IProperty>(
                fiscalDocument.FindProperty(nameof(FiscalDocument.IdempotencyKey)));
            IIndex index = Assert.Single(
                fiscalDocument.GetIndexes(),
                candidate => candidate.Properties.Select(property => property.Name)
                    .SequenceEqual(new[] { nameof(FiscalDocument.IdempotencyKey) }));

            Assert.Equal(typeof(string), idempotencyKey.ClrType);
            Assert.True(idempotencyKey.IsNullable);
            Assert.True(index.IsUnique);
            Assert.Equal(
                "[IdempotencyKey] IS NOT NULL",
                index.FindAnnotation("Relational:Filter")?.Value);
        }
    }
}
