// -----------------------------------------------------------------------
// <copyright file="FunctionExecutionReportStorageState.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports
{
    /// <summary>Typed result state returned by report storage operations.</summary>
    public enum FunctionExecutionReportStorageState
    {
        /// <summary>The report was persisted or read successfully.</summary>
        Succeeded,

        /// <summary>A report or latest pointer is available for reading.</summary>
        Available,

        /// <summary>No report exists at the requested path.</summary>
        Missing,

        /// <summary>The report exists but is outside the configured retention window.</summary>
        Expired,

        /// <summary>The report could not be deserialized.</summary>
        Corrupt,

        /// <summary>Storage could not be reached or permission was denied.</summary>
        Unavailable,

        /// <summary>A write operation failed.</summary>
        Failed,
    }
}
