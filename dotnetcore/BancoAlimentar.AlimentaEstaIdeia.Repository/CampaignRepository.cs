namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Default implementation for the <see cref="Campaign"/> repository pattern.
    /// </summary>
    public class CampaignRepository : GenericRepository<Campaign>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CampaignRepository"/> class.
        /// </summary>
        /// <param name="context"><see cref="ApplicationDbContext"/> instance.</param>
        public CampaignRepository(ApplicationDbContext context)
            : base(context)
        {
        }

        /// <summary>
        /// Gets the active campaign.
        /// </summary>
        /// <returns>A reference to the <see cref="Campaign"/></returns>
        public Campaign GetCurrentCampaign()
        {
            DateTime now = DateTime.UtcNow;

            Campaign result = this.DbContext.Campaigns
                .Include(p => p.ProductCatalogues)
                .Where(p => p.Start > now && p.End < now)
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
