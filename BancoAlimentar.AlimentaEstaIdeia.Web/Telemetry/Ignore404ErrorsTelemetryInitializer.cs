// -----------------------------------------------------------------------
// <copyright file="Ignore404ErrorsTelemetryInitializer.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry
{
    using System.Diagnostics;
    using OpenTelemetry;

    /// <summary>
    /// This class ignore 404 errors in Application Insights.
    /// </summary>
    public class Ignore404ErrorsTelemetryInitializer : BaseProcessor<Activity>
    {
        /// <summary>
        /// Sets the success to true for every request telemetry that status code is 404.
        /// </summary>
        /// <param name="telemetry">A reference to the telemetry activity.</param>
        public override void OnEnd(Activity telemetry)
        {
            if (telemetry.Kind == ActivityKind.Server &&
                telemetry.GetTagItem("http.response.status_code")?.ToString() == "404")
            {
                telemetry.SetStatus(ActivityStatusCode.Unset);
                telemetry.SetTag("error.type", null);
            }
        }
    }
}
