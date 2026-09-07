// -----------------------------------------------------------------------
// <copyright file="IFunctionExecutionTriggerClient.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services.FunctionExecutionReports
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>Queues a validated manual Azure Function execution request.</summary>
    public interface IFunctionExecutionTriggerClient
    {
        /// <summary>Queues a request for a catalog function.</summary>
        /// <param name="functionKey">Stable catalog function key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The queueing result.</returns>
        Task<FunctionExecutionTriggerResult> TriggerAsync(
            string functionKey,
            CancellationToken cancellationToken = default);
    }
}
