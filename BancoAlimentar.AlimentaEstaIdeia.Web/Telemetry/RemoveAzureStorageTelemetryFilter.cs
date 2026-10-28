// -----------------------------------------------------------------------
// <copyright file="RemoveAzureStorageTelemetryFilter.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    public class RemoveAzureStorageTelemetryFilter : BaseProcessor<Activity>
    {
        /// <inheritdoc/>
        public override void OnEnd(Activity item)
        {
            if (item.Kind != ActivityKind.Client)
            {
                return;
            }

            if (item.GetTagItem("az.namespace")?.ToString() == "Microsoft.Storage" ||
                item.Status != ActivityStatusCode.Error)
            {
                item.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
            }
        }
    }
}