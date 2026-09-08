// -----------------------------------------------------------------------
// <copyright file="20260908130000_PreventDuplicateEmailNotifications.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable disable

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.EntityFrameworkCore.Migrations;

    /// <summary>
    /// Prevents more than one email notification from being recorded for a payment.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260908130000_PreventDuplicateEmailNotifications")]
    public partial class PreventDuplicateEmailNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Subject",
                table: "PaymentNotifications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql(
                """
                ;WITH DuplicateNotifications AS
                (
                    SELECT Id,
                           ROW_NUMBER() OVER (
                               PARTITION BY PaymentId, NotificationType
                               ORDER BY Id) AS DuplicateRank
                    FROM PaymentNotifications
                    WHERE PaymentId IS NOT NULL
                )
                DELETE FROM PaymentNotifications
                WHERE Id IN
                (
                    SELECT Id
                    FROM DuplicateNotifications
                    WHERE DuplicateRank > 1
                );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentNotifications_PaymentId_NotificationType",
                table: "PaymentNotifications",
                columns: new[] { "PaymentId", "NotificationType" },
                unique: true,
                filter: "[PaymentId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentNotifications_PaymentId_NotificationType",
                table: "PaymentNotifications");

            migrationBuilder.DropColumn(
                name: "Subject",
                table: "PaymentNotifications");
        }
    }
}
