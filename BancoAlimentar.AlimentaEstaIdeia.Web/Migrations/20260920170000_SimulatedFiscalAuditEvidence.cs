// -----------------------------------------------------------------------
// <copyright file="20260920170000_SimulatedFiscalAuditEvidence.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable disable

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations
{
    using System;
    using Microsoft.EntityFrameworkCore.Migrations;

    /// <inheritdoc />
    public partial class SimulatedFiscalAuditEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "SimulatedFiscalDocuments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SimulatedFiscalDocuments",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SimulatedFiscalAuditEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    FoodBankId = table.Column<int>(type: "int", nullable: true),
                    SimulatedFiscalDocumentId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Result = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ActorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulatedFiscalAuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SimulatedFiscalAuditEvents_SimulatedFiscalDocuments_SimulatedFiscalDocumentId",
                        column: x => x.SimulatedFiscalDocumentId,
                        principalTable: "SimulatedFiscalDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SimulatedFiscalDocuments_TenantId_FoodBankId_IssuedAtUtc",
                table: "SimulatedFiscalDocuments",
                columns: new[] { "TenantId", "FoodBankId", "IssuedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SimulatedFiscalAuditEvents_SimulatedFiscalDocumentId",
                table: "SimulatedFiscalAuditEvents",
                column: "SimulatedFiscalDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_SimulatedFiscalAuditEvents_TenantId_OccurredAtUtc",
                table: "SimulatedFiscalAuditEvents",
                columns: new[] { "TenantId", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SimulatedFiscalAuditEvents");

            migrationBuilder.DropIndex(
                name: "IX_SimulatedFiscalDocuments_TenantId_FoodBankId_IssuedAtUtc",
                table: "SimulatedFiscalDocuments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SimulatedFiscalDocuments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SimulatedFiscalDocuments");
        }
    }
}
