namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

    public partial class DonationPaymentStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaymentStatus",
                table: "Donations",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "Donations");
        }
    }
}
