// -----------------------------------------------------------------------
// <copyright file="DonationItemRepositoryTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Unit tests for <see cref="DonationItemRepository"/>.
    /// </summary>
    public class DonationItemRepositoryTests : IClassFixture<ServicesFixture>
    {
        private readonly ServicesFixture fixture;
        private readonly DonationItemRepository repository;
        private readonly ApplicationDbContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="DonationItemRepositoryTests"/> class.
        /// </summary>
        /// <param name="servicesFixture">Shared services fixture.</param>
        public DonationItemRepositoryTests(ServicesFixture servicesFixture)
        {
            this.fixture = servicesFixture;
            this.repository = servicesFixture.ServiceProvider.GetRequiredService<DonationItemRepository>();
            this.context = servicesFixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        }

        /// <summary>
        /// Parses multiple id:quantity pairs from the donation items string.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanGetDonationItemsFromString()
        {
            var products = await this.context.ProductCatalogues.OrderBy(p => p.Id).Take(2).ToListAsync();
            string value = $"{products[0].Id}:2;{products[1].Id}:1";

            var result = this.repository.GetDonationItems(value);

            Assert.Equal(2, result.Count);
            Assert.Equal(2, result.First(i => i.ProductCatalogue.Id == products[0].Id).Quantity);
            Assert.Equal(1, result.First(i => i.ProductCatalogue.Id == products[1].Id).Quantity);
            Assert.Equal(products[0].Cost, result.First(i => i.ProductCatalogue.Id == products[0].Id).Price);
        }

        /// <summary>
        /// Empty or invalid tokens produce no donation items.
        /// </summary>
        [Fact]
        public void GetDonationItemsReturnsEmptyForBlankOrInvalidInput()
        {
            Assert.Empty(this.repository.GetDonationItems(string.Empty));
            Assert.Empty(this.repository.GetDonationItems("not-valid;1:abc"));
        }

        /// <summary>
        /// Builds a single cash donation line from the amount.
        /// </summary>
        [Fact]
        public void CanGetCashDonationItem()
        {
            var result = this.repository.GetCashDonationItem(25.0);

            var item = Assert.Single(result);
            Assert.Equal(25.0, item.Price);
            Assert.Equal(1, item.Quantity);
            Assert.Equal(ProductCatalogue.CashProductCatalogName, item.ProductCatalogue.Name);
        }

        /// <summary>
        /// Model-exception parsing returns all catalogue rows with updated quantities.
        /// </summary>
        [Fact]
        public void CanGetDonationItemsForModelException()
        {
            var productCatalogueRepository = this.fixture.ServiceProvider.GetRequiredService<ProductCatalogueRepository>();
            var (catalogues, _) = productCatalogueRepository.GetCurrentProductCatalogue();
            var product = catalogues.First();
            string value = $"{product.Id}:3";

            var result = this.repository.GetDonationItemsForModelException(value);

            Assert.Equal(catalogues.Count, result.Count);
            Assert.Equal(3, result.First(i => i.ProductCatalogue.Id == product.Id).Quantity);
            Assert.All(result.Where(i => i.ProductCatalogue.Id != product.Id), i => Assert.Equal(0, i.Quantity));
        }
    }
}
