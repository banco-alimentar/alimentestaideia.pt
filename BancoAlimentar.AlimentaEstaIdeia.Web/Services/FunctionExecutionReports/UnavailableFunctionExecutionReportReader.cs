// -----------------------------------------------------------------------
// <copyright file="UnavailableFunctionExecutionReportReader.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services.FunctionExecutionReports
{
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports;

    /// <summary>
    /// Reader used when report storage is disabled or not configured.
    /// </summary>
    public sealed class UnavailableFunctionExecutionReportReader : IFunctionExecutionReportReader
    {
        /// <inheritdoc />
        public Task<FunctionExecutionReportStorageResult> GetLatestAsync(
            FunctionExecutionReportScope scope,
            string functionKey,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateResult());
        }

        /// <inheritdoc />
        public Task<FunctionExecutionReportStorageResult> GetExecutionAsync(
            FunctionExecutionReportScope scope,
            string functionKey,
            string executionId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateResult());
        }

        private static FunctionExecutionReportStorageResult CreateResult()
        {
            return new FunctionExecutionReportStorageResult
            {
                State = FunctionExecutionReportStorageState.Unavailable,
                Diagnostic = "Execution report storage is not configured.",
            };
        }
    }
}
