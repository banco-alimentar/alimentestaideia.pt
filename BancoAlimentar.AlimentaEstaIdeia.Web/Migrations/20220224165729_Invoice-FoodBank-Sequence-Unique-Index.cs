// -----------------------------------------------------------------------
// <copyright file="20220224165729_Invoice-FoodBank-Sequence-Unique-Index.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable disable

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

/// <summary>
/// Multiple column indexes.
/// </summary>
public partial class InvoiceFoodBankSequenceUniqueIndex : Migration
{
    /// <summary>
    /// Migrate up.
    /// </summary>
    /// <param name="migrationBuilder">migrationBuilder.</param>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Invoices_Sequence",
            table: "Invoices");

        migrationBuilder.CreateIndex(
            "IX_Invoices_Sequence_FoodBank_Id",
            "Invoices",
            columns: new string[] { "Sequence", "FoodBankId" },
            unique: true);
    }

    /// <summary>
    /// Migrate down.
    /// </summary>
    /// <param name="migrationBuilder">migration builder.</param>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_Invoices_Sequence",
            table: "Invoices",
            column: "Sequence",
            unique: true);

        migrationBuilder.DropIndex(
            name: "IX_Invoices_Sequence_FoodBank_Id",
            table: "Invoices");
    }
}
