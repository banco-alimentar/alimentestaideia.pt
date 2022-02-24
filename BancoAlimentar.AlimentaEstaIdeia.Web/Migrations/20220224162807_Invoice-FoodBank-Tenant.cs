// -----------------------------------------------------------------------
// <copyright file="20220224162807_Invoice-FoodBank-Tenant.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable disable
namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

    /// <summary>
    /// Invoice Food Banks.
    /// </summary>
    public partial class InvoiceFoodBankTenant : Migration
    {
        /// <summary>
        /// Migrate up.
        /// </summary>
        /// <param name="migrationBuilder">migrationBuilder.</param>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FoodBankId",
                table: "Invoices",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_FoodBankId",
                table: "Invoices",
                column: "FoodBankId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_FoodBanks_FoodBankId",
                table: "Invoices",
                column: "FoodBankId",
                principalTable: "FoodBanks",
                principalColumn: "Id");
        }

        /// <summary>
        /// Migrate down.
        /// </summary>
        /// <param name="migrationBuilder">migration builder.</param>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_FoodBanks_FoodBankId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_FoodBankId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "FoodBankId",
                table: "Invoices");
        }
    }
}
