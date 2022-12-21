// -----------------------------------------------------------------------
// <copyright file="20220720163952_KeyVaultServicePrincipal.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Migrations
{
    using System;
    using Microsoft.EntityFrameworkCore.Migrations;

    /// <summary>
    /// Entity framework core migration.
    /// </summary>
    public partial class KeyVaultServicePrincipal : Migration
    {
        /// <summary>
        /// Going up in the migration.
        /// </summary>
        /// <param name="migrationBuilder">Migration builder.</param>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasServicePrincipalEnabled",
                table: "KeyVaultConfigurations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SasSPKeyVaultKeyName",
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
                name: "HasServicePrincipalEnabled",
                table: "KeyVaultConfigurations");

            migrationBuilder.DropColumn(
                name: "SasSPKeyVaultKeyName",
                table: "KeyVaultConfigurations");
        }
    }
}
