// -----------------------------------------------------------------------
// <copyright file="20260602120000_DonationItem-RequireDonationId.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable disable

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.EntityFrameworkCore.Migrations;

    /// <summary>
    /// Removes orphan donation line items and requires every item to belong to a donation.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260602120000_DonationItem-RequireDonationId")]
    public partial class DonationItemRequireDonationId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM DonationItems WHERE DonationId IS NULL;");

            migrationBuilder.DropForeignKey(
                name: "FK_DonationItems_Donations_DonationId",
                table: "DonationItems");

            migrationBuilder.AlterColumn<int>(
                name: "DonationId",
                table: "DonationItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DonationItems_Donations_DonationId",
                table: "DonationItems",
                column: "DonationId",
                principalTable: "Donations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DonationItems_Donations_DonationId",
                table: "DonationItems");

            migrationBuilder.AlterColumn<int>(
                name: "DonationId",
                table: "DonationItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_DonationItems_Donations_DonationId",
                table: "DonationItems",
                column: "DonationId",
                principalTable: "Donations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
