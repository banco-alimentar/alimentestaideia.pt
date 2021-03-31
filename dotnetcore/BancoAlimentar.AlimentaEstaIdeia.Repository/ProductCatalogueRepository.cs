namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System.Collections.Generic;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Default implementation for the <see cref="ProductCatalogue"/> repository pattern.
    /// </summary>
    public class ProductCatalogueRepository : GenericRepository<ProductCatalogue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProductCatalogueRepository"/> class.
        /// </summary>
        /// <param name="context"><see cref="ApplicationDbContext"/> instance.</param>
        public ProductCatalogueRepository(ApplicationDbContext context)
            : base(context)
        {
        }

        /// <summary>
        /// Gets the current product catalogue. This method is catalog aware, meaning that will return the current product catalogue associated with the current campaign.
        /// </summary>
        /// <returns>A read only collection of <see cref="ProductCatalogue"/>.</returns>
        public IReadOnlyList<ProductCatalogue> GetCurrentProductCatalogue()
        {
            List<ProductCatalogue> result = new List<ProductCatalogue>();

            CampaignRepository campaignRepository = new CampaignRepository(this.DbContext);
            Campaign value = campaignRepository.GetCurrentCampaign();
            if (value != null && value.ProductCatalogues.Count > 0)
            {
                result.AddRange(value.ProductCatalogues);
            }
            else
            {
                result = this.GetAll().ToList();
            }

            return result.AsReadOnly();
        }

        public IList<ProductCatalogue> GetAllWithCampaign()
        {
            return this.DbContext.ProductCatalogues.Include(p => p.Campaign).ToList();
        }
    }
}
