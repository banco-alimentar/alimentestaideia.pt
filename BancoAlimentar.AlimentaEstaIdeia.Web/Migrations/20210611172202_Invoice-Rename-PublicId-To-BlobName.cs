// -----------------------------------------------------------------------
// <copyright file="20210611172202_Invoice-Rename-PublicId-To-BlobName.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

/// <summary>
/// Entity framework core migration.
/// </summary>
public partial class InvoiceRenamePublicIdToBlobName : Migration
{
    /// <summary>
    /// Going up in the migration.
    /// </summary>
    /// <param name="migrationBuilder">Migration builder.</param>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "InvoicePublicId",
            table: "Invoices",
            newName: "BlobName");
    }

    /// <summary>
    /// Going down in the migration.
    /// </summary>
    /// <param name="migrationBuilder">Migration builder.</param>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "BlobName",
            table: "Invoices",
            newName: "InvoicePublicId");
    }
}
