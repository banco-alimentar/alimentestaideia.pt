// -----------------------------------------------------------------------
// <copyright file="ProductCatalogueRepositoryTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Unit tests for <see cref="ProductCatalogueRepository"/>.
    /// </summary>
    public class ProductCatalogueRepositoryTests : IClassFixture<ServicesFixture>
    {
        private readonly ProductCatalogueRepository repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProductCatalogueRepositoryTests"/> class.
        /// </summary>
        /// <param name="servicesFixture">Shared services fixture.</param>
        public ProductCatalogueRepositoryTests(ServicesFixture servicesFixture)
        {
            this.repository = servicesFixture.ServiceProvider.GetRequiredService<ProductCatalogueRepository>();
        }

        /// <summary>
        /// Returns the cash product catalogue entry from seed data.
        /// </summary>
        [Fact]
        public void CanGetCashProductCatalogue()
        {
            var result = this.repository.GetCashProductCatalogue();

            Assert.NotNull(result);
            Assert.Equal(ProductCatalogue.CashProductCatalogName, result.Name);
        }

        /// <summary>
        /// Loads campaign navigation for each product catalogue row.
        /// </summary>
        [Fact]
        public void CanGetAllWithCampaign()
        {
            var result = this.repository.GetAllWithCampaign();

            Assert.NotEmpty(result);
            Assert.All(
                result.Where(item => item.Name != ProductCatalogue.CashProductCatalogName),
                item => Assert.NotNull(item.Campaign));
        }

        /// <summary>
        /// Returns the current catalogue and its campaign from seed data.
        /// </summary>
        [Fact]
        public void CanGetCurrentProductCatalogue()
        {
            var (catalogues, campaign) = this.repository.GetCurrentProductCatalogue();

            Assert.NotEmpty(catalogues);
            Assert.NotNull(campaign);
            Assert.True(campaign.IsDefaultCampaign);
        }
    }
}
