// -----------------------------------------------------------------------
// <copyright file="20260919134255_FiscalDocumentSigningSnapshot.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable disable

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

    /// <inheritdoc />
    public partial class FiscalDocumentSigningSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SigningAlgorithm",
                table: "FiscalDocuments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "SigningKeyIdentifier",
                table: "FiscalDocuments",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: string.Empty);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SigningAlgorithm",
                table: "FiscalDocuments");

            migrationBuilder.DropColumn(
                name: "SigningKeyIdentifier",
                table: "FiscalDocuments");
        }
    }
}
