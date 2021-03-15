namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations
{
    using System;
    using Microsoft.EntityFrameworkCore.Migrations;

    public partial class CreditCardPayment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CreditCardPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Requested = table.Column<float>(type: "real", nullable: false),
                    Paid = table.Column<float>(type: "real", nullable: false),
                    FixedFee = table.Column<float>(type: "real", nullable: false),
                    VariableFee = table.Column<float>(type: "real", nullable: false),
                    Tax = table.Column<float>(type: "real", nullable: false),
                    Transfer = table.Column<float>(type: "real", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DonationId = table.Column<int>(type: "int", nullable: true),
                    TransactionKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EasyPayPaymentId = table.Column<string>(type: "nvarchar(max)", nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardPayments_DonationId",
                table: "CreditCardPayments",
                column: "DonationId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditCardPayments");
        }
    }
}
