// -----------------------------------------------------------------------
// <copyright file="DonationTelemetryMiddlewareExtensions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry
{
    using Microsoft.AspNetCore.Builder;

    /// <summary>
    /// Donation telemetry middleware extension.
    /// </summary>
    public static class DonationTelemetryMiddlewareExtensions
    {
        /// <summary>
        /// Adds the <see cref="DonationTelemetryMiddleware"/> to the Application builder.
        /// </summary>
        /// <param name="builder">Application builder.</param>
        /// <returns>A reference to the <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseDonationTelemetryMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DonationTelemetryMiddleware>();
        }
    }
}
