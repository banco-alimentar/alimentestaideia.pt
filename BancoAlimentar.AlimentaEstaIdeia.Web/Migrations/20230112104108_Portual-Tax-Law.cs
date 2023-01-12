// -----------------------------------------------------------------------
// <copyright file="20230112104108_Portual-Tax-Law.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable disable

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

    /// <inheritdoc />
    public partial class PortualTaxLaw : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Referral",
                table: "Donations");

            migrationBuilder.AddColumn<string>(
                name: "ATCUD",
                table: "FoodBanks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HashCypher",
                table: "FoodBanks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoftwareCertificateNumber",
                table: "FoodBanks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxRegistrationNumber",
                table: "FoodBanks",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ATCUD",
                table: "FoodBanks");

            migrationBuilder.DropColumn(
                name: "HashCypher",
                table: "FoodBanks");

            migrationBuilder.DropColumn(
                name: "SoftwareCertificateNumber",
                table: "FoodBanks");

            migrationBuilder.DropColumn(
                name: "TaxRegistrationNumber",
                table: "FoodBanks");

            migrationBuilder.AddColumn<string>(
                name: "Referral",
                table: "Donations",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
