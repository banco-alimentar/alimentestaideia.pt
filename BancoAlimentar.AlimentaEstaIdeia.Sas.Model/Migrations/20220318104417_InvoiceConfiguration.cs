// -----------------------------------------------------------------------
// <copyright file="20220318104417_InvoiceConfiguration.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable disable

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

/// <summary>
/// Entity framework core migration.
/// </summary>
public partial class InvoiceConfiguration : Migration
{
    /// <summary>
    /// Going up in the migration.
    /// </summary>
    /// <param name="migrationBuilder">Migration builder.</param>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "InvoiceConfigurationId",
            table: "Tenants",
            type: "int",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "InvoiceConfigurations",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                HeaderImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                FooterSignatureImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InvoiceConfigurations", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Tenants_InvoiceConfigurationId",
            table: "Tenants",
            column: "InvoiceConfigurationId");

        migrationBuilder.AddForeignKey(
            name: "FK_Tenants_InvoiceConfigurations_InvoiceConfigurationId",
            table: "Tenants",
            column: "InvoiceConfigurationId",
            principalTable: "InvoiceConfigurations",
            principalColumn: "Id");
    }

    /// <summary>
    /// Going down in the migration.
    /// </summary>
    /// <param name="migrationBuilder">Migration builder.</param>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Tenants_InvoiceConfigurations_InvoiceConfigurationId",
            table: "Tenants");

        migrationBuilder.DropTable(
            name: "InvoiceConfigurations");

        migrationBuilder.DropIndex(
            name: "IX_Tenants_InvoiceConfigurationId",
            table: "Tenants");

        migrationBuilder.DropColumn(
            name: "InvoiceConfigurationId",
            table: "Tenants");
    }
}
