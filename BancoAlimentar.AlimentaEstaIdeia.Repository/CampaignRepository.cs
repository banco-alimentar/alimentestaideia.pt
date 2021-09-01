// -----------------------------------------------------------------------
// <copyright file="CampaignRepository.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;

    /// <summary>
    /// Default implementation for the <see cref="Campaign"/> repository pattern.
    /// </summary>
    public class CampaignRepository : GenericRepository<Campaign>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CampaignRepository"/> class.
        /// </summary>
        /// <param name="context"><see cref="ApplicationDbContext"/> instance.</param>
        /// <param name="memoryCache">A reference to the Memory cache system.</param>
        /// <param name="telemetryClient">Telemetry Client.</param>
        public CampaignRepository(ApplicationDbContext context, IMemoryCache memoryCache, TelemetryClient telemetryClient)
            : base(context, memoryCache, telemetryClient)
        {
        }

        /// <summary>
        /// Gets the active campaign.
        /// </summary>
        /// <returns>A reference to the <see cref="Campaign"/>.</returns>
        public Campaign GetCurrentCampaign()
        {
            DateTime now = DateTime.Now;

            Campaign result = this.DbContext.Campaigns
                .Include(p => p.ProductCatalogues)
                .Where(p => p.Start < now && p.End > now && !p.IsDefaultCampaign)
                .FirstOrDefault();

            if (result == null)
            {
                result = this.DbContext.Campaigns
                    .Include(p => p.ProductCatalogues)
                    .Where(p => p.IsDefaultCampaign)
                    .FirstOrDefault();
            }

            return result;
        }
    }
}
