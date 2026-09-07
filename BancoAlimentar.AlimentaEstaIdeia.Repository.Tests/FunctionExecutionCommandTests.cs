// -----------------------------------------------------------------------
// <copyright file="FunctionExecutionCommandTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports;
    using Xunit;

    /// <summary>Tests the private manual-execution command contract.</summary>
    public sealed class FunctionExecutionCommandTests
    {
        /// <summary>The Web queue producer's trigger-source property is accepted.</summary>
        [Fact]
        public void Validate_AcceptsTriggerSourceWireProperty()
        {
            var command = new FunctionExecutionCommand
            {
                FunctionKey = "UpdateSubscriptions",
                CommandId = Guid.NewGuid().ToString("D"),
                RequestedAtUtc = DateTimeOffset.UtcNow,
                TriggerSource = "AdminManualTrigger",
            };

            Assert.Null(command.Validate());
            Assert.Equal("AdminManualTrigger", command.GetTriggerType());
        }

        /// <summary>Unknown function keys cannot be converted into executable types.</summary>
        [Fact]
        public void Validate_RejectsUnknownFunctionKey()
        {
            var command = new FunctionExecutionCommand
            {
                FunctionKey = "DeleteAllDataAndRun",
                CommandId = Guid.NewGuid().ToString("D"),
                RequestedAtUtc = DateTimeOffset.UtcNow,
                TriggerSource = "AdminManualTrigger",
            };

            Assert.NotNull(command.Validate());
        }
    }
}
