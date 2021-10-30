// -----------------------------------------------------------------------
// <copyright file="20211030113017_Subscription-User-Id.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

    /// <summary>
    /// Entity framework core migration.
    /// </summary>
    public partial class SubscriptionUserId : Migration
    {
        /// <summary>
        /// Going up in the migration.
        /// </summary>
        /// <param name="migrationBuilder">Migration builder.</param>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Subscriptions",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_UserId",
                table: "Subscriptions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_AspNetUsers_UserId",
                table: "Subscriptions",
                column: "UserId",
                principalTable: "AspNetUsers",
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
                name: "FK_Subscriptions_AspNetUsers_UserId",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_UserId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Subscriptions");
        }
    }
}
