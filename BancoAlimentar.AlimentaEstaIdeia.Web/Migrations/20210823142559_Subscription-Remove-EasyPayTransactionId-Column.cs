// -----------------------------------------------------------------------
// <copyright file="20210823142559_Subscription-Remove-EasyPayTransactionId-Column.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

/// <summary>
/// Entity framework core migration.
/// </summary>
public partial class SubscriptionRemoveEasyPayTransactionIdColumn : Migration
{
    /// <summary>
    /// Going up in the migration.
    /// </summary>
    /// <param name="migrationBuilder">Migration builder.</param>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "EasyPayTransactionId",
            table: "Subscriptions");
    }

    /// <summary>
    /// Going down in the migration.
    /// </summary>
    /// <param name="migrationBuilder">Migration builder.</param>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "EasyPayTransactionId",
            table: "Subscriptions",
            type: "nvarchar(max)",
            nullable: true);
    }
}
