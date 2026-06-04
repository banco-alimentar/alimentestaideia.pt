// -----------------------------------------------------------------------
// <copyright file="BackfillDonationCampaignIdTool.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.Database
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.Data.SqlClient;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Backfills <see cref="Donation.CampaignId"/> from existing <see cref="Donation.CampaignName"/>
    /// or by resolving the campaign active on the donation date.
    /// </summary>
    public class BackfillDonationCampaignIdTool : BaseDatabaseTool
    {
        private static readonly CultureInfo PtCulture = CultureInfo.GetCultureInfo("pt-PT");

        public BackfillDonationCampaignIdTool(ApplicationDbContext context, IUnitOfWork unitOfWork)
            : base(context, unitOfWork)
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether changes are logged only and not persisted.
        /// </summary>
        public bool DryRun { get; set; } = true;

        /// <inheritdoc/>
        public override void ExecuteTool()
        {
            string initialCatalog = this.GetInitialCatalog();

            Console.WriteLine($"BackfillDonationCampaignIdTool started at {DateTime.UtcNow:O}");
            Console.WriteLine($"Initial Catalog: {initialCatalog}");
            Console.WriteLine($"DryRun={DryRun} ({(DryRun ? "no database changes will be saved" : "changes will be saved")})");
            Console.WriteLine();

            List<Campaign> campaigns = this.Context.Campaigns
                .AsNoTracking()
                .OrderBy(c => c.Start)
                .ToList();

            if (campaigns.Count == 0)
            {
                Console.WriteLine("No campaigns found in database. Nothing to do.");
                this.WriteCompletionSummary(initialCatalog, updatedCount: 0, skippedCount: 0, unresolvedCount: 0);
                return;
            }

            Console.WriteLine($"Loaded {campaigns.Count} campaign(s):");
            foreach (Campaign campaign in campaigns)
            {
                Console.WriteLine(
                    $"  Id={campaign.Id}, Number={campaign.Number}, Start={FormatDate(campaign.Start)}, End={FormatDate(campaign.End)}, IsDefault={campaign.IsDefaultCampaign}");
            }

            Console.WriteLine();

            List<Donation> donations = this.Context.Donations
                .Where(d => d.CampaignId == null)
                .OrderBy(d => d.Id)
                .ToList();

            Console.WriteLine($"Found {donations.Count} donation(s) without CampaignId.");
            Console.WriteLine();

            int updatedCount = 0;
            int skippedCount = 0;
            int unresolvedCount = 0;

            foreach (Donation donation in donations)
            {
                Campaign resolvedCampaign = null;
                string reason = null;

                if (!string.IsNullOrWhiteSpace(donation.CampaignName))
                {
                    resolvedCampaign = campaigns
                        .FirstOrDefault(c => string.Equals(c.Number, donation.CampaignName, StringComparison.Ordinal));

                    if (resolvedCampaign != null)
                    {
                        reason = $"Matched stored CampaignName '{donation.CampaignName}' to campaign Id {resolvedCampaign.Id}.";
                    }
                    else
                    {
                        resolvedCampaign = this.ResolveCampaignForDate(campaigns, donation.DonationDate);
                        if (resolvedCampaign != null)
                        {
                            reason = $"CampaignName '{donation.CampaignName}' did not match any campaign; resolved by donation date {FormatDate(donation.DonationDate)} within campaign window.";
                        }
                        else
                        {
                            reason = $"CampaignName '{donation.CampaignName}' did not match any campaign and no campaign could be resolved for donation date {FormatDate(donation.DonationDate)}.";
                        }
                    }
                }
                else
                {
                    resolvedCampaign = this.ResolveCampaignForDate(campaigns, donation.DonationDate);
                    if (resolvedCampaign != null)
                    {
                        reason = $"CampaignName is null; resolved by donation date {FormatDate(donation.DonationDate)} within campaign Start/End window.";
                    }
                    else
                    {
                        reason = $"CampaignName is null and no campaign could be resolved for donation date {FormatDate(donation.DonationDate)}.";
                    }
                }

                if (resolvedCampaign == null)
                {
                    unresolvedCount++;
                    Console.WriteLine(
                        $"SKIP Donation Id={donation.Id}, DonationDate={FormatDate(donation.DonationDate)}, CampaignName='{donation.CampaignName ?? "(null)"}'");
                    Console.WriteLine($"     Reason: {reason}");
                    Console.WriteLine();
                    continue;
                }

                if (donation.CampaignId == resolvedCampaign.Id)
                {
                    skippedCount++;
                    Console.WriteLine(
                        $"SKIP Donation Id={donation.Id} already has CampaignId={donation.CampaignId}.");
                    Console.WriteLine();
                    continue;
                }

                updatedCount++;
                Console.WriteLine(
                    $"UPDATE Donation Id={donation.Id}, DonationDate={FormatDate(donation.DonationDate)}, CampaignName='{donation.CampaignName ?? "(null)"}'");
                Console.WriteLine(
                    $"     -> CampaignId={resolvedCampaign.Id}, CampaignNumber={resolvedCampaign.Number}, Start={FormatDate(resolvedCampaign.Start)}, End={FormatDate(resolvedCampaign.End)}");
                Console.WriteLine($"     Reason: {reason}");
                Console.WriteLine();

                if (!DryRun)
                {
                    donation.CampaignId = resolvedCampaign.Id;
                    this.Context.Entry(donation).Property(d => d.CampaignId).IsModified = true;
                }
            }

            if (!DryRun && updatedCount > 0)
            {
                int saved = this.Context.SaveChanges();
                Console.WriteLine($"Saved {saved} change(s) to the database.");
            }
            else if (DryRun && updatedCount > 0)
            {
                Console.WriteLine($"DryRun enabled: {updatedCount} donation(s) would be updated. Re-run with --execute to apply.");
            }

            Console.WriteLine();
            this.WriteCompletionSummary(initialCatalog, updatedCount, skippedCount, unresolvedCount);
        }

        private void WriteCompletionSummary(string initialCatalog, int updatedCount, int skippedCount, int unresolvedCount)
        {
            Console.WriteLine($"Summary: updated={updatedCount}, skipped={skippedCount}, unresolved={unresolvedCount}, dryRun={DryRun}");
            Console.WriteLine($"Initial Catalog: {initialCatalog}");
            Console.WriteLine($"BackfillDonationCampaignIdTool finished at {DateTime.UtcNow:O}");
        }

        private Campaign ResolveCampaignForDate(IReadOnlyList<Campaign> campaigns, DateTime donationDate)
        {
            DateTime localDonationDate = donationDate.Kind == DateTimeKind.Utc
                ? donationDate.ToLocalTime()
                : donationDate;

            Campaign timedCampaign = campaigns
                .Where(c => !c.IsDefaultCampaign && c.Start < localDonationDate && c.End > localDonationDate)
                .OrderByDescending(c => c.Start)
                .FirstOrDefault();

            if (timedCampaign != null)
            {
                return timedCampaign;
            }

            return campaigns.FirstOrDefault(c => c.IsDefaultCampaign);
        }

        private static string FormatDate(DateTime value)
        {
            return value.ToString("yyyy-MM-dd HH:mm:ss", PtCulture);
        }

        private string GetInitialCatalog()
        {
            string connectionString = this.Context.Database.GetConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return "(not specified)";
            }

            var builder = new SqlConnectionStringBuilder(connectionString);
            return string.IsNullOrWhiteSpace(builder.InitialCatalog)
                ? "(not specified)"
                : builder.InitialCatalog;
        }
    }
}
