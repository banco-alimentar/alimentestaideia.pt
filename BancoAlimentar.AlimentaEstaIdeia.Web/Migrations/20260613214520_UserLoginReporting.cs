// -----------------------------------------------------------------------
// <copyright file="20260613214520_UserLoginReporting.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable disable

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations
{
    using System;
    using Microsoft.EntityFrameworkCore.Migrations;

    /// <summary>
    /// Adds user login event tracking and registration metadata for reporting.
    /// </summary>
    public partial class UserLoginReporting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredAtUtc",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RegistrationCampaignId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationLoginProvider",
                table: "AspNetUsers",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserLoginEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CampaignId = table.Column<int>(type: "int", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLoginEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLoginEvents_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserLoginEvents_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginEvents_CampaignId_LoginProvider",
                table: "UserLoginEvents",
                columns: new[] { "CampaignId", "LoginProvider" });

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginEvents_OccurredAtUtc",
                table: "UserLoginEvents",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginEvents_UserId",
                table: "UserLoginEvents",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserLoginEvents");

            migrationBuilder.DropColumn(
                name: "RegisteredAtUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RegistrationCampaignId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RegistrationLoginProvider",
                table: "AspNetUsers");
        }
    }
}
