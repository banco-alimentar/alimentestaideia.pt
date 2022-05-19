// -----------------------------------------------------------------------
// <copyright file="20210520092614_include_referral.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

/// <summary>
/// Entity framework core migration.
/// </summary>
#pragma warning disable SA1300 // Element should begin with upper-case letter
public partial class include_referral : Migration
#pragma warning restore SA1300 // Element should begin with upper-case letter
{
    /// <summary>
    /// Going up in the migration.
    /// </summary>
    /// <param name="migrationBuilder">Migration builder.</param>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "ReferralId",
            table: "Donations",
            type: "int",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "Referrals",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                Active = table.Column<bool>(type: "bit", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Referrals", x => x.Id);
                table.ForeignKey(
                    name: "FK_Referrals_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Donations_ReferralId",
            table: "Donations",
            column: "ReferralId");

        migrationBuilder.CreateIndex(
            name: "IX_Referrals_UserId",
            table: "Referrals",
            column: "UserId");

        migrationBuilder.AddForeignKey(
            name: "FK_Donations_Referrals_ReferralId",
            table: "Donations",
            column: "ReferralId",
            principalTable: "Referrals",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <summary>
    /// Going down in the migration.
    /// </summary>
    /// <param name="migrationBuilder">Migration builder.</param>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Donations_Referrals_ReferralId",
            table: "Donations");

        migrationBuilder.DropTable(
            name: "Referrals");

        migrationBuilder.DropIndex(
            name: "IX_Donations_ReferralId",
            table: "Donations");

        migrationBuilder.DropColumn(
            name: "ReferralId",
            table: "Donations");
    }
}
