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
        private readonly IHttpContextAccessor httpContextAccessor;

        public DonationFlowTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public void Initialize(ITelemetry telemetry)
        {
            //if (telemetry != null &&
            //    httpContextAccessor.HttpContext != null &&
            //    httpContextAccessor.HttpContext.Session != null)
            //{
            //    if (telemetry is RequestTelemetry)
            //    {
            //        RequestTelemetry request = (RequestTelemetry)telemetry;
            //        request.Properties.Add("DonationSessionId", httpContextAccessor.HttpContext.Session.GetString(DonationSessionKey));
            //    }
            //}
        }
    }
}
