// -----------------------------------------------------------------------
// <copyright file="UserAuthenticationTelemetryInitializer.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry
{
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// User authenticated telemetry initializer.
    /// </summary>
    public class UserAuthenticationTelemetryInitializer : ITelemetryInitializer
    {
        /// <summary>
        /// Key used for the current user.
        /// </summary>
        public const string CurrentUserKey = "__currentUserKey";
        private readonly IHttpContextAccessor httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserAuthenticationTelemetryInitializer"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">Http context accessor.</param>
        public UserAuthenticationTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
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
                httpContextAccessor.HttpContext != null &&
                httpContextAccessor.HttpContext.User != null)
            {
                var user = telemetry.Context.User;
                var claim = httpContextAccessor.HttpContext.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
                if (claim != null)
                {
                    user.AuthenticatedUserId = claim.Value;
                }

                if (httpContextAccessor.HttpContext.Items.ContainsKey(CurrentUserKey))
                {
                    WebUser webUser = (WebUser)httpContextAccessor.HttpContext.Items[CurrentUserKey];
                    if (webUser != null)
                    {
                        user.Id = webUser.Id;
                    }
                }

                telemetry.Context.Session.Id = (string)this.httpContextAccessor.HttpContext.Items[DonationTelemetryMiddleware.SessionIdKey];
            }
        }
    }
}
