// -----------------------------------------------------------------------
// <copyright file="UpdateSubscriptionsFunctionTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function.Tests
{
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Function;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.Tests;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="UpdateSubscriptions"/>.
    /// </summary>
    public class UpdateSubscriptionsFunctionTests : IClassFixture<ServicesFixture>
    {
        private readonly ServicesFixture fixture;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateSubscriptionsFunctionTests"/> class.
        /// </summary>
        /// <param name="fixture">Shared repository test fixture.</param>
        public UpdateSubscriptionsFunctionTests(ServicesFixture fixture)
        {
            this.fixture = fixture;
        }

        /// <summary>
        /// Executes the daily subscription maintenance function without error.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task ExecuteFunction_CompletesSuccessfully()
        {
            var context = this.fixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var unitOfWork = this.fixture.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var function = new UpdateSubscriptions(
                TelemetryConfiguration.CreateDefault(),
                this.fixture.ServiceProvider);

            await function.ExecuteFunction(unitOfWork, context);
        }
    }
}
