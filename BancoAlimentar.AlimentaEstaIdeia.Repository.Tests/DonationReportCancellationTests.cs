// -----------------------------------------------------------------------
// <copyright file="DonationReportCancellationTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Donation report cancellation tests.
    /// </summary>
    public class DonationReportCancellationTests : IClassFixture<ServicesFixture>
    {
        private readonly ServicesFixture fixture;

        /// <summary>
        /// Initializes a new instance of the <see cref="DonationReportCancellationTests"/> class.
        /// </summary>
        /// <param name="fixture">Shared services fixture.</param>
        public DonationReportCancellationTests(ServicesFixture fixture)
        {
            this.fixture = fixture;
        }

        /// <summary>
        /// Snapshot generation propagates cancellation instead of returning partial data.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task BuildSnapshotAsyncPropagatesCancellation()
        {
            var repository = new DonationReportRepository(
                this.fixture.ServiceProvider.GetRequiredService<ApplicationDbContext>(),
                this.fixture.ServiceProvider.GetRequiredService<DonationRepository>());
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                repository.BuildSnapshotAsync("Test tenant", cancellation.Token));
        }
    }
}