namespace BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry
{
    using System;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Http;

    public class DonationFlowTelemetryInitializer : ITelemetryInitializer
    {
        public const string DonationSessionKey = "_donationSessionKey";
        private const string PropertyKey = "DonationSessionId";
        private readonly IHttpContextAccessor httpContextAccessor;

        public DonationFlowTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry != null &&
                httpContextAccessor.HttpContext != null)
            {
                ISupportProperties supportProperties = telemetry as ISupportProperties;
                if (supportProperties != null)
                {
                    object donationId = null;
                    if (httpContextAccessor.HttpContext.Items.TryGetValue(DonationSessionKey, out donationId))
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
