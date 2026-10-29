// -----------------------------------------------------------------------
// <copyright file="20260919145428_FiscalDocumentPdfArtifactMetadata.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable disable

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations
{
    using System;
    using Microsoft.EntityFrameworkCore.Migrations;

    /// <inheritdoc />
    public partial class FiscalDocumentPdfArtifactMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PdfGeneratedAtUtc",
                table: "FiscalDocuments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PdfTemplateVersion",
                table: "FiscalDocuments",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PdfGeneratedAtUtc",
                table: "FiscalDocuments");

            migrationBuilder.DropColumn(
                name: "PdfTemplateVersion",
                table: "FiscalDocuments");
        }
    }
}
