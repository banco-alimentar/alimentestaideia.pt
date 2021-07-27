// -----------------------------------------------------------------------
// <copyright file="ProductCatalogueRepository.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System.Collections.Generic;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;

    /// <summary>
    /// Default implementation for the <see cref="ProductCatalogue"/> repository pattern.
    /// </summary>
    public class ProductCatalogueRepository : GenericRepository<ProductCatalogue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProductCatalogueRepository"/> class.
        /// </summary>
        /// <param name="context"><see cref="ApplicationDbContext"/> instance.</param>
        /// <param name="memoryCache">A reference to the Memory cache system.</param>
        public ProductCatalogueRepository(ApplicationDbContext context, IMemoryCache memoryCache)
            : base(context, memoryCache)
        {
        }

        /// <summary>
        /// Gets the current product catalogue. This method is catalog aware, meaning that will return the current product catalogue associated with the current campaign.
        /// </summary>
        /// <returns>A read only collection of <see cref="ProductCatalogue"/>.</returns>
        public IReadOnlyList<ProductCatalogue> GetCurrentProductCatalogue()
        {
            List<ProductCatalogue> result = new List<ProductCatalogue>();

            CampaignRepository campaignRepository = new CampaignRepository(this.DbContext, this.MemoryCache);
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

        /// <summary>
        /// Gets a list of all the <see cref="ProductCatalogue"/> with the <see cref="Campaign"/> information.
        /// </summary>
        /// <returns>A list of all the <see cref="ProductCatalogue"/> with the <see cref="Campaign"/> information.</returns>
        public IList<ProductCatalogue> GetAllWithCampaign()
        {
            return this.DbContext.ProductCatalogues.Include(p => p.Campaign).ToList();
        }
    }
}
