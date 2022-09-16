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
	/// Application Insights Configuration Options.
	/// </summary>
	public class ApplicationInsightsPostConfigureOptions : IPostConfigureOptions<ApplicationInsightsServiceOptions>
	{
		private readonly IConfiguration configuration;

		/// <summary>
		/// Initializes a new instance of the <see cref="ApplicationInsightsPostConfigureOptions"/> class.
		/// </summary>
		/// <param name="configuration">Configuration.</param>
		public ApplicationInsightsPostConfigureOptions(IConfiguration configuration)
		{
			this.configuration = configuration;
		}

		/// <summary>
		/// Configure Application Insights.
		/// </summary>
		/// <param name="name">Name of the configuration.</param>
		/// <param name="options">Options.</param>
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
