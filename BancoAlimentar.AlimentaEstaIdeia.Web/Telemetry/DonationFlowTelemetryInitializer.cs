// -----------------------------------------------------------------------
// <copyright file="DonationFlowTelemetryInitializer.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry
{
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// This is a <see cref="ITelemetryInitializer"/> for the donation flow.
    /// </summary>
    public class DonationFlowTelemetryInitializer : ITelemetryInitializer
    {
        /// <summary>
        /// Key used to persist the donation id.
        /// </summary>
        public const string DonationSessionKey = "_donationSessionKey";
        private const string PropertyKey = "DonationSessionId";
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
        /// <param name="telemetry">Telemetry object.</param>
        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry != null &&
                httpContextAccessor.HttpContext != null)
            {
                if (telemetry is ISupportProperties supportProperties)
                {
                    if (httpContextAccessor.HttpContext.Items.TryGetValue(DonationSessionKey, out object donationId))
                    {
                        string value = donationId.ToString();
                        if (supportProperties.Properties.ContainsKey(PropertyKey))
                        {
                            supportProperties.Properties[PropertyKey] = value;
                        }
                        else
                        {
                            supportProperties.Properties.Add(PropertyKey, value);
                        }
                    }
                }
            }
        }
    }
}
