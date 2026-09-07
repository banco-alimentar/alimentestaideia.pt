// -----------------------------------------------------------------------
// <copyright file="IFunctionExecutionExecutor.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>Executes only functions explicitly present in the report catalog.</summary>
    public interface IFunctionExecutionExecutor
    {
        /// <summary>Executes a catalogued function.</summary>
        /// <param name="functionKey">Catalog function key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task object to monitor progress.</returns>
        Task ExecuteAsync(string functionKey, CancellationToken cancellationToken = default);
    }
}
