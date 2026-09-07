// -----------------------------------------------------------------------
// <copyright file="IFunctionExecutionCommandExecutor.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function
{
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports;

    /// <summary>Executes commands while preserving command and report metadata.</summary>
    public interface IFunctionExecutionCommandExecutor : IFunctionExecutionExecutor
    {
        /// <summary>Executes a validated command.</summary>
        /// <param name="command">Validated command.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task object to monitor progress.</returns>
        Task ExecuteAsync(FunctionExecutionCommand command, CancellationToken cancellationToken = default);
    }
}
