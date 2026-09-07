// -----------------------------------------------------------------------
// <copyright file="FunctionExecutionTriggerResult.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services.FunctionExecutionReports
{
    using System;

    /// <summary>Result of a manual Function execution request.</summary>
    public sealed class FunctionExecutionTriggerResult
    {
        private FunctionExecutionTriggerResult(bool isSucceeded, string message)
        {
            this.IsSucceeded = isSucceeded;
            this.Message = message;
        }

        /// <summary>Gets a value indicating whether the request was queued.</summary>
        public bool IsSucceeded { get; }

        /// <summary>Gets the safe user-facing result message.</summary>
        public string Message { get; }

        /// <summary>Creates a successful result.</summary>
        /// <param name="message">User-facing message.</param>
        /// <returns>Successful result.</returns>
        public static FunctionExecutionTriggerResult Succeeded(string message)
        {
            return new FunctionExecutionTriggerResult(true, message);
        }

        /// <summary>Creates a failed result.</summary>
        /// <param name="message">User-facing message.</param>
        /// <returns>Failed result.</returns>
        public static FunctionExecutionTriggerResult Failed(string message)
        {
            return new FunctionExecutionTriggerResult(false, message);
        }
    }
}
