// -----------------------------------------------------------------------
// <copyright file="FileNotFoundAzureStroageBlobFilter.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry.Filtering
{
    using System;
    using System.Diagnostics;
    using System.Linq;

    /// <summary>
    /// This class filter file not found errors in Azure Storage Blobs.
    /// </summary>
    public class FileNotFoundAzureStroageBlobFilter : ApplicationInsightsTelemetryFilter
    {
        /// <inheritdoc/>
        public override bool ShouldFilterTelemetry(Activity value)
        {
            return value.Kind == ActivityKind.Client &&
                value.Source.Name.StartsWith("Azure.Storage.Blobs", StringComparison.Ordinal) &&
                (value.GetTagItem("http.response.status_code")?.ToString() == "404" ||
                    value.GetTagItem("error.type")?.ToString() == "404") &&
                value.Tags.Any(tag => tag.Value?.Contains("wwwrooot", StringComparison.OrdinalIgnoreCase) == true);
        }
    }
}
