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
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Application Insights Configuration Options.
    /// </summary>
    public class ApplicationInsightsPostConfigureOptions
    {
        /// <summary>
        /// Configure Application Insights.
        /// </summary>
        /// <param name="name">Name of the configuration.</param>
        /// <param name="options">Options.</param>
        public static TelemetryClient ConfigureTelemetryClient(IServiceProvider serviceProvider)
        {
            IHttpContextAccessor httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
            TelemetryConfiguration telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
            
            if (httpContextAccessor.HttpContext != null)
            {
                Tenant tenant = httpContextAccessor.HttpContext.GetTenant();
                if (tenant != null)
                {
                    IConfiguration configuration = serviceProvider.GetRequiredService<IConfiguration>();
                    string tenantApplicationInsights = configuration["TenantApplicationInsights"];
                    if (string.IsNullOrEmpty(tenantApplicationInsights))
                    {
                        tenantApplicationInsights = configuration["APPINSIGHTS_CONNECTIONSTRING"];
                    }

                    telemetryConfiguration.ConnectionString = tenantApplicationInsights;
                }
            }

            return new TelemetryClient(telemetryConfiguration);
        }
    }
}
