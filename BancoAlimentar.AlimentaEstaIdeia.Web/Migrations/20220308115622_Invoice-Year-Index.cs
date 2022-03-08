// -----------------------------------------------------------------------
// <copyright file="20220308115622_Invoice-Year-Index.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable disable

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

    /// <summary>
    /// Adds new year column.
    /// </summary>
    public partial class InvoiceYearIndex : Migration
    {
        /// <summary>
        /// Migrate up.
        /// </summary>
        /// <param name="migrationBuilder">migrationBuilder.</param>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.DropIndex(
                name: "IX_Invoices_Sequence_FoodBank_Id",
                table: "Invoices");

            migrationBuilder.CreateIndex(
                "IX_Invoices_Sequence_FoodBank_Id_Year",
                "Invoices",
                columns: new string[] { "Sequence", "FoodBankId", "Year" },
                unique: true);
        }

        /// <summary>
        /// Migrate down.
        /// </summary>
        /// <param name="migrationBuilder">migration builder.</param>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Year",
                table: "Invoices");

            migrationBuilder.DropIndex(
               name: "IX_Invoices_Sequence_FoodBank_Id_Year",
               table: "Invoices");

            migrationBuilder.CreateIndex(
                "IX_Invoices_Sequence_FoodBank_Id",
                "Invoices",
                columns: new string[] { "Sequence", "FoodBankId" },
                unique: true);
        }
    }
}
