// -----------------------------------------------------------------------
// <copyright file="IFunctionExecutionReportReader.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>Read contract for latest and exact execution reports.</summary>
    public interface IFunctionExecutionReportReader
    {
        /// <summary>Reads the latest pointer for a function scope.</summary>
        Task<FunctionExecutionReportStorageResult> GetLatestAsync(FunctionExecutionReportScope scope, string functionKey, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new FunctionExecutionReportStorageResult
            {
                State = FunctionExecutionReportStorageState.Unavailable,
                Diagnostic = "Latest report lookup is not implemented by this reader.",
            });
        }

        /// <summary>Reads an exact execution report.</summary>
        Task<FunctionExecutionReportStorageResult> GetExecutionAsync(FunctionExecutionReportScope scope, string functionKey, string executionId, CancellationToken cancellationToken = default);
    }
}
