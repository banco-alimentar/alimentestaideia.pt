// -----------------------------------------------------------------------
// <copyright file="IFunctionExecutionReportIdGenerator.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports
{
    using System;

    /// <summary>Execution identifier generator seam.</summary>
    public interface IFunctionExecutionReportIdGenerator
    {
        /// <summary>Creates an execution identifier.</summary>
        string CreateId();
    }

    /// <summary>GUID-based production identifier generator.</summary>
    public sealed class GuidFunctionExecutionReportIdGenerator : IFunctionExecutionReportIdGenerator
    {
        /// <inheritdoc />
        public string CreateId() => Guid.NewGuid().ToString("N");
    }
}
