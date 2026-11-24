// -----------------------------------------------------------------------
// <copyright file="HttpTelemetryInitializerTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.Services
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Api.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry.Api;
    using Easypay.Rest.Client.Model;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Http;
    using Xunit;

    /// <summary>
    /// Tests repeated telemetry enrichment without starting a web host.
    /// </summary>
    public class HttpTelemetryInitializerTests
    {
        /// <summary>
        /// Adds payment properties once and preserves any existing values.
        /// </summary>
        /// <param name="hasExistingProperties">Whether telemetry already contains some properties.</param>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void InitializeTelemetry_PaymentNotificationIsIdempotent(bool hasExistingProperties)
        {
            var context = CreateContext("/easypay/payment");
            var notification = new TransactionNotificationRequest(id: Guid.NewGuid(), key: "payment-key", method: "MBW");
            context.Items[KeyNames.PaymentNotificationKey] = notification;
            var telemetry = new RequestTelemetry();
            if (hasExistingProperties)
            {
                telemetry.Properties["PaymentNotification-Id"] = "existing-id";
                telemetry.Properties["DonationId"] = "existing-donation";
            }

            var initializer = new TestHttpTelemetryInitializer();
            initializer.InitializeTelemetry(context, telemetry);
            initializer.InitializeTelemetry(context, telemetry);

            Assert.Equal(hasExistingProperties ? "existing-id" : notification.Id.ToString(), telemetry.Properties["PaymentNotification-Id"]);
            Assert.Equal(hasExistingProperties ? "existing-donation" : "42", telemetry.Properties["DonationId"]);
            Assert.Equal(notification.Key, telemetry.Properties["PaymentNotification-Key"]);
            Assert.Equal(notification.Method, telemetry.Properties["PaymentNotification-Method"]);
        }

        /// <summary>
        /// Adds generic notification properties once and preserves any existing values.
        /// </summary>
        /// <param name="hasExistingProperties">Whether telemetry already contains some properties.</param>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void InitializeTelemetry_GenericNotificationIsIdempotent(bool hasExistingProperties)
        {
            var context = CreateContext("/easypay/generic");
            var notification = new NotificationGeneric(
                id: Guid.NewGuid(),
                key: "generic-key",
                type: NotificationGeneric.TypeEnum.Capture,
                status: NotificationGeneric.StatusEnum.Success);
            context.Items[KeyNames.GenericNotificationKey] = notification;
            var telemetry = new RequestTelemetry();
            if (hasExistingProperties)
            {
                telemetry.Properties["GenericNotification-Id"] = "existing-id";
                telemetry.Properties["DonationId"] = "existing-donation";
            }

            var initializer = new TestHttpTelemetryInitializer();
            initializer.InitializeTelemetry(context, telemetry);
            initializer.InitializeTelemetry(context, telemetry);

            Assert.Equal(hasExistingProperties ? "existing-id" : notification.Id.ToString(), telemetry.Properties["GenericNotification-Id"]);
            Assert.Equal(hasExistingProperties ? "existing-donation" : "42", telemetry.Properties["DonationId"]);
            Assert.Equal(notification.Key, telemetry.Properties["GenericNotification-Key"]);
            Assert.Equal(notification.Type.ToString(), telemetry.Properties["GenericNotification-Type"]);
            Assert.Equal(notification.Status.ToString(), telemetry.Properties["GenericNotification-Status"]);
        }

        private static DefaultHttpContext CreateContext(string path)
        {
            var context = new DefaultHttpContext();
            context.Request.Path = path;
            context.SetTenant(new Tenant { Id = 1, Name = "test-tenant" });
            context.Items[KeyNames.DonationIdKey] = 42;
            return context;
        }

        private sealed class TestHttpTelemetryInitializer : HttpTelemetryInitializer
        {
            public TestHttpTelemetryInitializer()
                : base(new HttpContextAccessor())
            {
            }

            public void InitializeTelemetry(HttpContext context, ITelemetry telemetry)
            {
                this.OnInitializeTelemetry(context, new RequestTelemetry(), telemetry);
            }
        }
    }
}
