// -----------------------------------------------------------------------
// <copyright file="20260918232109_FiscalDocumentFoundation.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable disable

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations
{
    using System;
    using Microsoft.EntityFrameworkCore.Migrations;

    /// <inheritdoc />
    public partial class FiscalDocumentFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FiscalIssuerProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScopeKind = table.Column<int>(type: "int", nullable: false),
                    FoodBankId = table.Column<int>(type: "int", nullable: true),
                    LegalName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LegalAddress = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    TaxRegistrationNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SaftDocumentType = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    TaxRule = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SoftwareCertificateNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SigningKeyIdentifier = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    SigningKeyVersion = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ValidFromUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ValidToUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalIssuerProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiscalIssuerProfiles_FoodBanks_FoodBankId",
                        column: x => x.FoodBankId,
                        principalTable: "FoodBanks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FiscalDocumentSeries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FiscalIssuerProfileId = table.Column<int>(type: "int", nullable: false),
                    SeriesCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SaftDocumentType = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ProcessingMethod = table.Column<int>(type: "int", nullable: false),
                    FirstSequence = table.Column<int>(type: "int", nullable: false),
                    NextSequence = table.Column<int>(type: "int", nullable: false),
                    LastSequence = table.Column<int>(type: "int", nullable: true),
                    AtcudValidationCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AtCommunicationReference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalDocumentSeries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiscalDocumentSeries_FiscalIssuerProfiles_FiscalIssuerProfileId",
                        column: x => x.FiscalIssuerProfileId,
                        principalTable: "FiscalIssuerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FiscalDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonationId = table.Column<int>(type: "int", nullable: false),
                    SeriesId = table.Column<int>(type: "int", nullable: false),
                    FiscalIssuerProfileId = table.Column<int>(type: "int", nullable: false),
                    OriginalFiscalDocumentId = table.Column<int>(type: "int", nullable: true),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    DocumentNumber = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Atcud = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IssuedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    HashControl = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PreviousHash = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    SigningKeyVersion = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    QrPayload = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    SoftwareCertificateNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IssuerLegalName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IssuerTaxRegistrationNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IssuerLegalAddress = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    DonorName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DonorTaxRegistrationNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TaxRule = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NetTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GrossTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PdfBlobName = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PdfChecksum = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiscalDocuments_Donations_DonationId",
                        column: x => x.DonationId,
                        principalTable: "Donations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FiscalDocuments_FiscalDocumentSeries_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "FiscalDocumentSeries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FiscalDocuments_FiscalDocuments_OriginalFiscalDocumentId",
                        column: x => x.OriginalFiscalDocumentId,
                        principalTable: "FiscalDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FiscalDocuments_FiscalIssuerProfiles_FiscalIssuerProfileId",
                        column: x => x.FiscalIssuerProfileId,
                        principalTable: "FiscalIssuerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FiscalAuditEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    FoodBankId = table.Column<int>(type: "int", nullable: true),
                    FiscalIssuerProfileId = table.Column<int>(type: "int", nullable: true),
                    FiscalDocumentSeriesId = table.Column<int>(type: "int", nullable: true),
                    FiscalDocumentId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Result = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ActorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalAuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiscalAuditEvents_FiscalDocumentSeries_FiscalDocumentSeriesId",
                        column: x => x.FiscalDocumentSeriesId,
                        principalTable: "FiscalDocumentSeries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FiscalAuditEvents_FiscalDocuments_FiscalDocumentId",
                        column: x => x.FiscalDocumentId,
                        principalTable: "FiscalDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FiscalAuditEvents_FiscalIssuerProfiles_FiscalIssuerProfileId",
                        column: x => x.FiscalIssuerProfileId,
                        principalTable: "FiscalIssuerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FiscalDocumentEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FiscalDocumentId = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ActorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalDocumentEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiscalDocumentEvents_FiscalDocuments_FiscalDocumentId",
                        column: x => x.FiscalDocumentId,
                        principalTable: "FiscalDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FiscalDocumentLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FiscalDocumentId = table.Column<int>(type: "int", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    ProductCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    TaxExemptionReason = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NetTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GrossTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalDocumentLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiscalDocumentLines_FiscalDocuments_FiscalDocumentId",
                        column: x => x.FiscalDocumentId,
                        principalTable: "FiscalDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FiscalAuditEvents_FiscalDocumentId",
                table: "FiscalAuditEvents",
                column: "FiscalDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalAuditEvents_FiscalDocumentSeriesId",
                table: "FiscalAuditEvents",
                column: "FiscalDocumentSeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalAuditEvents_FiscalIssuerProfileId",
                table: "FiscalAuditEvents",
                column: "FiscalIssuerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalAuditEvents_TenantId_OccurredAtUtc",
                table: "FiscalAuditEvents",
                columns: new[] { "TenantId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDocumentEvents_FiscalDocumentId_OccurredAtUtc",
                table: "FiscalDocumentEvents",
                columns: new[] { "FiscalDocumentId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDocumentLines_FiscalDocumentId_LineNumber",
                table: "FiscalDocumentLines",
                columns: new[] { "FiscalDocumentId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDocuments_Atcud",
                table: "FiscalDocuments",
                column: "Atcud",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDocuments_DonationId",
                table: "FiscalDocuments",
                column: "DonationId",
                unique: true,
                filter: "[OriginalFiscalDocumentId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDocuments_FiscalIssuerProfileId",
                table: "FiscalDocuments",
                column: "FiscalIssuerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDocuments_OriginalFiscalDocumentId",
                table: "FiscalDocuments",
                column: "OriginalFiscalDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDocuments_SeriesId_Sequence",
                table: "FiscalDocuments",
                columns: new[] { "SeriesId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDocumentSeries_FiscalIssuerProfileId_AtcudValidationCode",
                table: "FiscalDocumentSeries",
                columns: new[] { "FiscalIssuerProfileId", "AtcudValidationCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDocumentSeries_FiscalIssuerProfileId_SaftDocumentType_SeriesCode",
                table: "FiscalDocumentSeries",
                columns: new[] { "FiscalIssuerProfileId", "SaftDocumentType", "SeriesCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FiscalIssuerProfiles_FoodBankId",
                table: "FiscalIssuerProfiles",
                column: "FoodBankId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalIssuerProfiles_ScopeKind_FoodBankId_ValidFromUtc",
                table: "FiscalIssuerProfiles",
                columns: new[] { "ScopeKind", "FoodBankId", "ValidFromUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FiscalAuditEvents");

            migrationBuilder.DropTable(
                name: "FiscalDocumentEvents");

            migrationBuilder.DropTable(
                name: "FiscalDocumentLines");

            migrationBuilder.DropTable(
                name: "FiscalDocuments");

            migrationBuilder.DropTable(
                name: "FiscalDocumentSeries");

            migrationBuilder.DropTable(
                name: "FiscalIssuerProfiles");
        }
    }
}
