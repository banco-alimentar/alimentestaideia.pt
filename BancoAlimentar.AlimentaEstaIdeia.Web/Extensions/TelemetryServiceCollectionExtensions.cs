// -----------------------------------------------------------------------
// <copyright file="TelemetryServiceCollectionExtensions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Extensions
{
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.ApplicationInsight;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry.Api;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry.Filtering;
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.ApplicationInsights.DependencyCollector;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Registers application telemetry services.
    /// </summary>
    public static class TelemetryServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Application Insights processing.
        /// </summary>
        /// <param name="services">The collection that receives telemetry registrations.</param>
        /// <param name="rootConfiguration">The host configuration.</param>
        /// <returns>The configured telemetry service collection.</returns>
        public static IServiceCollection AddApplicationTelemetry(
            this IServiceCollection services,
            IConfiguration rootConfiguration)
        {
            services.AddApplicationInsightsTelemetry();

            services.Configure<TelemetryConfiguration>(telemetry => LocalEmailCapture.ConfigureTelemetry(rootConfiguration, telemetry));
            services.AddApplicationInsightsTelemetryProcessor<RemoveAzureStorageTelemetryFilter>();
            services.AddApplicationInsightsTelemetryProcessor<FileNotFoundAzureStroageBlobFilter>();
            services.AddApplicationInsightsTelemetryProcessor<WebApplicationStatusFilter>();
            services.ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((module, options) =>
            {
                module.EnableRequestIdHeaderInjectionInW3CMode = true;
                module.SetComponentCorrelationHttpHeaders = true;
            });
            services.AddSingleton<ITelemetryInitializer, Ignore404ErrorsTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, HttpTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, DonationFlowTelemetryInitializer>();

            // Application Insights registers options against IConfiguration, which is tenant-scoped here.
            services.RemoveAll(typeof(IConfigureOptions<ApplicationInsightsServiceOptions>));
            services.RemoveAll(typeof(IPostConfigureOptions<ApplicationInsightsServiceOptions>));
            ApplicationInsightsPostConfigureOptions optionsSetup =
                new ApplicationInsightsPostConfigureOptions(rootConfiguration);
            services.AddSingleton<IConfigureOptions<ApplicationInsightsServiceOptions>>(optionsSetup);
            services.AddSingleton<IPostConfigureOptions<ApplicationInsightsServiceOptions>>(optionsSetup);
            if (LocalEmailCapture.IsEnabled(rootConfiguration))
            {
                services.PostConfigure<ApplicationInsightsServiceOptions>(options => options.EnableQuickPulseMetricStream = false);
            }

            return services;
        }
    }
}
