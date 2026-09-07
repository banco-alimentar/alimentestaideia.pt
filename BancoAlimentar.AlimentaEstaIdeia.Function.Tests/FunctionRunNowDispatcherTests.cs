// -----------------------------------------------------------------------
// <copyright file="FunctionRunNowDispatcherTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Function;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports;
    using Xunit;

    /// <summary>
    /// Integration tests for the HTTP Run Now dispatcher.
    /// </summary>
    public sealed class FunctionRunNowDispatcherTests
    {
        /// <summary>
        /// Every catalog function is accepted by the dispatcher.
        /// </summary>
        /// <param name="functionKey">Catalog function key.</param>
        [Theory]
        [InlineData("GenerateDonationReportFunction")]
        [InlineData("GenerateSiteHealthReportFunction")]
        [InlineData("DeleteOldSubscriptionFunction")]
        [InlineData("MultiBancoPaymentNotificationFunction")]
        [InlineData("UpdateSubscriptions")]
        public async Task DispatchAsync_AcceptsOnlyCatalogFunctionKeys(string functionKey)
        {
            var executor = new RecordingFunctionExecutor();
            var dispatcher = new FunctionHttpDispatcher(executor, new AllowTimerFunctionExecutionGuard());

            FunctionHttpDispatchResult result = await dispatcher.DispatchAsync(functionKey);

            Assert.True(result.Accepted);
            Assert.Equal(new[] { functionKey }, executor.FunctionKeys);
        }

        /// <summary>
        /// Arbitrary function names cannot be used to invoke an executable type.
        /// </summary>
        [Theory]
        [InlineData("DeleteAllDataAndRun")]
        [InlineData("System.IO.File.Delete")]
        [InlineData("../GenerateDonationReportFunction")]
        [InlineData("")]
        public async Task DispatchAsync_RejectsUnknownFunctionKeysWithoutExecution(string functionKey)
        {
            var executor = new RecordingFunctionExecutor();
            var dispatcher = new FunctionHttpDispatcher(executor, new AllowTimerFunctionExecutionGuard());

            FunctionHttpDispatchResult result = await dispatcher.DispatchAsync(functionKey);

            Assert.False(result.Accepted);
            Assert.Empty(executor.FunctionKeys);
        }

        /// <summary>
        /// The existing deployment-slot guard prevents Run Now from bypassing non-production safety.
        /// </summary>
        [Theory]
        [InlineData("preprod")]
        [InlineData("developer")]
        public async Task DispatchAsync_PreservesSlotGuard(string slotName)
        {
            var executor = new RecordingFunctionExecutor();
            var dispatcher = new FunctionHttpDispatcher(executor, new DenyTimerFunctionExecutionGuard(slotName));

            FunctionHttpDispatchResult result = await dispatcher.DispatchAsync("UpdateSubscriptions");

            Assert.False(result.Accepted);
            Assert.Empty(executor.FunctionKeys);
            Assert.Equal(slotName, result.RejectedSlotName);
        }

        /// <summary>
        /// A rejected developer or preprod command can still persist its skipped report without tenant execution.
        /// </summary>
        [Fact]
        public async Task DispatchAsync_RecordsSkippedReportWithoutExecutingTenantWork()
        {
            var executor = new RecordingSkipAwareFunctionExecutor();
            var dispatcher = new FunctionHttpDispatcher(executor, new DenyTimerFunctionExecutionGuard("developer"));

            FunctionHttpDispatchResult result = await dispatcher.DispatchAsync("GenerateDonationReportFunction");

            Assert.False(result.Accepted);
            Assert.Empty(executor.FunctionKeys);
            Assert.Equal(new[] { "GenerateDonationReportFunction" }, executor.SkippedFunctionKeys);
        }

        private sealed class RecordingFunctionExecutor : IFunctionExecutionExecutor
        {
            private readonly System.Collections.Generic.List<string> functionKeys = new System.Collections.Generic.List<string>();

            /// <summary>Gets the function keys executed by the fake.</summary>
            public System.Collections.Generic.IReadOnlyList<string> FunctionKeys => this.functionKeys;

            /// <inheritdoc />
            public Task ExecuteAsync(string functionKey, CancellationToken cancellationToken = default)
            {
                this.functionKeys.Add(functionKey);
                return Task.CompletedTask;
            }
        }

        private sealed class AllowTimerFunctionExecutionGuard : IFunctionExecutionGuard
        {
            /// <inheritdoc />
            public bool CanExecute(string functionKey, out string rejectedSlotName)
            {
                rejectedSlotName = null;
                return true;
            }
        }

        private sealed class RecordingSkipAwareFunctionExecutor : IFunctionExecutionExecutor, IFunctionExecutionSkipReporter
        {
            private readonly System.Collections.Generic.List<string> functionKeys = new System.Collections.Generic.List<string>();
            private readonly System.Collections.Generic.List<string> skippedFunctionKeys = new System.Collections.Generic.List<string>();

            /// <summary>Gets the function keys executed by the fake.</summary>
            public System.Collections.Generic.IReadOnlyList<string> FunctionKeys => this.functionKeys;

            /// <summary>Gets the function keys recorded as skipped by the fake.</summary>
            public System.Collections.Generic.IReadOnlyList<string> SkippedFunctionKeys => this.skippedFunctionKeys;

            /// <inheritdoc />
            public Task ExecuteAsync(string functionKey, CancellationToken cancellationToken = default)
            {
                this.functionKeys.Add(functionKey);
                return Task.CompletedTask;
            }

            /// <inheritdoc />
            public Task RecordSkippedAsync(FunctionExecutionCommand command, CancellationToken cancellationToken = default)
            {
                this.skippedFunctionKeys.Add(command.FunctionKey);
                return Task.CompletedTask;
            }
        }

        private sealed class DenyTimerFunctionExecutionGuard : IFunctionExecutionGuard
        {
            private readonly string slotName;

            /// <summary>
            /// Initializes a new instance of the <see cref="DenyTimerFunctionExecutionGuard"/> class.
            /// </summary>
            /// <param name="slotName">The rejected deployment slot.</param>
            public DenyTimerFunctionExecutionGuard(string slotName)
            {
                this.slotName = slotName;
            }

            /// <inheritdoc />
            public bool CanExecute(string functionKey, out string rejectedSlotName)
            {
                rejectedSlotName = this.slotName;
                return false;
            }
        }
    }
}
