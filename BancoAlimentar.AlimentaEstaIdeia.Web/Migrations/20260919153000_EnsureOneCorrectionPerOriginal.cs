// -----------------------------------------------------------------------
// <copyright file="20260919153000_EnsureOneCorrectionPerOriginal.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable disable

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

    /// <inheritdoc />
    public partial class EnsureOneCorrectionPerOriginal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FiscalDocuments_OriginalFiscalDocumentId",
                table: "FiscalDocuments");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDocuments_OriginalFiscalDocumentId",
                table: "FiscalDocuments",
                column: "OriginalFiscalDocumentId",
                unique: true,
                filter: "[Kind] = 1 /* Correction */");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FiscalDocuments_OriginalFiscalDocumentId",
                table: "FiscalDocuments");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDocuments_OriginalFiscalDocumentId",
                table: "FiscalDocuments",
                column: "OriginalFiscalDocumentId");
        }
    }
}
