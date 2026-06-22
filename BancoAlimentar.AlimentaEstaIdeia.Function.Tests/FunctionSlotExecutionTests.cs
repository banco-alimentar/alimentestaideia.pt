// -----------------------------------------------------------------------
// <copyright file="FunctionSlotExecutionTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function.Tests
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Function;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="FunctionSlotExecution"/>.
    /// </summary>
    public class FunctionSlotExecutionTests
    {
        /// <summary>
        /// Timer functions must not run database work from non-production deployment slots.
        /// </summary>
        /// <param name="slotName">Simulated WEBSITE_SLOT_NAME value.</param>
        /// <param name="expected">Expected allow/deny result.</param>
        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("Production", true)]
        [InlineData("production", true)]
        [InlineData("preprod", false)]
        [InlineData("developer", false)]
        public void ShouldRunTimerFunctions_respects_deployment_slot(string slotName, bool expected)
        {
            string previous = Environment.GetEnvironmentVariable(FunctionSlotExecution.WebsiteSlotNameVariable);
            try
            {
                if (slotName == null)
                {
                    Environment.SetEnvironmentVariable(FunctionSlotExecution.WebsiteSlotNameVariable, null);
                }
                else
                {
                    Environment.SetEnvironmentVariable(FunctionSlotExecution.WebsiteSlotNameVariable, slotName);
                }

                Assert.Equal(expected, FunctionSlotExecution.ShouldRunTimerFunctions());
            }
            finally
            {
                Environment.SetEnvironmentVariable(FunctionSlotExecution.WebsiteSlotNameVariable, previous);
            }
        }
    }
}
