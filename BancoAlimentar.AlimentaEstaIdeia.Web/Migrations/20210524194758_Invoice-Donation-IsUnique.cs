// -----------------------------------------------------------------------
// <copyright file="20210524194758_Invoice-Donation-IsUnique.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

    public partial class InvoiceDonationIsUnique : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_DonationId",
                table: "Invoices");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_DonationId",
                table: "Invoices",
                column: "DonationId",
                unique: true,
                filter: "[DonationId] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_DonationId",
                table: "Invoices");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_DonationId",
                table: "Invoices",
                column: "DonationId");
        }
    }
}
