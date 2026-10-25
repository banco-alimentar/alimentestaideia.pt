// -----------------------------------------------------------------------
// <copyright file="DonationItemRepository.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
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
            if (!TryParseDonationItems(value, out IReadOnlyList<(int ProductId, int Quantity)> parsedItems))
            {
                return new List<DonationItem>();
            }

            IReadOnlyDictionary<int, ProductCatalogue> products = this.GetCurrentProductsById();
            if (parsedItems.Any(item => !products.ContainsKey(item.ProductId)))
            {
                return new List<DonationItem>();
            }

            return parsedItems
                .Select(item => new DonationItem
                {
                    Quantity = item.Quantity,
                    ProductCatalogue = products[item.ProductId],
                    Price = products[item.ProductId].Cost,
                })
                .ToList();
        }

        /// <summary>
        /// Gets the Donations items from the string representation. This is used when the 50c validation process.
        /// </summary>
        /// <param name="value">String with the donation items.</param>
        /// <returns>A reference to <see cref="ICollection{DonationItem}"/> with all the donation items.</returns>
        public ICollection<DonationItem> GetDonationItemsForModelException(string value)
        {
            IReadOnlyDictionary<int, ProductCatalogue> products = this.GetCurrentProductsById();
            Dictionary<int, int> quantities = new Dictionary<int, int>();
            if (TryParseDonationItems(value, out IReadOnlyList<(int ProductId, int Quantity)> parsedItems)
                && parsedItems.All(item => products.ContainsKey(item.ProductId)))
            {
                quantities = parsedItems.ToDictionary(item => item.ProductId, item => item.Quantity);
            }

            return products.Values
                .Select(product => new DonationItem
                {
                    Quantity = quantities.TryGetValue(product.Id, out int quantity) ? quantity : 0,
                    ProductCatalogue = product,
                    Price = product.Cost,
                })
                .ToList();
        }

        /// <summary>
        /// Sets the parent <see cref="Donation"/> on each item so EF persists a valid foreign key.
        /// </summary>
        /// <param name="items">Donation line items.</param>
        /// <param name="donation">Owning donation.</param>
        public void AttachItemsToDonation(ICollection<DonationItem> items, Donation donation)
        {
            if (items == null || donation == null)
            {
                return;
            }

            foreach (DonationItem item in items)
            {
                item.Donation = donation;
            }
        }

        /// <summary>
        /// Removes existing persisted items for a donation and replaces them with a new set.
        /// </summary>
        /// <param name="donation">Donation being updated (for example on form re-submit).</param>
        /// <param name="newItems">Replacement line items.</param>
        /// <returns>The attached replacement items.</returns>
        public ICollection<DonationItem> ReplaceDonationItems(Donation donation, ICollection<DonationItem> newItems)
        {
            if (donation == null)
            {
                throw new ArgumentNullException(nameof(donation));
            }

            if (donation.DonationItems != null && donation.DonationItems.Count > 0)
            {
                this.RemoveRange(donation.DonationItems);
                donation.DonationItems.Clear();
            }

            var attachedItems = newItems?.ToList() ?? new List<DonationItem>();
            this.AttachItemsToDonation(attachedItems, donation);
            donation.DonationItems = attachedItems;
            return attachedItems;
        }

        private static bool TryParseDonationItems(
            string value,
            out IReadOnlyList<(int ProductId, int Quantity)> parsedItems)
        {
            List<(int ProductId, int Quantity)> result = new List<(int ProductId, int Quantity)>();
            HashSet<int> productIds = new HashSet<int>();
            if (string.IsNullOrWhiteSpace(value))
            {
                parsedItems = result;
                return false;
            }

            foreach (string pair in value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                string[] values = pair.Split(':');
                if (values.Length != 2
                    || !int.TryParse(values[0], out int productId)
                    || !int.TryParse(values[1], out int quantity)
                    || productId <= 0
                    || quantity <= 0
                    || !productIds.Add(productId))
                {
                    parsedItems = new List<(int ProductId, int Quantity)>();
                    return false;
                }

                result.Add((productId, quantity));
            }

            parsedItems = result;
            return result.Count > 0;
        }

        private IReadOnlyDictionary<int, ProductCatalogue> GetCurrentProductsById()
        {
            var currentCatalogue = new ProductCatalogueRepository(
                this.DbContext,
                this.MemoryCache,
                this.TelemetryClient).GetCurrentProductCatalogue();
            return currentCatalogue.ProductCatalogues.ToDictionary(product => product.Id);
        }
    }
}
