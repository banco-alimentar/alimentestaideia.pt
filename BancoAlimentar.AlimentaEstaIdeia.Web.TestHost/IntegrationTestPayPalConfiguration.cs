// -----------------------------------------------------------------------
// <copyright file="IntegrationTestPayPalConfiguration.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.TestHost
{
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Services;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using PayPalCheckoutSdk.Core;
    using PayPalCheckoutSdk.Orders;
    using PayPalHttp;

    /// <summary>
    /// Registers a stub PayPal checkout client for integration tests.
    /// </summary>
    public static class IntegrationTestPayPalConfiguration
    {
        /// <summary>
        /// Replaces <see cref="PayPalBuilder"/> with a stub that supports order create and capture.
        /// </summary>
        /// <param name="services">Application services.</param>
        /// <param name="approveUrl">Redirect URL returned in the order approve link.</param>
        public static void AddStubPayPalCheckoutWithCapture(
            IServiceCollection services,
            string approveUrl = "https://paypal.integration.test/approve")
        {
            services.RemoveAll(typeof(PayPalBuilder));
            services.AddScoped<PayPalBuilder>(serviceProvider =>
            {
                var builder = new PayPalBuilder(
                    serviceProvider.GetRequiredService<IConfiguration>(),
                    serviceProvider.GetRequiredService<IHttpContextAccessor>());
                var stubClient = new StubPayPalHttpClient(request =>
                {
                    if (request is OrdersCaptureRequest)
                    {
                        var captured = new Order
                        {
                            Id = System.Guid.NewGuid().ToString(),
                            Status = "COMPLETED",
                        };
                        return Task.FromResult(new PayPalHttp.HttpResponse(null, HttpStatusCode.OK, captured));
                    }

                    var order = new Order
                    {
                        Id = System.Guid.NewGuid().ToString(),
                        Links = new List<LinkDescription>
                        {
                            new LinkDescription
                            {
                                Rel = "approve",
                                Href = approveUrl,
                            },
                        },
                    };
                    return Task.FromResult(new PayPalHttp.HttpResponse(null, HttpStatusCode.Created, order));
                });
                builder.SetPayPalHttpClientOverride(stubClient);
                return builder;
            });
        }

        /// <summary>
        /// Replaces <see cref="PayPalBuilder"/> with one that returns a stub PayPal HTTP client.
        /// </summary>
        /// <param name="services">Application services.</param>
        /// <param name="approveUrl">Redirect URL returned in the order approve link.</param>
        public static void AddStubPayPalCheckout(IServiceCollection services, string approveUrl = "https://paypal.integration.test/approve")
        {
            services.RemoveAll(typeof(PayPalBuilder));
            services.AddScoped<PayPalBuilder>(serviceProvider =>
            {
                var builder = new PayPalBuilder(
                    serviceProvider.GetRequiredService<IConfiguration>(),
                    serviceProvider.GetRequiredService<IHttpContextAccessor>());
                var stubClient = new StubPayPalHttpClient(_ =>
                {
                    var order = new Order
                    {
                        Id = System.Guid.NewGuid().ToString(),
                        Links = new List<LinkDescription>
                        {
                            new LinkDescription
                            {
                                Rel = "approve",
                                Href = approveUrl,
                            },
                        },
                    };
                    return Task.FromResult(new PayPalHttp.HttpResponse(null, HttpStatusCode.Created, order));
                });
                builder.SetPayPalHttpClientOverride(stubClient);
                return builder;
            });
        }
    }
}
