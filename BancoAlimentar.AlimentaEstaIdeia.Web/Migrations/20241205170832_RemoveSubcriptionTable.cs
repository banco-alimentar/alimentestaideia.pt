// -----------------------------------------------------------------------
// <copyright file="20241205170832_RemoveSubcriptionTable.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable disable

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

    /// <inheritdoc />
    public partial class RemoveSubcriptionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubscriptionDonations");

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionId",
                table: "Donations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Donations_SubscriptionId",
                table: "Donations",
                column: "SubscriptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Donations_Subscriptions_SubscriptionId",
                table: "Donations",
                column: "SubscriptionId",
                principalTable: "Subscriptions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Donations_Subscriptions_SubscriptionId",
                table: "Donations");

            migrationBuilder.DropIndex(
                name: "IX_Donations_SubscriptionId",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "SubscriptionId",
                table: "Donations");

            migrationBuilder.CreateTable(
                name: "SubscriptionDonations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonationId = table.Column<int>(type: "int", nullable: true),
                    SubscriptionId = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionDonations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionDonations_Donations_DonationId",
                        column: x => x.DonationId,
                        principalTable: "Donations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SubscriptionDonations_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "Subscriptions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionDonations_DonationId",
                table: "SubscriptionDonations",
                column: "DonationId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionDonations_SubscriptionId",
                table: "SubscriptionDonations",
                column: "SubscriptionId");
        }
    }
}
