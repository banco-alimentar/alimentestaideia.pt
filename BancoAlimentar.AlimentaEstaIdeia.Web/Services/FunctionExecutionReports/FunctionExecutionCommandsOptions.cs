// -----------------------------------------------------------------------
// <copyright file="FunctionExecutionCommandsOptions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services.FunctionExecutionReports
{
    /// <summary>
    /// Configuration for the private Azure Storage Queue used to request a manual Function execution.
    /// </summary>
    public sealed class FunctionExecutionCommandsOptions
    {
        /// <summary>Configuration section name.</summary>
        public const string SectionName = "FunctionExecutionCommands";

        /// <summary>Gets or sets a value indicating whether manual execution requests are enabled.</summary>
        public bool Enabled { get; set; }

        /// <summary>Gets or sets the private queue storage connection string.</summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>Gets or sets the queue name.</summary>
        public string QueueName { get; set; } = "function-execution-commands";
    }
}
