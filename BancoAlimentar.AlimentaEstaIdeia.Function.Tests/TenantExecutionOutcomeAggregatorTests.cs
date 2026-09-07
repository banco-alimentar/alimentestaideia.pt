// -----------------------------------------------------------------------
// <copyright file="TenantExecutionOutcomeAggregatorTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function.Tests
{
    using BancoAlimentar.AlimentaEstaIdeia.Function;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports;
    using Xunit;

    /// <summary>Tests aggregate outcomes for multi-tenant function execution.</summary>
    public class TenantExecutionOutcomeAggregatorTests
    {
        /// <summary>Skipped tenants are not counted as failures.</summary>
        [Fact]
        public void AllSkippedTenants_AreSkipped()
        {
            FunctionExecutionReportOutcome outcome = TenantExecutionOutcomeAggregator.GetOverallOutcome(0, 0, 3, 0);

            Assert.Equal(FunctionExecutionReportOutcome.Skipped, outcome);
        }

        /// <summary>All failed tenants produce a failed execution.</summary>
        [Fact]
        public void AllFailedTenants_AreFailed()
        {
            FunctionExecutionReportOutcome outcome = TenantExecutionOutcomeAggregator.GetOverallOutcome(0, 3, 0, 0);

            Assert.Equal(FunctionExecutionReportOutcome.Failed, outcome);
        }

        /// <summary>Mixed tenant outcomes produce a partial execution.</summary>
        [Theory]
        [InlineData(1, 1, 0, 0)]
        [InlineData(0, 1, 2, 0)]
        [InlineData(1, 0, 1, 0)]
        [InlineData(0, 0, 0, 2)]
        public void MixedTenantOutcomes_ArePartial(int succeeded, int failed, int skipped, int partial)
        {
            FunctionExecutionReportOutcome outcome = TenantExecutionOutcomeAggregator.GetOverallOutcome(
                succeeded,
                failed,
                skipped,
                partial);

            Assert.Equal(FunctionExecutionReportOutcome.Partial, outcome);
        }

        /// <summary>All successful tenants produce a successful execution.</summary>
        [Fact]
        public void AllSuccessfulTenants_AreSucceeded()
        {
            FunctionExecutionReportOutcome outcome = TenantExecutionOutcomeAggregator.GetOverallOutcome(3, 0, 0, 0);

            Assert.Equal(FunctionExecutionReportOutcome.Succeeded, outcome);
        }
    }
}
