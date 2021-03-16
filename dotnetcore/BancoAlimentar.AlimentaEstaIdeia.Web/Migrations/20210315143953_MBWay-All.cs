using Microsoft.EntityFrameworkCore.Migrations;

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations
{
    public partial class MBWayAll : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "FixedFee",
                table: "MBWayPayments",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Paid",
                table: "MBWayPayments",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Requested",
                table: "MBWayPayments",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Tax",
                table: "MBWayPayments",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Transfer",
                table: "MBWayPayments",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "VariableFee",
                table: "MBWayPayments",
                type: "real",
                nullable: false,
                defaultValue: 0f);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FixedFee",
                table: "MBWayPayments");

            migrationBuilder.DropColumn(
                name: "Paid",
                table: "MBWayPayments");

            migrationBuilder.DropColumn(
                name: "Requested",
                table: "MBWayPayments");

            migrationBuilder.DropColumn(
                name: "Tax",
                table: "MBWayPayments");

            migrationBuilder.DropColumn(
                name: "Transfer",
                table: "MBWayPayments");

            migrationBuilder.DropColumn(
                name: "VariableFee",
                table: "MBWayPayments");
        }
    }
}
