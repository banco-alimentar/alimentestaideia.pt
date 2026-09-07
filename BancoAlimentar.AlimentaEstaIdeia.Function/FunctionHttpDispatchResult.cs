// -----------------------------------------------------------------------
// <copyright file="FunctionHttpDispatchResult.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function
{
    /// <summary>Result of validating and dispatching a manual command.</summary>
    public sealed class FunctionHttpDispatchResult
    {
        /// <summary>Gets a value indicating whether the command was accepted for execution.</summary>
        public bool Accepted { get; init; }

        /// <summary>Gets the rejected deployment slot, if the safety guard rejected the command.</summary>
        public string RejectedSlotName { get; init; }

        /// <summary>Gets the validation or dispatch error.</summary>
        public string Error { get; init; }

        /// <summary>Gets the command identifier.</summary>
        public string CommandId { get; init; }
    }
}
