// -----------------------------------------------------------------------
// <copyright file="ProductCatalogueRepository.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository;

using System.Collections.Generic;
using System.Linq;
using BancoAlimentar.AlimentaEstaIdeia.Common.Repository.Repository;
using BancoAlimentar.AlimentaEstaIdeia.Model;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

/// <summary>
/// Default implementation for the <see cref="ProductCatalogue"/> repository pattern.
/// </summary>
public class ProductCatalogueRepository : GenericRepository<ProductCatalogue, ApplicationDbContext>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProductCatalogueRepository"/> class.
    /// </summary>
    /// <param name="context"><see cref="ApplicationDbContext"/> instance.</param>
    /// <param name="memoryCache">A reference to the Memory cache system.</param>
    /// <param name="telemetryClient">Telemetry Client.</param>
    public ProductCatalogueRepository(ApplicationDbContext context, IMemoryCache memoryCache, TelemetryClient telemetryClient)
        : base(context, memoryCache, telemetryClient)
    {
    }

    /// <summary>
    /// Gets the current product catalogue. This method is catalog aware, meaning that will return the current product catalogue associated with the current campaign.
    /// </summary>
    /// <returns>A read only collection of <see cref="ProductCatalogue"/>.</returns>
    public IReadOnlyList<ProductCatalogue> GetCurrentProductCatalogue()
    {
        List<ProductCatalogue> result = new List<ProductCatalogue>();

        CampaignRepository campaignRepository = new CampaignRepository(this.DbContext, this.MemoryCache, this.TelemetryClient);
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

    /// <summary>
    /// Gets the <see cref="ProductCatalogue"/> that belong to a cash donation.
    /// </summary>
    /// <returns>The <see cref="ProductCatalogue"/> for a cash donation.</returns>
    public ProductCatalogue GetCashProductCatalogue()
    {
        return this.DbContext.ProductCatalogues
            .Where(p => p.Name == ProductCatalogue.CashProductCatalogName)
            .FirstOrDefault();
    }
}
