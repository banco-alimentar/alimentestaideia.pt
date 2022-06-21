// -----------------------------------------------------------------------
// <copyright file="20211228162645_Identifier.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

    /// <summary>
    /// Entity framework core migration.
    /// </summary>
    public partial class Identifier : Migration
    {
        /// <summary>
        /// Going up in the migration.
        /// </summary>
        /// <param name="migrationBuilder">Migration builder.</param>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DomainIdentifier",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Environment",
                table: "KeyVaultConfigurations",
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
                name: "DomainIdentifier",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Environment",
                table: "KeyVaultConfigurations");
        }
    }
}
