// -----------------------------------------------------------------------
// <copyright file="20261023121000_ReferralCodeUnique.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable disable

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Migrations
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.EntityFrameworkCore.Migrations;

    /// <summary>
    /// Enforces unique referral codes and consolidates duplicate referral rows.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261023121000_ReferralCodeUnique")]
    public partial class ReferralCodeUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "IF EXISTS (SELECT 1 FROM [dbo].[Referrals] WHERE DATALENGTH([Code]) > 900) " +
                "BEGIN THROW 51000, 'Referral codes longer than 450 characters must be corrected before this migration can run.', 1; END;");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Referrals",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.Sql(
                @"SELECT [Id], [Code],
                         ROW_NUMBER() OVER (
                             PARTITION BY [Code]
                             ORDER BY CASE WHEN [Active] = 1 THEN 0 ELSE 1 END, [Id]) AS [DuplicateRank]
                  INTO #RankedReferrals
                  FROM [dbo].[Referrals]
                  WHERE [Code] IS NOT NULL;

                  UPDATE donation
                  SET [ReferralId] = canonical.[Id]
                  FROM [dbo].[Donations] AS donation
                  INNER JOIN #RankedReferrals AS duplicate ON duplicate.[Id] = donation.[ReferralId] AND duplicate.[DuplicateRank] > 1
                  INNER JOIN #RankedReferrals AS canonical ON canonical.[Code] = duplicate.[Code] AND canonical.[DuplicateRank] = 1;

                  UPDATE linkOpen
                  SET [ReferralId] = canonical.[Id]
                  FROM [dbo].[ReferralLinkOpens] AS linkOpen
                  INNER JOIN #RankedReferrals AS duplicate ON duplicate.[Id] = linkOpen.[ReferralId] AND duplicate.[DuplicateRank] > 1
                  INNER JOIN #RankedReferrals AS canonical ON canonical.[Code] = duplicate.[Code] AND canonical.[DuplicateRank] = 1;

                  UPDATE referral
                  SET [LinkOpenCount] = referral.[LinkOpenCount] + duplicateCounts.[DuplicateLinkOpenCount]
                  FROM [dbo].[Referrals] AS referral
                  INNER JOIN #RankedReferrals AS canonical ON canonical.[Id] = referral.[Id] AND canonical.[DuplicateRank] = 1
                  INNER JOIN (
                      SELECT duplicate.[Code], SUM(referral.[LinkOpenCount]) AS [DuplicateLinkOpenCount]
                      FROM [dbo].[Referrals] AS referral
                      INNER JOIN #RankedReferrals AS duplicate ON duplicate.[Id] = referral.[Id]
                      WHERE duplicate.[DuplicateRank] > 1
                      GROUP BY duplicate.[Code]
                  ) AS duplicateCounts ON duplicateCounts.[Code] = canonical.[Code];

                  DELETE referral
                  FROM [dbo].[Referrals] AS referral
                  INNER JOIN #RankedReferrals AS duplicate ON duplicate.[Id] = referral.[Id]
                  WHERE duplicate.[DuplicateRank] > 1;

                  DROP TABLE #RankedReferrals;");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_Code",
                table: "Referrals",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Referrals_Code",
                table: "Referrals");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Referrals",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);
        }
    }
}
