using Microsoft.EntityFrameworkCore.Migrations;

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations
{
    public partial class DonationPayment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Donations_DonationId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_DonationId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DonationId",
                table: "Payments");

            migrationBuilder.AddColumn<int>(
                name: "PaymentId",
                table: "Donations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Donations_PaymentId",
                table: "Donations",
                column: "PaymentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Donations_Payments_PaymentId",
                table: "Donations",
                column: "PaymentId",
                principalTable: "Payments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Donations_Payments_PaymentId",
                table: "Donations");

            migrationBuilder.DropIndex(
                name: "IX_Donations_PaymentId",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "PaymentId",
                table: "Donations");

            migrationBuilder.AddColumn<int>(
                name: "DonationId",
                table: "Payments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_DonationId",
                table: "Payments",
                column: "DonationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Donations_DonationId",
                table: "Payments",
                column: "DonationId",
                principalTable: "Donations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
