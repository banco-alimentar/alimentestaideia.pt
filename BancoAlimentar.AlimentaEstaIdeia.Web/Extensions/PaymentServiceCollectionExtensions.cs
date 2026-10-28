// -----------------------------------------------------------------------
// <copyright file="PaymentServiceCollectionExtensions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Extensions
{
    using System;
    using System.Threading.RateLimiting;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Services;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Pages;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Pages.Tenants;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services.EasyPay;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services.Invoices;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.RateLimiting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Registers payment, invoice, and mail services.
    /// </summary>
    public static class PaymentServiceCollectionExtensions
    {
        /// <summary>
        /// Adds services used by payment and invoice flows.
        /// </summary>
        /// <param name="services">The collection that receives payment registrations.</param>
        /// <param name="configuration">The host configuration.</param>
        /// <returns>The configured payment service collection.</returns>
        public static IServiceCollection AddPaymentServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddScoped<EasyPayBuilder>();
            services.Configure<EasyPayWebhookVerificationOptions>(
                configuration.GetSection(EasyPayWebhookVerificationOptions.SectionName));
            services.AddScoped<EasyPayApiCredentialsFactory>();
            services.AddScoped<IEasyPayWebhookVerifier, EasyPayApiWebhookVerifier>();
            services.AddScoped<PayPalBuilder>();
            services.AddScoped<IMail, Mail>();
            services.Configure<InvoiceDownloadOptions>(
                configuration.GetSection(InvoiceDownloadOptions.SectionName));
            services.AddSingleton<IInvoiceDownloadTokenService, InvoiceDownloadTokenService>();
            services.AddRateLimiter(rateLimiterOptions =>
            {
                rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                rateLimiterOptions.AddFixedWindowLimiter(
                    "invoice-download",
                    options =>
                    {
                        options.Window = TimeSpan.FromMinutes(1);
                        options.PermitLimit = 30;
                        options.QueueLimit = 0;
                    });
            });
            return services;
        }
    }
}
