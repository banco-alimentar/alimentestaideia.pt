// -----------------------------------------------------------------------
// <copyright file="FunctionExecutionReportScopeKind.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports
{
    /// <summary>Scope used by a function execution report.</summary>
    public enum FunctionExecutionReportScopeKind
    {
        /// <summary>The report belongs to one tenant.</summary>
        Tenant,

        /// <summary>The report belongs to the host or global scope.</summary>
        Global,
    }
}
