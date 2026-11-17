// -----------------------------------------------------------------------
// <copyright file="ApplicationInsightsTelemetryFilter.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry.Filtering
{
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// This class is used to filter telemetry.
    /// </summary>
    public abstract class ApplicationInsightsTelemetryFilter : ITelemetryProcessor
    {
        private readonly ITelemetryProcessor next;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationInsightsTelemetryFilter"/> class.
        /// </summary>
        /// <param name="next">Next telemetry filter in the chain.</param>
        public ApplicationInsightsTelemetryFilter(ITelemetryProcessor next)
        {
            this.next = next;
        }

        /// <inheritdoc/>
        public void Process(ITelemetry item)
        {
            if (this.ShouldFilterTelemetry(item))
            {
                return;
            }

            this.next.Process(item);
        }

        /// <summary>
        /// This method decided if the telemetry should be filter or not.
        /// </summary>
        /// <param name="value">A reference to the <see cref="ITelemetry"/>.</param>
        /// <returns>True if the telemetry item is going to be filtered, false otherwise.</returns>
        public abstract bool ShouldFilterTelemetry(ITelemetry value);
    }
}
