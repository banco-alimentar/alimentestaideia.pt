// -----------------------------------------------------------------------
// <copyright file="RemoveAzureStorageTelemetryFilter.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    public class RemoveAzureStorageTelemetryFilter : ITelemetryProcessor
    {
        private readonly ITelemetryProcessor next;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveAzureStorageTelemetryFilter"/> class.
        /// </summary>
        /// <param name="next">Next telemetry processor.</param>
        public RemoveAzureStorageTelemetryFilter(ITelemetryProcessor next)
        {
            this.next = next;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveAzureStorageTelemetryFilter"/> class.
        /// </summary>
        public RemoveAzureStorageTelemetryFilter()
        {
        }

        /// <inheritdoc/>
        public void Process(ITelemetry item)
        {
            // To filter out an item, return without calling the next processor.
            if (!Filter(item))
            {
                return;
            }

            this.next.Process(item);
        }

        /// <summary>
        /// Filters the telemetry.
        /// </summary>
        /// <param name="item">Telemetry item.</param>
        /// <returns>False to be filtered, true otherwise.</returns>
        private bool Filter(ITelemetry item)
        {
            DependencyTelemetry dependency = item as DependencyTelemetry;
            if (dependency == null)
            {
                return true;
            }

            if (dependency.Type == "Microsoft.Storage")
            {
                return false;
            }

            return dependency.Success != true;
        }
    }
}