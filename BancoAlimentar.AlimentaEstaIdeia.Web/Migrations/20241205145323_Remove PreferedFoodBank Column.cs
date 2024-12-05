// -----------------------------------------------------------------------
// <copyright file="20241205145323_Remove PreferedFoodBank Column.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable disable

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

    /// <inheritdoc />
    public partial class RemovePreferedFoodBankColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Donations_ConfirmedPaymentId",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "PreferedFoodBank",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_Donations_ConfirmedPaymentId",
                table: "Donations",
                column: "ConfirmedPaymentId",
                unique: true,
                filter: "[ConfirmedPaymentId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Donations_ConfirmedPaymentId",
                table: "Donations");

            migrationBuilder.AddColumn<string>(
                name: "PreferedFoodBank",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Donations_ConfirmedPaymentId",
                table: "Donations",
                column: "ConfirmedPaymentId");
        }
    }
}
