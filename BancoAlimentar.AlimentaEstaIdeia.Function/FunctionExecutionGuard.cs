// -----------------------------------------------------------------------
// <copyright file="FunctionExecutionGuard.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function
{
    using System;

    /// <summary>Uses the existing deployment-slot safety rule for manual commands.</summary>
    public sealed class FunctionExecutionGuard : IFunctionExecutionGuard
    {
        /// <summary>Checks whether the current slot permits side-effecting work.</summary>
        /// <param name="functionKey">Catalog function key.</param>
        /// <param name="rejectedSlotName">Rejected slot, when applicable.</param>
        /// <returns>A value indicating whether execution is allowed.</returns>
        public bool CanExecute(string functionKey, out string rejectedSlotName)
        {
            rejectedSlotName = FunctionSlotExecution.ShouldRunTimerFunctions()
                ? null
                : Environment.GetEnvironmentVariable(FunctionSlotExecution.WebsiteSlotNameVariable);
            return rejectedSlotName == null;
        }
    }
}
