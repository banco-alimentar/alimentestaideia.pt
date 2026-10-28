// -----------------------------------------------------------------------
// <copyright file="20261024174319_HardenPaymentConcurrency.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable disable

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations
{
    using System;
    using Microsoft.EntityFrameworkCore.Migrations;

    /// <inheritdoc />
    public partial class HardenPaymentConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS
                (
                    SELECT EasyPayPaymentId
                    FROM Payments
                    WHERE EasyPayPaymentId IS NOT NULL
                    GROUP BY EasyPayPaymentId
                    HAVING COUNT(*) > 1
                )
                    THROW 51000, 'Duplicate EasyPayPaymentId values must be reconciled before applying HardenPaymentConcurrency.', 1;

                IF EXISTS
                (
                    SELECT 1
                    FROM Payments
                    WHERE LEN(EasyPayPaymentId) > 128
                )
                    THROW 51001, 'EasyPayPaymentId values longer than 128 characters must be reconciled before applying HardenPaymentConcurrency.', 1;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "EasyPayPaymentId",
                table: "Payments",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                table: "PaymentNotifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryStatus",
                table: "PaymentNotifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAttemptAtUtc",
                table: "PaymentNotifications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastError",
                table: "PaymentNotifications",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PaymentNotifications",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SentAtUtc",
                table: "PaymentNotifications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SequenceScope",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE Invoices
                SET SequenceScope = COALESCE(FoodBankId, 0);

                UPDATE PaymentNotifications
                SET DeliveryStatus = 1,
                    LastAttemptAtUtc = Created,
                    SentAtUtc = Created,
                    AttemptCount = 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_Invoices_Sequence_FoodBank_Id_Year",
                table: "Invoices");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_EasyPayPaymentId",
                table: "Payments",
                column: "EasyPayPaymentId",
                unique: true,
                filter: "[EasyPayPaymentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Year_SequenceScope_Sequence",
                table: "Invoices",
                columns: new[] { "Year", "SequenceScope", "Sequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_EasyPayPaymentId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_Year_SequenceScope_Sequence",
                table: "Invoices");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Sequence_FoodBank_Id_Year",
                table: "Invoices",
                columns: new[] { "Sequence", "FoodBankId", "Year" },
                unique: true);

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                table: "PaymentNotifications");

            migrationBuilder.DropColumn(
                name: "DeliveryStatus",
                table: "PaymentNotifications");

            migrationBuilder.DropColumn(
                name: "LastAttemptAtUtc",
                table: "PaymentNotifications");

            migrationBuilder.DropColumn(
                name: "LastError",
                table: "PaymentNotifications");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PaymentNotifications");

            migrationBuilder.DropColumn(
                name: "SentAtUtc",
                table: "PaymentNotifications");

            migrationBuilder.DropColumn(
                name: "SequenceScope",
                table: "Invoices");

            migrationBuilder.AlterColumn<string>(
                name: "EasyPayPaymentId",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldNullable: true);
        }
    }
}
