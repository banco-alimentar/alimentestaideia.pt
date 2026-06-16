// -----------------------------------------------------------------------
// <copyright file="BackfillDonationPeriodoOficialTool.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    /// Backfills <see cref="Donation.PeriodoOficial"/> from donation dates and linked campaigns.
    /// </summary>
    public class BackfillDonationPeriodoOficialTool : BaseDatabaseTool
    {
        private static readonly CultureInfo PtCulture = CultureInfo.GetCultureInfo("pt-PT");

        public BackfillDonationPeriodoOficialTool(ApplicationDbContext context, IUnitOfWork unitOfWork)
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

            Console.WriteLine($"BackfillDonationPeriodoOficialTool started at {DateTime.UtcNow:O}");
            Console.WriteLine($"Initial Catalog: {initialCatalog}");
            Console.WriteLine($"DryRun={DryRun} ({(DryRun ? "no database changes will be saved" : "changes will be saved")})");
            Console.WriteLine();

            Dictionary<int, Campaign> campaignsById = this.Context.Campaigns
                .AsNoTracking()
                .ToDictionary(campaign => campaign.Id);

            Console.WriteLine($"Loaded {campaignsById.Count} campaign(s).");
            Console.WriteLine();

            List<Donation> donations = this.Context.Donations
                .OrderBy(donation => donation.Id)
                .ToList();

            Console.WriteLine($"Found {donations.Count} donation(s) to evaluate.");
            Console.WriteLine();

            int updatedCount = 0;
            int unchangedCount = 0;
            int missingCampaignCount = 0;

            foreach (Donation donation in donations)
            {
                bool periodoOficial = false;
                if (donation.CampaignId.HasValue
                    && campaignsById.TryGetValue(donation.CampaignId.Value, out Campaign campaign))
                {
                    periodoOficial = DonationPeriodoOficial.IsWithinOfficialPeriod(
                        donation.DonationDate,
                        campaign);
                }
                else if (donation.CampaignId.HasValue)
                {
                    missingCampaignCount++;
                }

                if (donation.PeriodoOficial == periodoOficial)
                {
                    unchangedCount++;
                    continue;
                }

                updatedCount++;
                Console.WriteLine(
                    $"UPDATE Donation Id={donation.Id}, DonationDate={FormatDate(donation.DonationDate)}, CampaignId={donation.CampaignId?.ToString() ?? "(null)"}");
                Console.WriteLine($"     -> PeriodoOficial={periodoOficial} (was {donation.PeriodoOficial})");
                Console.WriteLine();

                if (!DryRun)
                {
                    donation.PeriodoOficial = periodoOficial;
                    this.Context.Entry(donation).Property(d => d.PeriodoOficial).IsModified = true;
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
            Console.WriteLine(
                $"Summary: updated={updatedCount}, unchanged={unchangedCount}, missingCampaign={missingCampaignCount}, dryRun={DryRun}");
            Console.WriteLine($"Initial Catalog: {initialCatalog}");
            Console.WriteLine($"BackfillDonationPeriodoOficialTool finished at {DateTime.UtcNow:O}");
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
