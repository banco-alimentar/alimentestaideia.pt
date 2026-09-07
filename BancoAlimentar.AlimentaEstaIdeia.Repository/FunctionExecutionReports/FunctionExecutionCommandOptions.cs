// -----------------------------------------------------------------------
// <copyright file="FunctionExecutionCommandOptions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports
{
    using System;
    using Microsoft.Extensions.Configuration;

    /// <summary>Configuration for the private manual-execution command queue.</summary>
    public sealed class FunctionExecutionCommandOptions
    {
        /// <summary>Gets or sets whether manual commands are accepted.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Gets or sets the queue storage connection string.</summary>
        public string ConnectionString { get; set; }

        /// <summary>Gets or sets the queue name.</summary>
        public string QueueName { get; set; } = "function-execution-commands";

        /// <summary>Gets or sets the maximum accepted message length.</summary>
        public int MaxMessageLength { get; set; } = 16 * 1024;

        /// <summary>Reads and validates options from configuration.</summary>
        /// <param name="configuration">Configuration source.</param>
        /// <returns>Validated options.</returns>
        public static FunctionExecutionCommandOptions Read(IConfiguration configuration)
        {
            var options = new FunctionExecutionCommandOptions();
            configuration?.GetSection("FunctionExecutionCommands").Bind(options);
            if (string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                options.ConnectionString = configuration?["AzureWebJobsStorage"];
            }

            if (string.IsNullOrWhiteSpace(options.QueueName)
                || options.QueueName.Length > 63
                || options.QueueName != options.QueueName.ToLowerInvariant())
            {
                throw new InvalidOperationException("FunctionExecutionCommands:QueueName must be a lowercase Azure Queue name.");
            }

            if (options.MaxMessageLength < 1024 || options.MaxMessageLength > 64 * 1024)
            {
                throw new InvalidOperationException("FunctionExecutionCommands:MaxMessageLength must be between 1024 and 65536.");
            }

            return options;
        }
    }
}
