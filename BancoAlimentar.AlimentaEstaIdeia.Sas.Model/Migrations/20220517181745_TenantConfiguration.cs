// -----------------------------------------------------------------------
// <copyright file="20220517181745_TenantConfiguration.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable disable

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Migrations
{
    using System;
    using Microsoft.EntityFrameworkCore.Migrations;

    /// <summary>
    /// Entity framework core migration.
    /// </summary>
    public partial class TenantConfiguration : Migration
    {
        /// <summary>
        /// Going up in the migration.
        /// </summary>
        /// <param name="migrationBuilder">Migration builder.</param>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResourceGroupId = table.Column<Guid>(type: "nvarchar(max)", nullable: true),
                    ServicePrincipalId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KeyVaultSecretId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeploymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantConfigurations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantConfigurations_TenantId",
                table: "TenantConfigurations",
                column: "TenantId");
        }

        /// <summary>
        /// Going down in the migration.
        /// </summary>
        /// <param name="migrationBuilder">Migration builder.</param>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantConfigurations");
        }
    }
}
