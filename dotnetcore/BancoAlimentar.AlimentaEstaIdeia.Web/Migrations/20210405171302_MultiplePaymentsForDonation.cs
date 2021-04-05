using Microsoft.EntityFrameworkCore.Migrations;

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations
{
    public partial class MultiplePaymentsForDonation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropForeignKey(
            //    name: "FK_Payments_Donations_DonationId",
            //    table: "Payments");

            //migrationBuilder.DropIndex(
            //    name: "IX_Payments_DonationId",
            //    table: "Payments");

            //migrationBuilder.DropColumn(
            //    name: "DonationId",
            //    table: "Payments");

            migrationBuilder.CreateTable(
                name: "PaymentItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonationId = table.Column<int>(type: "int", nullable: true),
                    PaymentId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentItem_Donations_DonationId",
                        column: x => x.DonationId,
                        principalTable: "Donations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentItem_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentItem_DonationId",
                table: "PaymentItem",
                column: "DonationId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentItem_PaymentId",
                table: "PaymentItem",
                column: "PaymentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentItem");

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
