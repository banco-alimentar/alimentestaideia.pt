using Microsoft.EntityFrameworkCore.Migrations;

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations
{
    public partial class PaymentItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentItem_Donations_DonationId",
                table: "PaymentItem");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentItem_Payments_PaymentId",
                table: "PaymentItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PaymentItem",
                table: "PaymentItem");

            migrationBuilder.RenameTable(
                name: "PaymentItem",
                newName: "PaymentItems");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentItem_PaymentId",
                table: "PaymentItems",
                newName: "IX_PaymentItems_PaymentId");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentItem_DonationId",
                table: "PaymentItems",
                newName: "IX_PaymentItems_DonationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PaymentItems",
                table: "PaymentItems",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentItems_Donations_DonationId",
                table: "PaymentItems",
                column: "DonationId",
                principalTable: "Donations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentItems_Payments_PaymentId",
                table: "PaymentItems",
                column: "PaymentId",
                principalTable: "Payments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentItems_Donations_DonationId",
                table: "PaymentItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentItems_Payments_PaymentId",
                table: "PaymentItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PaymentItems",
                table: "PaymentItems");

            migrationBuilder.RenameTable(
                name: "PaymentItems",
                newName: "PaymentItem");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentItems_PaymentId",
                table: "PaymentItem",
                newName: "IX_PaymentItem_PaymentId");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentItems_DonationId",
                table: "PaymentItem",
                newName: "IX_PaymentItem_DonationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PaymentItem",
                table: "PaymentItem",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentItem_Donations_DonationId",
                table: "PaymentItem",
                column: "DonationId",
                principalTable: "Donations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentItem_Payments_PaymentId",
                table: "PaymentItem",
                column: "PaymentId",
                principalTable: "Payments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
