// -----------------------------------------------------------------------
// <copyright file="IFunctionExecutionReportStore.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>Write contract for immutable execution reports.</summary>
    public interface IFunctionExecutionReportStore
    {
        /// <summary>Writes one immutable execution report.</summary>
        Task WriteExecutionAsync(FunctionExecutionReport report, CancellationToken cancellationToken = default);

        /// <summary>Publishes the latest pointer monotonically.</summary>
        Task PublishLatestAsync(FunctionExecutionReportLatestPointer pointer, CancellationToken cancellationToken = default);
    }
}
