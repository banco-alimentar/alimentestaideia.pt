// -----------------------------------------------------------------------
// <copyright file="Ignore404ErrorsTelemetryInitializer.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry
{
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// This class ignore 404 errors in Application Insights.
    /// </summary>
    public class Ignore404ErrorsTelemetryInitializer : ITelemetryInitializer
    {
        /// <summary>
        /// Sets the success to true for every request telemetry that status code is 404.
        /// </summary>
        /// <param name="telemetry">A reference to the <see cref="ITelemetry"/>.</param>
        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry is RequestTelemetry request)
            {
                if (request.ResponseCode == "404")
                {
                    request.Success = true;
                }
            }
        }
    }
}
