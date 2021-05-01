// -----------------------------------------------------------------------
// <copyright file="DonationTelemetryMiddlewareExtensions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry
{
    using Microsoft.AspNetCore.Builder;

    public static class DonationTelemetryMiddlewareExtensions
    {
        public static IApplicationBuilder UseDonationTelemetryMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DonationTelemetryMiddleware>();
        }
    }
}
