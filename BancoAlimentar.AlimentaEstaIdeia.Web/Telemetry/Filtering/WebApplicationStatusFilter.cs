// -----------------------------------------------------------------------
// <copyright file="WebApplicationStatusFilter.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry.Filtering
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// This class filter the /status endpoint if succeed.
    /// </summary>
    public class WebApplicationStatusFilter : ApplicationInsightsTelemetryFilter
    {
        /// <inheritdoc/>
        public override bool ShouldFilterTelemetry(Activity value)
        {
            if (value.Kind != ActivityKind.Server)
            {
                return false;
            }

            string path = value.GetTagItem("url.path")?.ToString() ?? string.Empty;
            string statusCode = value.GetTagItem("http.response.status_code")?.ToString();
            return (statusCode == "200" && path == "/status") ||
                path.StartsWith("/images/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase);
        }
    }
}
