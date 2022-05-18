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

#pragma warning disable SA1118 // Parameter should not span multiple lines

            migrationBuilder.InsertData(
                table: "Tenants",
                columns: new[] { "Id", "Created", "InvoiceConfigurationId", "InvoicingStrategy", "Name", "PaymentStrategy", "PublicId" },
                values: new object[,]
                {
                    { 5, new DateTime(2022, 5, 18, 17, 11, 59, 395, DateTimeKind.Utc).AddTicks(7283), null, "MultipleTablesPerFoodBank", "localhost", "IndividualPaymentProcessorPerFoodBank", new Guid("9d46682c-588b-45ce-8829-f8ce771dc10e") },
                    { 7, new DateTime(2022, 5, 18, 17, 11, 59, 395, DateTimeKind.Utc).AddTicks(7288), null, "MultipleTablesPerFoodBank", "bancoalimentar", "SharedPaymentProcessor", new Guid("2d4d6448-71d3-454a-a584-9ebfc0b7ede5") },
                    { 8, new DateTime(2022, 5, 18, 17, 11, 59, 395, DateTimeKind.Utc).AddTicks(7334), null, "SingleInvoiceTable", "alimentaestaideia-beta", "SharedPaymentProcessor", new Guid("bd31d165-b8df-4c7a-a5e3-5e3d155948e2") },
                    { 9, new DateTime(2022, 5, 18, 17, 11, 59, 395, DateTimeKind.Utc).AddTicks(7335), null, "SingleInvoiceTable", "alimentaestaideia-beta", "SharedPaymentProcessor", new Guid("de68a683-0cd2-44ce-b9c6-505aabbcdfc3") },
                    { 10, new DateTime(2022, 5, 18, 17, 11, 59, 395, DateTimeKind.Utc).AddTicks(7336), null, "SingleInvoiceTable", "doar-dev.alimentestaideia.pt", "SharedPaymentProcessor", new Guid("03317653-9140-4cc0-91e0-2b2aa2a8e5fe") },
                    { 13, new DateTime(2022, 5, 18, 17, 11, 59, 395, DateTimeKind.Utc).AddTicks(7338), null, "SingleInvoiceTable", "doar-dev.alimentestaideia-dev.pt", "SharedPaymentProcessor", new Guid("f3aea354-b2ad-4451-893f-891dfb2c6c99") },
                    { 14, new DateTime(2022, 5, 18, 17, 11, 59, 395, DateTimeKind.Utc).AddTicks(7340), null, "SingleInvoiceTable", "alimentaestaideia-developer.azurewebsites.net", "SharedPaymentProcessor", new Guid("f904b771-b750-4392-a8f6-f76a8b9cc1be") },
                });

            migrationBuilder.InsertData(
                table: "DomainIdentifiers",
                columns: new[] { "Id", "Created", "DomainName", "Environment", "TenantId" },
                values: new object[,]
                {
                    { 3, new DateTime(2022, 5, 18, 17, 11, 59, 395, DateTimeKind.Utc).AddTicks(7495), "localhost", "Development", 5 },
                    { 4, new DateTime(2022, 5, 18, 17, 11, 59, 395, DateTimeKind.Utc).AddTicks(7495), "doar-dev.bancoalimentar.pt", "Development", 7 },
                    { 5, new DateTime(2022, 5, 18, 17, 11, 59, 395, DateTimeKind.Utc).AddTicks(7496), "alimentaestaideia-beta.azurewebsites.net", "Staging", 10 },
                    { 6, new DateTime(2022, 5, 18, 17, 11, 59, 395, DateTimeKind.Utc).AddTicks(7496), "doar-dev.alimentestaideia.pt", "Staging", 10 },
                    { 7, new DateTime(2022, 5, 18, 17, 11, 59, 395, DateTimeKind.Utc).AddTicks(7497), "alimentaestaideia-developer.azurewebsites.net", "Staging", 14 },
                    { 8, new DateTime(2022, 5, 18, 17, 11, 59, 395, DateTimeKind.Utc).AddTicks(7497), "alimentaestaideia-developer.azurewebsites.net", "Development", 14 },
                    { 9, new DateTime(2022, 5, 18, 17, 11, 59, 395, DateTimeKind.Utc).AddTicks(7497), "doar-dev.bancoalimentar.pt", "Staging", 7 },
                });

            migrationBuilder.InsertData(
                table: "KeyVaultConfigurations",
                columns: new[] { "Id", "Created", "Environment", "TenantId", "Vault" },
                values: new object[,]
                {
                    { 1, new DateTime(2022, 5, 18, 17, 11, 59, 395, DateTimeKind.Utc).AddTicks(7468), "Development", 7, "doarbancoalimentar" },
                    { 7, new DateTime(2022, 5, 18, 17, 11, 59, 395, DateTimeKind.Utc).AddTicks(7470), "Development", 5, "doarbancoalimentar" },
                    { 8, new DateTime(2022, 5, 18, 17, 11, 59, 395, DateTimeKind.Utc).AddTicks(7471), "Development", 10, "doaralimentestaideia" },
                    { 9, new DateTime(2022, 5, 18, 17, 11, 59, 395, DateTimeKind.Utc).AddTicks(7472), "Development", 13, "doaralimentestaideia" },
                    { 10, new DateTime(2022, 5, 18, 17, 11, 59, 395, DateTimeKind.Utc).AddTicks(7473), "Staging", 10, "doaralimentestaideia" },
                    { 11, new DateTime(2022, 5, 18, 17, 11, 59, 395, DateTimeKind.Utc).AddTicks(7474), "Staging", 7, "doarbancoalimentar" },
                    { 12, new DateTime(2022, 5, 18, 17, 11, 59, 395, DateTimeKind.Utc).AddTicks(7475), "Staging", 14, "alimentaestaideia-key" },
                });

            migrationBuilder.InsertData(
                table: "TenantConfigurations",
                columns: new[] { "Id", "ResourceGroupId", "ServicePrincipalId", "KeyVaultSecretId", "DeploymentId", "TenantId" },
                values: new object[,]
                {
                    { 1, "/subscriptions/3cce75ab-2603-49a8-b424-a6cc19ac998f/resourceGroups/Test", "5b3c1539-cd7f-472d-a220-2bb0eec1b99b", "https://ocw.vault.azure.net/secrets/ServicePrincipalSecret/58b80768ce124a51b86230caca6087ce", null, null },
                });

#pragma warning restore SA1118 // Parameter should not span multiple lines
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
