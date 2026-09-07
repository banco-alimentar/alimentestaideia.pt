// -----------------------------------------------------------------------
// <copyright file="FunctionExecutionReportOutcome.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports
{
    /// <summary>Describes the final result of a function execution.</summary>
    public enum FunctionExecutionReportOutcome
    {
        /// <summary>The execution completed successfully.</summary>
        Succeeded,

        /// <summary>Some scopes completed and at least one scope failed.</summary>
        Partial,

        /// <summary>The execution failed.</summary>
        Failed,

        /// <summary>The execution was intentionally skipped.</summary>
        Skipped,

        /// <summary>The function was disabled by configuration.</summary>
        Disabled,
    }
}
