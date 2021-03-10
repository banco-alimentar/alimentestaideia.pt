using Microsoft.EntityFrameworkCore.Migrations;

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations
{
    public partial class DonationItemPrice : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "DonationItems",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Price",
                table: "DonationItems");
        }
    }
}
