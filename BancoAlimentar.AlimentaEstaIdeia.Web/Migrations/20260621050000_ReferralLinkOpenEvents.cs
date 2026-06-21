// -----------------------------------------------------------------------
// <copyright file="20260621050000_ReferralLinkOpenEvents.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable disable

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.EntityFrameworkCore.Migrations;

    /// <summary>
    /// Adds per-event storage for referral link opens.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260621050000_ReferralLinkOpenEvents")]
    public partial class ReferralLinkOpenEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReferralLinkOpens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReferralId = table.Column<int>(type: "int", nullable: false),
                    OpenedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferralLinkOpens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReferralLinkOpens_Referrals_ReferralId",
                        column: x => x.ReferralId,
                        principalTable: "Referrals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReferralLinkOpens_OpenedAtUtc",
                table: "ReferralLinkOpens",
                column: "OpenedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ReferralLinkOpens_ReferralId_OpenedAtUtc",
                table: "ReferralLinkOpens",
                columns: new[] { "ReferralId", "OpenedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReferralLinkOpens");
        }
    }
}
