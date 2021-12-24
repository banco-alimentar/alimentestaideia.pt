// -----------------------------------------------------------------------
// <copyright file="20211224152053_AddCampaignReportEnd.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations
{
    using System;
    using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable

    /// <summary>
    /// add a campaign report end date.
    /// </summary>
    public partial class AddCampaignReportEnd : Migration
    {
        /// <summary>
        /// Move Up.
        /// </summary>
        /// <param name="migrationBuilder">migrationBuilder.</param>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReportEnd",
                table: "Campaigns",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <summary>
        /// Move Down migration.
        /// </summary>
        /// <param name="migrationBuilder">migrationBuilder.</param>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReportEnd",
                table: "Campaigns");
        }
    }
}
