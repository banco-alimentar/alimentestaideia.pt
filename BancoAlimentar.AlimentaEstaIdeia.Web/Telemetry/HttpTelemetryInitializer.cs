// -----------------------------------------------------------------------
// <copyright file="HttpTelemetryInitializer.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry.Api
{
    using System.Security.Claims;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Api.Model;
    using Easypay.Rest.Client.Model;
    using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Define the EasyPay telemetry initializer to send extended telemetry data to AI.
    /// </summary>
    public class HttpTelemetryInitializer : TelemetryInitializerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpTelemetryInitializer"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">Http context accessor.</param>
        public HttpTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
            : base(httpContextAccessor)
        {
        }

        /// <summary>
        /// Initialize the telemetry.
        /// </summary>
        /// <param name="platformContext">Http context.</param>
        /// <param name="requestTelemetry">Current http request telemetry.</param>
        /// <param name="telemetry">Current telemetry object.</param>
        protected override void OnInitializeTelemetry(
            HttpContext platformContext,
            RequestTelemetry requestTelemetry,
            ITelemetry telemetry)
        {
            ISupportProperties operationTelemetry = telemetry as ISupportProperties;
            if (operationTelemetry != null)
            {
                if (platformContext.Items.TryGetValue(KeyNames.DonationSessionKey, out object donationId))
                {
                    string value = donationId.ToString();
                    if (operationTelemetry.Properties.ContainsKey(KeyNames.PropertyKey))
                    {
                        operationTelemetry.Properties[KeyNames.PropertyKey] = value;
                    }
                    else
                    {
                        operationTelemetry.Properties.Add(KeyNames.PropertyKey, value);
                    }
                }

                Sas.Model.Tenant tenant = platformContext.GetTenant();
                if (!operationTelemetry.Properties.ContainsKey(KeyNames.TenantId))
                {
                    operationTelemetry.Properties.Add(KeyNames.TenantId, tenant.Id.ToString());
                }

                if (!operationTelemetry.Properties.ContainsKey(KeyNames.TenantName))
                {
                    operationTelemetry.Properties.Add(KeyNames.TenantName, tenant.Name);
                }
            }

            // try
            // {
            //    if (platformContext.Session.IsAvailable)
            //    {
            //        telemetry.Context.Session.Id = platformContext.Session.Id;
            //    }
            // }
            // catch
            // {
            //    // this is the worst code that any developer can write, but.....
            //    // I don't care if the session is not ready yet, so ignoring this.
            // }
            if (platformContext.User != null)
            {
                var user = telemetry.Context.User;
                var claim = platformContext.User.FindFirst(ClaimTypes.NameIdentifier);
                if (claim != null)
                {
                    user.AuthenticatedUserId = claim.Value;
                }

                if (platformContext.Items.ContainsKey(KeyNames.CurrentUserKey))
                {
                    WebUser webUser = (WebUser)platformContext.Items[KeyNames.CurrentUserKey];
                    if (webUser != null)
                    {
                        user.Id = webUser.Id;
                    }
                }
            }

            if (platformContext.Items.ContainsKey(KeyNames.DonationIdKey))
            {
                operationTelemetry.Properties.Add("DonationId", platformContext.Items[KeyNames.DonationIdKey].ToString());
            }

            if (platformContext.Request.Path.StartsWithSegments(new PathString("/easypay/generic")))
            {
                if (platformContext.Items[KeyNames.GenericNotificationKey] is NotificationGeneric body)
                {
                    operationTelemetry.Properties.Add("GenericNotification-Id", body.Id.ToString());
                    operationTelemetry.Properties.Add("GenericNotification-Key", body.Key);
                    operationTelemetry.Properties.Add("GenericNotification-Type", body.Type?.ToString());
                    operationTelemetry.Properties.Add("GenericNotification-Status", body.Status?.ToString());
                }
            }

            if (platformContext.Request.Path.StartsWithSegments(new PathString("/easypay/payment")))
            {
                if (platformContext.Items[KeyNames.PaymentNotificationKey] is TransactionNotificationRequest body)
                {
                    operationTelemetry.Properties.Add("PaymentNotification-Id", body.Id.ToString());
                    operationTelemetry.Properties.Add("PaymentNotification-Key", body.Key);
                    operationTelemetry.Properties.Add("PaymentNotification-Method", body.Method);

                    // operationTelemetry.Properties.Add("PaymentNotification-Transaction-Key", body.Transaction?.Key);
                    // operationTelemetry.Properties.Add("PaymentNotification-Transaction-Id", body.Transaction?.Id.ToString());
                }
            }
        }
    }
}
