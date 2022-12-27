// -----------------------------------------------------------------------
// <copyright file="SasTelemetryConfiguration.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------


namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.ApplicationInsight
{
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Sas Telemetry Configuration.
    /// </summary>
    public class SasTelemetryConfiguration :
        IConfigureOptions<TelemetryConfiguration>,
        IPostConfigureOptions<TelemetryConfiguration>,
        IOptions<TelemetryConfiguration>
    {
        private readonly IConfiguration configuration;
        private static readonly object LockObject = new object();
        private TelemetryConfiguration telemetryConfiguration;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryConfigurationOptions"/> class.
        /// </summary>
        /// <param name="configureOptions">Options to be configured.</param>
        /// <param name="applicationInsightsServiceOptions">User defined serviceOptions.</param>
        public SasTelemetryConfiguration(
            IEnumerable<IConfigureOptions<TelemetryConfiguration>> configureOptions,
            IOptions<ApplicationInsightsServiceOptions> applicationInsightsServiceOptions)
        {
            this.Value = TelemetryConfiguration.CreateDefault();

            var configureOptionsArray = configureOptions.ToArray();
            foreach (var c in configureOptionsArray)
            {
                c.Configure(this.Value);
            }

            if (applicationInsightsServiceOptions.Value.EnableActiveTelemetryConfigurationSetup)
            {
                lock (LockObject)
                {
                    // workaround for Microsoft/ApplicationInsights-dotnet#613
                    // as we expect some customers to use TelemetryConfiguration.Active together with dependency injection
                    // we make sure it has been set up, it must be done only once even if there are multiple Web Hosts in the process
                    if (!IsActiveConfigured(this.Value.InstrumentationKey))
                    {
                        foreach (var c in configureOptionsArray)
                        {
#pragma warning disable CS0618 // This must be maintained for backwards compatibility.
                            c.Configure(TelemetryConfiguration.Active);
#pragma warning restore CS0618
                        }
                    }
                }
            }
        }

        public TelemetryConfiguration Value
        {
            get
            {
                return this.telemetryConfiguration;
            }
            internal set
            {
                this.telemetryConfiguration = value;
            }
        }

        /// <inheritdoc/>
        public void Configure(TelemetryConfiguration options)
        {

        }

        /// <inheritdoc/>
        public void PostConfigure(string? name, TelemetryConfiguration options)
        {

        }

        /// <summary>
        /// Determines if TelemetryConfiguration.Active needs to be configured.
        /// </summary>
        /// <param name="instrumentationKey">Instrumentation key.</param>
        /// <returns>True is TelemertryConfiguration.Active was previously configured.</returns>
        private static bool IsActiveConfigured(string instrumentationKey)
        {
#pragma warning disable CS0618 // This must be maintained for backwards compatibility.
            var active = TelemetryConfiguration.Active;
#pragma warning restore CS0618
            if (string.IsNullOrEmpty(active.InstrumentationKey) && !string.IsNullOrEmpty(instrumentationKey))
            {
                return false;
            }

            if (active.TelemetryInitializers.Count <= 1)
            {
                return false;
            }

            return true;
        }
    }
}
