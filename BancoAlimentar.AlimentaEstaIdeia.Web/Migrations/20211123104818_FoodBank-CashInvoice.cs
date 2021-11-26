// -----------------------------------------------------------------------
// <copyright file="20211123104818_FoodBank-CashInvoice.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

    /// <summary>
    /// Entity framework core migration.
    /// </summary>
    public partial class FoodBankCashInvoice : Migration
    {
        /// <summary>
        /// Going up in the migration.
        /// </summary>
        /// <param name="migrationBuilder">Migration builder.</param>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReceiptHeader",
                table: "FoodBanks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptName",
                table: "FoodBanks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptPlace",
                table: "FoodBanks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptSignatureImg",
                table: "FoodBanks",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <summary>
        /// Going down in the migration.
        /// </summary>
        /// <param name="migrationBuilder">Migration builder.</param>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceiptHeader",
                table: "FoodBanks");

            migrationBuilder.DropColumn(
                name: "ReceiptName",
                table: "FoodBanks");

            migrationBuilder.DropColumn(
                name: "ReceiptPlace",
                table: "FoodBanks");

            migrationBuilder.DropColumn(
                name: "ReceiptSignatureImg",
                table: "FoodBanks");
        }
    }
}
