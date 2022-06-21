// -----------------------------------------------------------------------
// <copyright file="20220222104027_Add-Payment-Invoicing-Strategy.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable disable
namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

    /// <summary>
    /// Entity framework core migration.
    /// </summary>
    public partial class AddPaymentInvoicingStrategy : Migration
    {
        /// <summary>
        /// Going up in the migration.
        /// </summary>
        /// <param name="migrationBuilder">Migration builder.</param>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InvoicingStrategy",
                table: "Tenants",
                type: "nvarchar(180)",
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "PaymentStrategy",
                table: "Tenants",
                type: "nvarchar(180)",
                nullable: false,
                defaultValue: string.Empty);
        }

        /// <summary>
        /// Going down in the migration.
        /// </summary>
        /// <param name="migrationBuilder">Migration builder.</param>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvoicingStrategy",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "PaymentStrategy",
                table: "Tenants");
        }
    }
}
