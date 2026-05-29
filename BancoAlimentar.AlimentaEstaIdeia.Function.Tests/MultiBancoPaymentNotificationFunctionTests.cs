// -----------------------------------------------------------------------
// <copyright file="MultiBancoPaymentNotificationFunctionTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Function;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.Tests;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="MultiBancoPaymentNotificationFunction"/>.
    /// </summary>
    public class MultiBancoPaymentNotificationFunctionTests : IClassFixture<ServicesFixture>
    {
        private readonly ServicesFixture fixture;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiBancoPaymentNotificationFunctionTests"/> class.
        /// </summary>
        /// <param name="fixture">Shared repository test fixture.</param>
        public MultiBancoPaymentNotificationFunctionTests(ServicesFixture fixture)
        {
            this.fixture = fixture;
        }

        /// <summary>
        /// Calls the configured reminder endpoint for pending multibanco payments.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task ExecuteFunction_CallsReminderEndpointForPendingPayments()
        {
            var context = this.fixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var unitOfWork = this.fixture.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var donation = await context.Donations
                .Include(d => d.User)
                .FirstAsync(d => d.Id == this.fixture.DonationId);

            var payment = new MultiBankPayment
            {
                Created = DateTime.UtcNow.AddDays(-4),
                TransactionKey = Guid.NewGuid().ToString(),
                EasyPayPaymentId = Guid.NewGuid().ToString(),
                Donation = donation,
            };
            context.Payments.Add(payment);
            await context.SaveChangesAsync();

            var requestedUrls = new List<string>();
            var handler = new RecordingHttpMessageHandler(requestedUrls);
            var httpClient = new HttpClient(handler);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["ApiCertificateV3"] = IntegrationTestCredentials.ApiCertificateV3,
                    ["WebUrl"] = "https://integration.test/notifications/payment?multibankId={0}&key={1}",
                })
                .Build();

            var function = new MultiBancoPaymentNotificationFunction(
                TelemetryConfiguration.CreateDefault(),
                this.fixture.ServiceProvider);
            FunctionTestHelper.SetConfiguration(function, configuration);
            FunctionTestHelper.SetHttpClient(function, httpClient);

            await function.ExecuteFunction(unitOfWork, context);

            Assert.Contains(requestedUrls, url => url.Contains($"multibankId={payment.Id}", StringComparison.Ordinal));
            Assert.Contains(requestedUrls, url => url.Contains(IntegrationTestCredentials.ApiCertificateV3, StringComparison.Ordinal));
        }

        private sealed class RecordingHttpMessageHandler : HttpMessageHandler
        {
            private readonly List<string> requestedUrls;

            public RecordingHttpMessageHandler(List<string> requestedUrls)
            {
                this.requestedUrls = requestedUrls;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                this.requestedUrls.Add(request.RequestUri?.ToString() ?? string.Empty);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    RequestMessage = request,
                });
            }
        }
    }
}
