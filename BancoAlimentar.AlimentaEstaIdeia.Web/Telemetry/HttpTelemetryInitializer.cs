// -----------------------------------------------------------------------
// <copyright file="HttpTelemetryInitializer.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry.Api
{
    using System.Diagnostics;
    using System.Security.Claims;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Api.Model;
    using Easypay.Rest.Client.Model;
    using Microsoft.AspNetCore.Http;
    using OpenTelemetry;

    /// <summary>
    /// Define the EasyPay telemetry initializer to send extended telemetry data to AI.
    /// </summary>
    public class HttpTelemetryInitializer : BaseProcessor<Activity>
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpTelemetryInitializer"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">Http context accessor.</param>
        public HttpTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Initialize the telemetry.
        /// </summary>
        /// <param name="telemetry">Current telemetry object.</param>
        public override void OnEnd(Activity telemetry)
        {
            HttpContext platformContext = this.httpContextAccessor.HttpContext;
            if (platformContext == null || telemetry.Kind != ActivityKind.Server)
            {
                return;
            }

            if (platformContext.Items.TryGetValue(KeyNames.DonationSessionKey, out object donationId))
            {
                telemetry.SetTag(KeyNames.PropertyKey, donationId.ToString());
            }

            Sas.Model.Tenant tenant = platformContext.GetTenant();
            telemetry.SetTag(KeyNames.TenantId, tenant.Id.ToString());
            telemetry.SetTag(KeyNames.TenantName, tenant.Name);

            if (platformContext.User != null)
            {
                var claim = platformContext.User.FindFirst(ClaimTypes.NameIdentifier);
                if (claim != null)
                {
                    telemetry.SetTag("enduser.id", claim.Value);
                }

                if (platformContext.Items.ContainsKey(KeyNames.CurrentUserKey))
                {
                    WebUser webUser = (WebUser)platformContext.Items[KeyNames.CurrentUserKey];
                    if (webUser != null)
                    {
                        telemetry.SetTag("enduser.pseudo.id", webUser.Id);
                    }
                }
            }

            if (platformContext.Items.ContainsKey(KeyNames.DonationIdKey))
            {
                telemetry.SetTag("DonationId", platformContext.Items[KeyNames.DonationIdKey].ToString());
            }

            if (platformContext.Request.Path.StartsWithSegments(new PathString("/easypay/generic")))
            {
                if (platformContext.Items[KeyNames.GenericNotificationKey] is NotificationGeneric body)
                {
                    telemetry.SetTag("GenericNotification-Id", body.Id.ToString());
                    telemetry.SetTag("GenericNotification-Key", body.Key);
                    telemetry.SetTag("GenericNotification-Type", body.Type?.ToString());
                    telemetry.SetTag("GenericNotification-Status", body.Status?.ToString());
                }
            }

            if (platformContext.Request.Path.StartsWithSegments(new PathString("/easypay/payment")))
            {
                if (platformContext.Items[KeyNames.PaymentNotificationKey] is TransactionNotificationRequest body)
                {
                    telemetry.SetTag("PaymentNotification-Id", body.Id.ToString());
                    telemetry.SetTag("PaymentNotification-Key", body.Key);
                    telemetry.SetTag("PaymentNotification-Method", body.Method);
                }
            }
        }
    }
}
