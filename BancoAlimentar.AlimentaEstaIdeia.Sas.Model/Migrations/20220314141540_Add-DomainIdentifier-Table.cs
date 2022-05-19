// -----------------------------------------------------------------------
// <copyright file="20220314141540_Add-DomainIdentifier-Table.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable disable

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Migrations;

using System;
using Microsoft.EntityFrameworkCore.Migrations;

/// <summary>
/// Entity framework core migration.
/// </summary>
public partial class AddDomainIdentifierTable : Migration
{
    /// <summary>
    /// Going up in the migration.
    /// </summary>
    /// <param name="migrationBuilder">Migration builder.</param>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "DomainIdentifier",
            table: "Tenants");

        migrationBuilder.CreateTable(
            name: "DomainIdentifiers",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                DomainName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Environment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                TenantId = table.Column<int>(type: "int", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DomainIdentifiers", x => x.Id);
                table.ForeignKey(
                    name: "FK_DomainIdentifiers_Tenants_TenantId",
                    column: x => x.TenantId,
                    principalTable: "Tenants",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateIndex(
            name: "IX_DomainIdentifiers_TenantId",
            table: "DomainIdentifiers",
            column: "TenantId");
    }

    /// <summary>
    /// Going down in the migration.
    /// </summary>
    /// <param name="migrationBuilder">Migration builder.</param>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "DomainIdentifiers");

        migrationBuilder.AddColumn<string>(
            name: "DomainIdentifier",
            table: "Tenants",
            type: "nvarchar(max)",
            nullable: true);
    }
}
