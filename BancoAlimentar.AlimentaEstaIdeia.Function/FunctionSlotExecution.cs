// -----------------------------------------------------------------------
// <copyright file="FunctionSlotExecution.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function
{
    using System;

    /// <summary>
    /// Determines whether timer functions should execute for the current deployment slot.
    /// </summary>
    public static class FunctionSlotExecution
    {
        /// <summary>
        /// Azure sets <c>WEBSITE_SLOT_NAME</c> on non-production deployment slots (e.g. preprod, developer).
        /// It is empty on the production slot.
        /// </summary>
        public const string WebsiteSlotNameVariable = "WEBSITE_SLOT_NAME";

        /// <summary>
        /// Returns true when timer side effects should run (production slot or local development).
        /// </summary>
        /// <returns>True when execution is allowed.</returns>
        public static bool ShouldRunTimerFunctions()
        {
            string slotName = Environment.GetEnvironmentVariable(WebsiteSlotNameVariable);
            if (string.IsNullOrWhiteSpace(slotName))
            {
                return true;
            }

            return string.Equals(slotName, "Production", StringComparison.OrdinalIgnoreCase);
        }
    }
}
