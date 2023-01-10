// -----------------------------------------------------------------------
// <copyright file="TenantTelemetryProcessor.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.ApplicationInsight;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Sets the specific telemetry Instrumentation Key per tenant.
    /// </summary>
    public class TenantTelemetryProcessor : ITelemetryProcessor
    {
        private const string InstrumentationKeyName = "InstrumentationKey";
        private static readonly char[] SplitSemicolon = new char[] { ';' };
        private readonly ITelemetryProcessor next;
        private readonly IHttpContextAccessor httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantTelemetryProcessor"/> class.
        /// </summary>
        public TenantTelemetryProcessor(ITelemetryProcessor next, IHttpContextAccessor httpContextAccessor)
        {
            this.next = next;
            this.httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc/>
        public void Process(ITelemetry item)
        {
            if (this.httpContextAccessor.HttpContext != null)
            {
                IDictionary<string, string>? tenantConfiguration = this.httpContextAccessor.HttpContext.GetTenantSpecificConfiguration();
                if (tenantConfiguration != null)
                {
                    string? applicationInsightsConnectionString = null;
                    if (tenantConfiguration.TryGetValue(
                        ApplicationInsightsPostConfigureOptions.ApplicationInsightsTenantConfigurationKeyName,
                        out applicationInsightsConnectionString))
                    {
                        IDictionary<string, string> connectionStringElements = Parse(applicationInsightsConnectionString);
                        if (connectionStringElements.ContainsKey(InstrumentationKeyName))
                        {
                            string instrumentationKey = connectionStringElements[InstrumentationKeyName];
                            item.Context.InstrumentationKey = instrumentationKey;
                        }
                    }
                }
            }

            this.next.Process(item);
        }

        /// <summary>
        /// Parse a given string and return a dictionary of the key/value pairs.
        /// This method will do some validation and throw exceptions if the input string does not conform to the definition of a configuration string.
        /// </summary>
        /// <param name="configString">Input string to be parsed. This string cannot be null or empty. This string will be checked for validity.</param>
        /// <returns>Returns a dictionary of Key/Value pairs. Keys are not case sensitive.</returns>
        /// <remarks>This is used by both Connection Strings and Self-Diagnostics configuration.</remarks>
        private static IDictionary<string, string> Parse(string configString)
        {
            if (configString == null)
            {
                string message = "Input cannot be null.";
                throw new ArgumentNullException(message);
            }
            else if (string.IsNullOrWhiteSpace(configString))
            {
                string message = "Input cannot be empty.";
                throw new ArgumentException(message);
            }

            var keyValuePairs = configString.Split(SplitSemicolon, StringSplitOptions.RemoveEmptyEntries);
            var dictionary = new Dictionary<string, string>(keyValuePairs.Length, StringComparer.OrdinalIgnoreCase);

            foreach (var pair in keyValuePairs)
            {
                var keyAndValue = pair.Split('=');
                if (keyAndValue.Length != 2)
                {
                    string message = "Input contains invalid delimiters and cannot be parsed. Expected example: 'key1=value1;key2=value2;key3=value3'.";
                    throw new ArgumentException(message);
                }

                var key = keyAndValue[0].Trim();
                var value = keyAndValue[1].Trim();

                if (dictionary.ContainsKey(key))
                {
                    string message = "Input cannot contain duplicate keys. Duplicate key: '{key}'.";
                    throw new ArgumentException(message);
                }

                dictionary.Add(key, value);
            }

            return dictionary;
        }
    }
}
