// -----------------------------------------------------------------------
// <copyright file="WebApplicationStatusFilter.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry.Filtering
{
    using System.Linq;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// This class filter the /status endpoint if succeed.
    /// </summary>
    public class WebApplicationStatusFilter : ApplicationInsightsTelemetryFilter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebApplicationStatusFilter"/> class.
        /// </summary>
        /// <param name="next">Next Telemetry processor.</param>
        public WebApplicationStatusFilter(ITelemetryProcessor next)
            : base(next)
        {
        }

        /// <inheritdoc/>
        public override bool ShouldFilterTelemetry(ITelemetry value)
        {
            bool result = false;

            RequestTelemetry requestTelemetry = value as RequestTelemetry;
            if (requestTelemetry != null)
            {
                if (requestTelemetry.ResponseCode == "200" && requestTelemetry.Url.AbsolutePath == "/status")
                {
                    result = true;
                }

                if (requestTelemetry.Url.Segments.Contains("images") ||
                    requestTelemetry.Url.Segments.Contains("lib") ||
                    requestTelemetry.Url.Segments.Contains("js"))
                {
                    result = true;
                }
            }

            return result;
        }
    }
}
