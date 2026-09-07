// -----------------------------------------------------------------------
// <copyright file="TenantExecutionOutcomeAggregator.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function
{
    using BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports;

    /// <summary>Calculates the overall outcome for a multi-tenant execution.</summary>
    public static class TenantExecutionOutcomeAggregator
    {
        /// <summary>Gets the aggregate outcome without treating skipped tenants as failures.</summary>
        /// <param name="succeeded">Number of successful tenants.</param>
        /// <param name="failed">Number of failed tenants.</param>
        /// <param name="skipped">Number of skipped or disabled tenants.</param>
        /// <param name="partial">Number of partially completed tenants.</param>
        /// <returns>The aggregate function outcome.</returns>
        public static FunctionExecutionReportOutcome GetOverallOutcome(
            int succeeded,
            int failed,
            int skipped,
            int partial)
        {
            int processed = succeeded + failed + skipped + partial;
            if (processed == 0 || succeeded == processed)
            {
                return FunctionExecutionReportOutcome.Succeeded;
            }

            if (failed == processed)
            {
                return FunctionExecutionReportOutcome.Failed;
            }

            if (skipped == processed)
            {
                return FunctionExecutionReportOutcome.Skipped;
            }

            return FunctionExecutionReportOutcome.Partial;
        }
    }
}
