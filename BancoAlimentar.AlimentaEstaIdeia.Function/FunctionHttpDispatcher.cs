// -----------------------------------------------------------------------
// <copyright file="FunctionHttpDispatcher.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports;

    /// <summary>
    /// Validates command keys and dispatches manual executions. This class is not an HTTP-triggered Function.
    /// </summary>
    public sealed class FunctionHttpDispatcher
    {
        private readonly IFunctionExecutionExecutor executor;
        private readonly IFunctionExecutionGuard guard;

        /// <summary>Initializes a new instance of the <see cref="FunctionHttpDispatcher"/> class.</summary>
        /// <param name="executor">Allow-listed executor.</param>
        /// <param name="guard">Deployment-slot guard.</param>
        public FunctionHttpDispatcher(IFunctionExecutionExecutor executor, IFunctionExecutionGuard guard)
        {
            this.executor = executor ?? throw new ArgumentNullException(nameof(executor));
            this.guard = guard ?? throw new ArgumentNullException(nameof(guard));
        }

        /// <summary>Dispatches a function key for the focused dispatcher test seam.</summary>
        /// <param name="functionKey">Catalog function key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Dispatch result.</returns>
        public Task<FunctionHttpDispatchResult> DispatchAsync(string functionKey, CancellationToken cancellationToken = default)
        {
            var command = new FunctionExecutionCommand
            {
                FunctionKey = functionKey,
                CommandId = Guid.NewGuid().ToString("D"),
                RequestedAtUtc = DateTime.UtcNow,
                TriggerType = "AdminManualTrigger",
                TriggerSource = "AdminManualTrigger",
                CorrelationId = Guid.NewGuid().ToString("N"),
            };
            return this.DispatchAsync(command, cancellationToken);
        }

        /// <summary>Validates and dispatches a queue command.</summary>
        /// <param name="command">Command to dispatch.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Dispatch result.</returns>
        public async Task<FunctionHttpDispatchResult> DispatchAsync(
            FunctionExecutionCommand command,
            CancellationToken cancellationToken = default)
        {
            if (command == null)
            {
                return new FunctionHttpDispatchResult { Accepted = false, Error = "The command is required." };
            }

            string validationError = command.Validate();
            if (validationError != null)
            {
                return new FunctionHttpDispatchResult
                {
                    Accepted = false,
                    Error = validationError,
                    CommandId = command.CommandId,
                };
            }

            if (!this.guard.CanExecute(command.FunctionKey, out string rejectedSlotName))
            {
                if (this.executor is IFunctionExecutionSkipReporter skipReporter)
                {
                    await skipReporter.RecordSkippedAsync(command, cancellationToken).ConfigureAwait(false);
                }

                return new FunctionHttpDispatchResult
                {
                    Accepted = false,
                    RejectedSlotName = rejectedSlotName,
                    Error = "Execution is disabled for the current deployment slot.",
                    CommandId = command.CommandId,
                };
            }

            if (this.executor is IFunctionExecutionCommandExecutor commandExecutor)
            {
                await commandExecutor.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await this.executor.ExecuteAsync(command.FunctionKey, cancellationToken).ConfigureAwait(false);
            }

            return new FunctionHttpDispatchResult
            {
                Accepted = true,
                CommandId = command.CommandId,
            };
        }
    }
}
