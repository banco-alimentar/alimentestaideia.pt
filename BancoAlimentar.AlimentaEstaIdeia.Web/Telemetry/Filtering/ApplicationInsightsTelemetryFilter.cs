// -----------------------------------------------------------------------
// <copyright file="ApplicationInsightsTelemetryFilter.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry.Filtering
{
    using System.Diagnostics;
    using OpenTelemetry;

    /// <summary>
    /// This class is used to filter telemetry.
    /// </summary>
    public abstract class ApplicationInsightsTelemetryFilter : BaseProcessor<Activity>
    {
        /// <inheritdoc/>
        public override void OnEnd(Activity item)
        {
            if (this.ShouldFilterTelemetry(item))
            {
                item.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
            }
        }

        /// <summary>
        /// This method decided if the telemetry should be filter or not.
        /// </summary>
        /// <param name="value">A reference to the telemetry activity.</param>
        /// <returns>True if the telemetry item is going to be filtered, false otherwise.</returns>
        public abstract bool ShouldFilterTelemetry(Activity value);
    }
}
