// -----------------------------------------------------------------------
// <copyright file="EasyPayLiveTestConfiguration.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Detects whether live Easypay API credentials are available for optional integration tests.
    /// </summary>
    internal static class EasyPayLiveTestConfiguration
    {
        /// <summary>
        /// Gets a value indicating whether live Easypay API credentials are configured.
        /// </summary>
        /// <param name="configuration">Application configuration.</param>
        /// <returns>True when Easypay settings are present and look valid.</returns>
        internal static bool IsConfigured(IConfiguration configuration)
        {
            string baseUrl = configuration["Easypay:BaseUrl"];
            string apiKey = configuration["Easypay:ApiKey"];
            string accountId = configuration["Easypay:AccountId"];

            if (string.IsNullOrWhiteSpace(baseUrl)
                || string.IsNullOrWhiteSpace(apiKey)
                || string.IsNullOrWhiteSpace(accountId))
            {
                return false;
            }

            if (baseUrl.Contains("#{", StringComparison.Ordinal)
                || apiKey.Contains("#{", StringComparison.Ordinal)
                || accountId.Contains("#{", StringComparison.Ordinal))
            {
                return false;
            }

            return Uri.TryCreate(baseUrl, UriKind.Absolute, out _);
        }
    }
}
