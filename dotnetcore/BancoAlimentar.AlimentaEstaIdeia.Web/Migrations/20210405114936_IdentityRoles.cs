using Microsoft.EntityFrameworkCore.Migrations;

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations
{
    public partial class IdentityRoles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "WebUserId",
                table: "AspNetUserTokens",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WebUserId",
                table: "AspNetUserLogins",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WebUserId",
                table: "AspNetUserClaims",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WebUserId",
                table: "AspNetRoles",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserTokens_WebUserId",
                table: "AspNetUserTokens",
                column: "WebUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_WebUserId",
                table: "AspNetUserLogins",
                column: "WebUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_WebUserId",
                table: "AspNetUserClaims",
                column: "WebUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoles_WebUserId",
                table: "AspNetRoles",
                column: "WebUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetRoles_AspNetUsers_WebUserId",
                table: "AspNetRoles",
                column: "WebUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_WebUserId",
                table: "AspNetUserClaims",
                column: "WebUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_WebUserId",
                table: "AspNetUserLogins",
                column: "WebUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_WebUserId",
                table: "AspNetUserTokens",
                column: "WebUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetRoles_AspNetUsers_WebUserId",
                table: "AspNetRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_WebUserId",
                table: "AspNetUserClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_WebUserId",
                table: "AspNetUserLogins");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_WebUserId",
                table: "AspNetUserTokens");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUserTokens_WebUserId",
                table: "AspNetUserTokens");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUserLogins_WebUserId",
                table: "AspNetUserLogins");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUserClaims_WebUserId",
                table: "AspNetUserClaims");

            migrationBuilder.DropIndex(
                name: "IX_AspNetRoles_WebUserId",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "WebUserId",
                table: "AspNetUserTokens");

            migrationBuilder.DropColumn(
                name: "WebUserId",
                table: "AspNetUserLogins");

            migrationBuilder.DropColumn(
                name: "WebUserId",
                table: "AspNetUserClaims");

            migrationBuilder.DropColumn(
                name: "WebUserId",
                table: "AspNetRoles");

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
