// -----------------------------------------------------------------------
// <copyright file="FunctionExecutionFunctionDescriptor.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports
{
    /// <summary>Metadata used to display a function in Admin.</summary>
    public sealed class FunctionExecutionFunctionDescriptor
    {
        /// <summary>Initializes a descriptor.</summary>
        public FunctionExecutionFunctionDescriptor(
            string functionKey,
            string displayNameResourceKey,
            string descriptionResourceKey,
            string schedule,
            FunctionExecutionReportScopeKind scopeKind)
        {
            this.FunctionKey = functionKey;
            this.DisplayNameResourceKey = displayNameResourceKey;
            this.DescriptionResourceKey = descriptionResourceKey;
            this.Schedule = schedule;
            this.ScopeKind = scopeKind;
        }

        /// <summary>Gets the stable key.</summary>
        public string FunctionKey { get; }

        /// <summary>Gets the display resource key.</summary>
        public string DisplayNameResourceKey { get; }

        /// <summary>Gets the description resource key.</summary>
        public string DescriptionResourceKey { get; }

        /// <summary>Gets the timer schedule.</summary>
        public string Schedule { get; }

        /// <summary>Gets the report scope kind.</summary>
        public FunctionExecutionReportScopeKind ScopeKind { get; }
    }
}
