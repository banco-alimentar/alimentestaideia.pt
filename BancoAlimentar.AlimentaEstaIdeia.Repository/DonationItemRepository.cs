// -----------------------------------------------------------------------
// <copyright file="DonationItemRepository.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System.Collections.Generic;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Common.Repository.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.Extensions.Caching.Memory;

    /// <summary>
    /// Default implementation for the <see cref="DonationItem"/> repository pattern.
    /// </summary>
    public class DonationItemRepository : GenericRepository<DonationItem, ApplicationDbContext>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DonationItemRepository"/> class.
        /// </summary>
        /// <param name="context"><see cref="ApplicationDbContext"/> instance.</param>
        /// <param name="memoryCache">A reference to the Memory cache system.</param>
        /// <param name="telemetryClient">Telemetry Client.</param>
        public DonationItemRepository(ApplicationDbContext context, IMemoryCache memoryCache, TelemetryClient telemetryClient)
            : base(context, memoryCache, telemetryClient)
        {
        }

        /// <summary>
        /// Get the cash donation item.
        /// </summary>
        /// <param name="amount">Cash donation amount.</param>
        /// <returns>A reference to the <see cref="DonationItem"/>.</returns>
        public ICollection<DonationItem> GetCashDonationItem(double amount)
        {
            return new List<DonationItem>()
            {
                new DonationItem()
                {
                    Quantity = 1,
                    Price = amount,
                    ProductCatalogue = new ProductCatalogueRepository(
                        this.DbContext,
                        this.MemoryCache,
                        this.TelemetryClient).GetCashProductCatalogue(),
                },
            };
        }

        /// <summary>
        /// Gets the Donations items from the string representation.
        /// </summary>
        /// <param name="value">String with the donation items.</param>
        /// <returns>A reference to <see cref="ICollection{DonationItem}"/> with all the donation items.</returns>
        public ICollection<DonationItem> GetDonationItems(string value)
        {
            List<DonationItem> result = new List<DonationItem>();

            if (!string.IsNullOrEmpty(value))
            {
                foreach (string pair in value.Split(new[] { ';' }))
                {
                    string[] values = pair.Split(":");
                    int id;
                    int quantity;
                    if (values.Length == 2 && int.TryParse(values[0], out id) && int.TryParse(values[1], out quantity))
                    {
                        ProductCatalogue product = this.DbContext.ProductCatalogues.Where(p => p.Id == id).First();

                        DonationItem donationItem = new DonationItem()
                        {
                            Quantity = quantity,
                            ProductCatalogue = product,
                            Price = product.Cost,
                        };

                        result.Add(donationItem);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the Donations items from the string representation. This is used when the 50c validation process.
        /// </summary>
        /// <param name="value">String with the donation items.</param>
        /// <returns>A reference to <see cref="ICollection{DonationItem}"/> with all the donation items.</returns>
        public ICollection<DonationItem> GetDonationItemsForModelException(string value)
        {
            List<DonationItem> result = new List<DonationItem>();

            if (!string.IsNullOrEmpty(value))
            {
                var allProductCatalog = new ProductCatalogueRepository(this.DbContext, this.MemoryCache, this.TelemetryClient).GetCurrentProductCatalogue();

                foreach (var item in allProductCatalog)
                {
                    result.Add(new DonationItem() { Quantity = 0, ProductCatalogue = item });
                }

                foreach (string pair in value.Split(new[] { ';' }))
                {
                    string[] values = pair.Split(":");
                    int id;
                    int quantity;
                    if (values.Length == 2 && int.TryParse(values[0], out id) && int.TryParse(values[1], out quantity))
                    {
                        ProductCatalogue product = allProductCatalog.Where(p => p.Id == id).First();

                        DonationItem donationItem = new DonationItem()
                        {
                            Quantity = quantity,
                            ProductCatalogue = product,
                            Price = product.Cost,
                        };

                        var itemToRemove = result.Where(p => p.ProductCatalogue.Id == id).FirstOrDefault();
                        int index = result.IndexOf(itemToRemove);
                        result.Remove(itemToRemove);
                        result.Insert(index, donationItem);
                    }
                }
            }

            return result;
        }
    }
}
