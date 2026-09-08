// -----------------------------------------------------------------------
// <copyright file="AdminFunctionExecutionQueueTrigger.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function
{
    using System;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    /// <summary>Consumes private, catalog-validated manual execution commands.</summary>
    public sealed class AdminFunctionExecutionQueueTrigger
    {
        private readonly FunctionHttpDispatcher dispatcher;
        private readonly FunctionExecutionCommandOptions options;
        private readonly ILogger<AdminFunctionExecutionQueueTrigger> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminFunctionExecutionQueueTrigger"/> class.
        /// </summary>
        /// <param name="dispatcher">Allow-listed dispatcher.</param>
        /// <param name="configuration">Function configuration.</param>
        /// <param name="logger">Logger.</param>
        public AdminFunctionExecutionQueueTrigger(
            FunctionHttpDispatcher dispatcher,
            IConfiguration configuration,
            ILogger<AdminFunctionExecutionQueueTrigger> logger)
        {
            this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            this.options = FunctionExecutionCommandOptions.Read(configuration);
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Processes one manual command from the private storage queue.</summary>
        /// <param name="message">Serialized command.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task object to monitor progress.</returns>
        [Function("AdminFunctionExecutionQueueTrigger")]
        public async Task Run(
            [QueueTrigger("%FunctionExecutionCommands:QueueName%", Connection = "FunctionExecutionCommands:ConnectionString")] string message,
            CancellationToken cancellationToken)
        {
            if (!this.options.Enabled)
            {
                this.logger.LogWarning("Manual function execution commands are disabled.");
                return;
            }

            if (string.IsNullOrWhiteSpace(message) || message.Length > this.options.MaxMessageLength)
            {
                throw new InvalidOperationException("The function execution command message is empty or too large.");
            }

            FunctionExecutionCommand command;
            try
            {
                command = JsonSerializer.Deserialize<FunctionExecutionCommand>(message, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                });
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException("The function execution command message is not valid JSON.", exception);
            }

            string validationError = command?.Validate() ?? "The function execution command is required.";
            if (validationError != null)
            {
                throw new InvalidOperationException(validationError);
            }

            FunctionHttpDispatchResult result = await this.dispatcher.DispatchAsync(command, cancellationToken).ConfigureAwait(false);
            if (!result.Accepted)
            {
                this.logger.LogWarning(
                    "Manual function command {CommandId} was not accepted: {Error}",
                    result.CommandId,
                    result.Error);
            }
        }
    }
}
