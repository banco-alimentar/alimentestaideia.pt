using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations
{
    public partial class Payments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PayPalPayments_Donations_DonationId",
                table: "PayPalPayments");

            migrationBuilder.DropTable(
                name: "CreditCardPayments");

            migrationBuilder.DropTable(
                name: "MBWayPayments");

            migrationBuilder.DropTable(
                name: "MultiBankPayments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PayPalPayments",
                table: "PayPalPayments");

            migrationBuilder.RenameTable(
                name: "PayPalPayments",
                newName: "Payments");

            migrationBuilder.RenameIndex(
                name: "IX_PayPalPayments_DonationId",
                table: "Payments",
                newName: "IX_Payments_DonationId");

            migrationBuilder.AlterColumn<decimal>(
                name: "DonationAmount",
                table: "Donations",
                type: "decimal(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)");

            migrationBuilder.AddColumn<string>(
                name: "Alias",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EasyPayPaymentId",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "FixedFee",
                table: "Payments",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Message",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "Paid",
                table: "Payments",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "Requested",
                table: "Payments",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "Tax",
                table: "Payments",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransactionKey",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "Transfer",
                table: "Payments",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "VariableFee",
                table: "Payments",
                type: "real",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Payments",
                table: "Payments",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Donations_DonationId",
                table: "Payments",
                column: "DonationId",
                principalTable: "Donations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Donations_DonationId",
                table: "Payments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Payments",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Alias",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "EasyPayPaymentId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "FixedFee",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Message",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Paid",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Requested",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Tax",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "TransactionKey",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Transfer",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Url",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "VariableFee",
                table: "Payments");

            migrationBuilder.RenameTable(
                name: "Payments",
                newName: "PayPalPayments");

            migrationBuilder.RenameIndex(
                name: "IX_Payments_DonationId",
                table: "PayPalPayments",
                newName: "IX_PayPalPayments_DonationId");

            migrationBuilder.AlterColumn<decimal>(
                name: "DonationAmount",
                table: "Donations",
                type: "decimal(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PayPalPayments",
                table: "PayPalPayments",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "CreditCardPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DonationId = table.Column<int>(type: "int", nullable: true),
                    EasyPayPaymentId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FixedFee = table.Column<float>(type: "real", nullable: false),
                    Paid = table.Column<float>(type: "real", nullable: false),
                    Requested = table.Column<float>(type: "real", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tax = table.Column<float>(type: "real", nullable: false),
                    TransactionKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Transfer = table.Column<float>(type: "real", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VariableFee = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditCardPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditCardPayments_Donations_DonationId",
                        column: x => x.DonationId,
                        principalTable: "Donations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MBWayPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Alias = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DonationId = table.Column<int>(type: "int", nullable: true),
                    EasyPayPaymentId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FixedFee = table.Column<float>(type: "real", nullable: false),
                    Paid = table.Column<float>(type: "real", nullable: false),
                    Requested = table.Column<float>(type: "real", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tax = table.Column<float>(type: "real", nullable: false),
                    TransactionKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Transfer = table.Column<float>(type: "real", nullable: false),
                    VariableFee = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MBWayPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MBWayPayments_Donations_DonationId",
                        column: x => x.DonationId,
                        principalTable: "Donations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MultiBankPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DonationId = table.Column<int>(type: "int", nullable: true),
                    EasyPayPaymentId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TransactionKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MultiBankPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MultiBankPayments_Donations_DonationId",
                        column: x => x.DonationId,
                        principalTable: "Donations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardPayments_DonationId",
                table: "CreditCardPayments",
                column: "DonationId");

            migrationBuilder.CreateIndex(
                name: "IX_MBWayPayments_DonationId",
                table: "MBWayPayments",
                column: "DonationId");

            migrationBuilder.CreateIndex(
                name: "IX_MultiBankPayments_DonationId",
                table: "MultiBankPayments",
                column: "DonationId");

            migrationBuilder.AddForeignKey(
                name: "FK_PayPalPayments_Donations_DonationId",
                table: "PayPalPayments",
                column: "DonationId",
                principalTable: "Donations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
