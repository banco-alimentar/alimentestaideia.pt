// -----------------------------------------------------------------------
// <copyright file="20210707154354_ConfirmedPayment-Donation.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

    public partial class ConfirmedPaymentDonation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConfirmedPaymentId",
                table: "Donations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Donations_ConfirmedPaymentId",
                table: "Donations",
                column: "ConfirmedPaymentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Donations_Payments_ConfirmedPaymentId",
                table: "Donations",
                column: "ConfirmedPaymentId",
                principalTable: "Payments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Donations_Payments_ConfirmedPaymentId",
                table: "Donations");

            migrationBuilder.DropIndex(
                name: "IX_Donations_ConfirmedPaymentId",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "ConfirmedPaymentId",
                table: "Donations");
        }
    }
}
