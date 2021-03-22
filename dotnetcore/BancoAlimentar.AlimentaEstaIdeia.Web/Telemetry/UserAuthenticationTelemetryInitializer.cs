namespace BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry
{
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Http;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class UserAuthenticationTelemetryInitializer : ITelemetryInitializer
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public UserAuthenticationTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry != null &&
                httpContextAccessor.HttpContext != null &&
                httpContextAccessor.HttpContext.User != null)
            {
                var user = telemetry.Context.User;
                var claim = httpContextAccessor.HttpContext.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
                if (claim != null)
                {
                    user.AuthenticatedUserId = claim.Value;
                }
            }
        }
    }
}
