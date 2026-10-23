// -----------------------------------------------------------------------
// <copyright file="DonationFlowTelemetryInitializer.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry
{
    using System.Diagnostics;
    using Microsoft.AspNetCore.Http;
    using OpenTelemetry;

    /// <summary>
    /// Enriches donation flow activities.
    /// </summary>
    public class DonationFlowTelemetryInitializer : BaseProcessor<Activity>
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="DonationFlowTelemetryInitializer"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">Http context accesor.</param>
        public DonationFlowTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Initialize the telemetry initializer.
        /// </summary>
        /// <param name="telemetry">Telemetry activity.</param>
        public override void OnEnd(Activity telemetry)
        {
            if (this.httpContextAccessor.HttpContext?.Items.TryGetValue(
                KeyNames.DonationSessionKey,
                out object donationId) == true)
            {
                telemetry.SetTag(KeyNames.PropertyKey, donationId.ToString());
            }
        }
    }
}
