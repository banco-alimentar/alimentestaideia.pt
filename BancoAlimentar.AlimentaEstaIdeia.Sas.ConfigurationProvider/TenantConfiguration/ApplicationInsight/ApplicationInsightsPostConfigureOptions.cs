// -----------------------------------------------------------------------
// <copyright file="ApplicationInsightsPostConfigureOptions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.ApplicationInsight
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Application Insights options setup using host (root) configuration.
    /// Must not depend on scoped <see cref="TenantConfigurationRoot"/>.
    /// </summary>
    public class ApplicationInsightsPostConfigureOptions :
        IConfigureOptions<ApplicationInsightsServiceOptions>,
        IPostConfigureOptions<ApplicationInsightsServiceOptions>
    {
        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationInsightsPostConfigureOptions"/> class.
        /// </summary>
        /// <param name="configuration">Host/root configuration (singleton).</param>
        public ApplicationInsightsPostConfigureOptions(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        /// <inheritdoc/>
        public void Configure(ApplicationInsightsServiceOptions options)
        {
            // Binding from scoped tenant IConfiguration is handled in PostConfigure using root config.
        }

        /// <inheritdoc/>
        public void PostConfigure(string name, ApplicationInsightsServiceOptions options)
        {
            string tenantApplicationInsights = this.configuration["TenantApplicationInsights"];
            if (string.IsNullOrEmpty(tenantApplicationInsights))
            {
                tenantApplicationInsights = this.configuration["APPINSIGHTS_CONNECTIONSTRING"];
            }

#if DEBUG
			options.EnableAppServicesHeartbeatTelemetryModule = false;
			options.EnableAzureInstanceMetadataTelemetryModule = false;
#else
            options.EnableAppServicesHeartbeatTelemetryModule = true;
            options.EnableAzureInstanceMetadataTelemetryModule = true;
#endif

            options.ConnectionString = tenantApplicationInsights;
        }
    }
}
