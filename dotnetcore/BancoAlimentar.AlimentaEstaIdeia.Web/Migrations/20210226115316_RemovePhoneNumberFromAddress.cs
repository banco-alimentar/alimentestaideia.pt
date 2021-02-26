using Microsoft.EntityFrameworkCore.Migrations;

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations
{
    public partial class RemovePhoneNumberFromAddress : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "DonorAddresses");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "DonorAddresses",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }
    }
}
