// -----------------------------------------------------------------------
// <copyright file="20260920153932_SimulatedFiscalPersistence.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable disable

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations
{
    using System;
    using Microsoft.EntityFrameworkCore.Migrations;

    /// <inheritdoc />
    public partial class SimulatedFiscalPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SimulatedFiscalDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonationId = table.Column<int>(type: "int", nullable: false),
                    PaymentId = table.Column<int>(type: "int", nullable: true),
                    ScopeKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    InvoicingStrategy = table.Column<int>(type: "int", nullable: false),
                    FoodBankId = table.Column<int>(type: "int", nullable: true),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    SyntheticIdentifier = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LocalChecksum = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SimulationQrPayload = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    SimulationQrPng = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    ArtifactReference = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    OriginEnvironment = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IssuedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulatedFiscalDocuments", x => x.Id);
                    table.CheckConstraint("CK_SimulatedFiscalDocuments_InvoicingStrategy_FoodBankId", "([InvoicingStrategy] = 0 AND [FoodBankId] IS NULL) OR ([InvoicingStrategy] = 1 AND [FoodBankId] IS NOT NULL)");
                });

            migrationBuilder.CreateTable(
                name: "SimulatedFiscalSequences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScopeKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    InvoicingStrategy = table.Column<int>(type: "int", nullable: false),
                    FoodBankId = table.Column<int>(type: "int", nullable: true),
                    NextSequence = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulatedFiscalSequences", x => x.Id);
                    table.CheckConstraint("CK_SimulatedFiscalSequences_InvoicingStrategy_FoodBankId", "([InvoicingStrategy] = 0 AND [FoodBankId] IS NULL) OR ([InvoicingStrategy] = 1 AND [FoodBankId] IS NOT NULL)");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SimulatedFiscalDocuments_DonationId",
                table: "SimulatedFiscalDocuments",
                column: "DonationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SimulatedFiscalDocuments_ScopeKey_Sequence",
                table: "SimulatedFiscalDocuments",
                columns: new[] { "ScopeKey", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SimulatedFiscalDocuments_SyntheticIdentifier",
                table: "SimulatedFiscalDocuments",
                column: "SyntheticIdentifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SimulatedFiscalSequences_ScopeKey",
                table: "SimulatedFiscalSequences",
                column: "ScopeKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SimulatedFiscalDocuments");

            migrationBuilder.DropTable(
                name: "SimulatedFiscalSequences");
        }
    }
}
