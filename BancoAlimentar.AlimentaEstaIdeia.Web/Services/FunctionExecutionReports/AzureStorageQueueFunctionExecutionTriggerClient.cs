// -----------------------------------------------------------------------
// <copyright file="AzureStorageQueueFunctionExecutionTriggerClient.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services.FunctionExecutionReports
{
    using System;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Storage.Queues;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Queues allow-listed manual Function execution commands without exposing queue credentials to the browser.
    /// </summary>
    public sealed class AzureStorageQueueFunctionExecutionTriggerClient : IFunctionExecutionTriggerClient
    {
        private const string ManualTriggerSource = "AdminManualTrigger";

        private readonly FunctionExecutionCommandsOptions options;
        private readonly ILogger<AzureStorageQueueFunctionExecutionTriggerClient> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureStorageQueueFunctionExecutionTriggerClient"/> class.
        /// </summary>
        /// <param name="options">Queue options.</param>
        /// <param name="logger">Logger.</param>
        public AzureStorageQueueFunctionExecutionTriggerClient(
            FunctionExecutionCommandsOptions options,
            ILogger<AzureStorageQueueFunctionExecutionTriggerClient> logger)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<FunctionExecutionTriggerResult> TriggerAsync(
            string functionKey,
            CancellationToken cancellationToken = default)
        {
            if (FunctionExecutionReportCatalog.Find(functionKey) == null)
            {
                return FunctionExecutionTriggerResult.Failed("The selected function is not available.");
            }

            if (!this.options.Enabled
                || string.IsNullOrWhiteSpace(this.options.ConnectionString)
                || string.IsNullOrWhiteSpace(this.options.QueueName))
            {
                return FunctionExecutionTriggerResult.Failed("Manual function execution is not configured.");
            }

            string commandId = Guid.NewGuid().ToString("D");
            var command = new FunctionExecutionCommand
            {
                SchemaVersion = 1,
                FunctionKey = functionKey,
                CommandId = commandId,
                RequestedAtUtc = DateTime.UtcNow,
                TriggerType = ManualTriggerSource,
                CorrelationId = commandId,
            };

            try
            {
                var client = new QueueClient(
                    this.options.ConnectionString,
                    this.options.QueueName,
                    new QueueClientOptions
                    {
                        MessageEncoding = QueueMessageEncoding.Base64,
                    });

                await client.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                await client.SendMessageAsync(
                    JsonSerializer.Serialize(command),
                    cancellationToken).ConfigureAwait(false);

                return FunctionExecutionTriggerResult.Succeeded("The function execution was queued.");
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                this.logger.LogError(
                    exception,
                    "Could not queue manual execution request for catalog function {FunctionKey}.",
                    functionKey);
                return FunctionExecutionTriggerResult.Failed("The function execution could not be queued.");
            }
        }
    }
}
