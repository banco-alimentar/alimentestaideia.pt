// -----------------------------------------------------------------------
// <copyright file="FileNotFoundAzureStroageBlobFilter.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry.Filtering
{
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;

    /// <summary>
    /// This class filter file not found errors in Azure Storage Blobs.
    /// </summary>
    public class FileNotFoundAzureStroageBlobFilter : ApplicationInsightsTelemetryFilter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileNotFoundAzureStroageBlobFilter"/> class.
        /// </summary>
        /// <param name="next">Next telemetry processor.</param>
        public FileNotFoundAzureStroageBlobFilter(ITelemetryProcessor next)
            : base(next)
        {
        }

        /// <inheritdoc/>
        public override bool ShouldFilterTelemetry(ITelemetry value)
        {
            bool result = false;

            DependencyTelemetry operationTelemetry = value as DependencyTelemetry;
            if (operationTelemetry != null &&
                operationTelemetry.Type == "Azure blob" &&
                operationTelemetry.ResultCode == "404" &&
                operationTelemetry.Properties.ContainsKey("Container") &&
                operationTelemetry.Properties["Container"].Contains("wwwrooot"))
            {
                result = true;
            }

            return result;
        }
    }
}
