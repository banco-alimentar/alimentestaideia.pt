// -----------------------------------------------------------------------
// <copyright file="FunctionExecutionReportScope.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports
{
    using System;

    /// <summary>Identifies the slot and tenant boundary for a report.</summary>
    public sealed class FunctionExecutionReportScope
    {
        /// <summary>Initializes a new instance of the <see cref="FunctionExecutionReportScope"/> class.</summary>
        /// <param name="slotKey">Deployment slot.</param>
        /// <param name="scopeKey">Tenant key or global.</param>
        public FunctionExecutionReportScope(string slotKey, string scopeKey)
        {
            this.SlotKey = FunctionExecutionReportPaths.NormalizeSegment(slotKey, nameof(slotKey));
            this.ScopeKey = FunctionExecutionReportPaths.NormalizeSegment(scopeKey, nameof(scopeKey));
        }

        /// <summary>Gets the normalized deployment slot.</summary>
        public string SlotKey { get; }

        /// <summary>Gets the normalized tenant or global scope.</summary>
        public string ScopeKey { get; }

        /// <summary>Returns the scope as a readable value.</summary>
        public override string ToString() => $"{this.SlotKey}/{this.ScopeKey}";
    }
}
