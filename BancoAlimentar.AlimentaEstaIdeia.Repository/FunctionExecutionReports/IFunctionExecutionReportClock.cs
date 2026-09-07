// -----------------------------------------------------------------------
// <copyright file="IFunctionExecutionReportClock.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports
{
    using System;

    /// <summary>UTC clock seam for execution reporting.</summary>
    public interface IFunctionExecutionReportClock
    {
        /// <summary>Gets the current UTC time.</summary>
        DateTime UtcNow { get; }
    }

    /// <summary>Production UTC clock.</summary>
    public sealed class SystemUtcClock : IFunctionExecutionReportClock
    {
        /// <inheritdoc />
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
