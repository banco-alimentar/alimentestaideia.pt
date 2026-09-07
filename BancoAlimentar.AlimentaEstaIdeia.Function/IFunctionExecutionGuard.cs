// -----------------------------------------------------------------------
// <copyright file="IFunctionExecutionGuard.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function
{
    /// <summary>Determines whether a manual command may execute in the current slot.</summary>
    public interface IFunctionExecutionGuard
    {
        /// <summary>Checks the existing timer safety rule.</summary>
        /// <param name="functionKey">Catalog function key.</param>
        /// <param name="rejectedSlotName">Rejected slot, when applicable.</param>
        /// <returns>A value indicating whether execution is allowed.</returns>
        bool CanExecute(string functionKey, out string rejectedSlotName);
    }
}
