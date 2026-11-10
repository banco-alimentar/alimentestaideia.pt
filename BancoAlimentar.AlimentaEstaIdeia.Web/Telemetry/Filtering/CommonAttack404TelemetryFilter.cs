// -----------------------------------------------------------------------
// <copyright file="CommonAttack404TelemetryFilter.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry.Filtering
{
    using System;
    using System.Linq;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// Filters 404 requests for paths probing technologies that this site does not use.
    /// </summary>
    public class CommonAttack404TelemetryFilter : ApplicationInsightsTelemetryFilter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommonAttack404TelemetryFilter"/> class.
        /// </summary>
        /// <param name="next">Next telemetry processor.</param>
        public CommonAttack404TelemetryFilter(ITelemetryProcessor next)
            : base(next)
        {
        }

        /// <inheritdoc/>
        public override bool ShouldFilterTelemetry(ITelemetry value)
        {
            if (value is not RequestTelemetry requestTelemetry ||
                requestTelemetry.ResponseCode != "404" ||
                requestTelemetry.Url == null)
            {
                return false;
            }

            string[] pathSegments = requestTelemetry.Url.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            return pathSegments.Any(IsCommonAttackPathSegment);
        }

        private static bool IsCommonAttackPathSegment(string segment)
        {
            return segment.Equals(".env", StringComparison.OrdinalIgnoreCase) ||
                segment.StartsWith(".env.", StringComparison.OrdinalIgnoreCase) ||
                segment.Equals(".git", StringComparison.OrdinalIgnoreCase) ||
                segment.Equals(".svn", StringComparison.OrdinalIgnoreCase) ||
                segment.Equals("wordpress", StringComparison.OrdinalIgnoreCase) ||
                segment.StartsWith("wp-", StringComparison.OrdinalIgnoreCase) ||
                segment.Equals("xmlrpc.php", StringComparison.OrdinalIgnoreCase) ||
                segment.EndsWith(".php", StringComparison.OrdinalIgnoreCase) ||
                segment.Equals("cgi-bin", StringComparison.OrdinalIgnoreCase);
        }
    }
}
