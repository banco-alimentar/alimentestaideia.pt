// -----------------------------------------------------------------------
// <copyright file="IFunctionExecutionSkipReporter.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function
{
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports;

    /// <summary>Records a skipped execution when the deployment-slot guard rejects a command.</summary>
    public interface IFunctionExecutionSkipReporter
    {
        /// <summary>Writes a skipped execution report for a validated command.</summary>
        /// <param name="command">Validated command.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task object to monitor progress.</returns>
        Task RecordSkippedAsync(FunctionExecutionCommand command, CancellationToken cancellationToken = default);
    }
}
