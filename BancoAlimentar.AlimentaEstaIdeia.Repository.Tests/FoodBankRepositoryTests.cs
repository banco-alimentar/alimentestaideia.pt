// -----------------------------------------------------------------------
// <copyright file="FoodBankRepositoryTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    /// Unit tests for <see cref="FoodBankRepository"/>.
    /// </summary>
    public class FoodBankRepositoryTests : IClassFixture<ServicesFixture>
    {
        private readonly FoodBankRepository repository;
        private readonly ApplicationDbContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="FoodBankRepositoryTests"/> class.
        /// </summary>
        /// <param name="servicesFixture">Shared services fixture.</param>
        public FoodBankRepositoryTests(ServicesFixture servicesFixture)
        {
            this.repository = servicesFixture.ServiceProvider.GetRequiredService<FoodBankRepository>();
            this.context = servicesFixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        }

        /// <summary>
        /// Returns seeded food banks.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanGetAllFoodBanks()
        {
            var seededCount = await this.context.FoodBanks.CountAsync();
            var result = this.repository.GetAll().ToList();

            Assert.Equal(seededCount, result.Count);
            Assert.All(result, foodBank => Assert.False(string.IsNullOrWhiteSpace(foodBank.Name)));
        }

        /// <summary>
        /// Finds a food bank by identifier.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanGetFoodBankById()
        {
            var expected = await this.context.FoodBanks.FirstAsync();
            var result = this.repository.GetById(expected.Id);

            Assert.NotNull(result);
            Assert.Equal(expected.Name, result.Name);
        }

        /// <summary>
        /// Returns null when the food bank id is unknown.
        /// </summary>
        [Fact]
        public void ReturnsNullForUnknownFoodBankId()
        {
            var result = this.repository.GetById(int.MaxValue);
            Assert.Null(result);
        }

        /// <summary>
        /// Persists a newly added food bank.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanAddFoodBank()
        {
            var foodBank = new FoodBank
            {
                Name = $"Integration Food Bank {System.Guid.NewGuid():N}",
                ReceiptName = "Receipt",
                ReceiptPlace = "Lisboa",
            };

            this.repository.Add(foodBank);
            await this.context.SaveChangesAsync();

            var saved = await this.context.FoodBanks.FirstAsync(f => f.Name == foodBank.Name);
            Assert.Equal(foodBank.ReceiptPlace, saved.ReceiptPlace);
        }
    }
}
