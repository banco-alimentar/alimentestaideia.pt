// -----------------------------------------------------------------------
// <copyright file="20260615074144_Donation-AddPeriodoOficial.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable disable

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

    /// <summary>
    /// Adds <see cref="BancoAlimentar.AlimentaEstaIdeia.Model.Donation.PeriodoOficial"/> to donations.
    /// </summary>
    public partial class DonationAddPeriodoOficial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PeriodoOficial",
                table: "Donations",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PeriodoOficial",
                table: "Donations");
        }
    }
}
