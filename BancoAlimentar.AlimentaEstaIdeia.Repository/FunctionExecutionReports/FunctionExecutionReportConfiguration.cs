// -----------------------------------------------------------------------
// <copyright file="FunctionExecutionReportConfiguration.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports
{
    using System;
    using Microsoft.Extensions.Configuration;

    /// <summary>Creates validated report options from application configuration.</summary>
    public static class FunctionExecutionReportConfiguration
    {
        /// <summary>Loads options and applies the existing Azure Storage fallback.</summary>
        /// <param name="configuration">Application configuration.</param>
        /// <returns>Validated options.</returns>
        public static FunctionExecutionReportOptions Create(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            FunctionExecutionReportOptions options = new FunctionExecutionReportOptions();
            IConfigurationSection section = configuration.GetSection("FunctionExecutionReports");
            section.Bind(options);

            if (string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                options.ConnectionString = configuration["AzureStorage:ConnectionString"] ?? string.Empty;
            }

            FunctionExecutionReportOptions.Validate(options);
            return options;
        }
    }
}
