using Microsoft.EntityFrameworkCore.Migrations;

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations
{
    public partial class InvoiceDonationIsUnique : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_DonationId",
                table: "Invoices");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_DonationId",
                table: "Invoices",
                column: "DonationId",
                unique: true,
                filter: "[DonationId] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_DonationId",
                table: "Invoices");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_DonationId",
                table: "Invoices",
                column: "DonationId");
        }
    }
}
