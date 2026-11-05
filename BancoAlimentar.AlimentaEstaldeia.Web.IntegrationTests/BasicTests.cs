// -----------------------------------------------------------------------
// <copyright file="BasicTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.IntegrationTests
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Middleware;
    using BancoAlimentar.AlimentaEstaIdeia.Testing.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry.Api;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry.Filtering;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.AspNetCore.Routing.Matching;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Class for basic 200 HTTP status code tests.
    /// </summary>
    public class BasicTests
        : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient client;
        private readonly CustomWebApplicationFactory factory;
        private readonly ITestOutputHelper outputHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="BasicTests"/> class.
        /// </summary>
        /// <param name="factory">Factory class.</param>
        /// <param name="outputHelper">Test output helper.</param>
        public BasicTests(CustomWebApplicationFactory factory, ITestOutputHelper outputHelper)
        {
            this.factory = factory;
            this.outputHelper = outputHelper;
            this.client = factory.CreateClient();
        }

        /// <summary>
        /// Checks if for those endpoints we get a 200-209 status code.
        /// </summary>
        /// <param name="url">The url to check against.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Theory]
        [InlineData("/")]
        [InlineData("/Index")]
        [InlineData("/Donation")]
        [InlineData("/Maintenance")]
        [InlineData("/Identity/Account/Register")]
        [InlineData("/Identity/Account/Login")]
        [InlineData("/Identity/Account/Login?donate=true")]
        [InlineData("/Identity/Account/ForgotPassword")]
        [InlineData("/Identity/Account/ResendEmailConfirmation")]
        public async Task Get_EndpointsReturnSuccessAndCorrectContentType(string url)
        {
            // Act
            var response = await this.client.GetAsync(url);

            // Assert
            if (response.IsSuccessStatusCode)
            {
                response.EnsureSuccessStatusCode(); // Status Code 200-299
                Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType.ToString());
            }
            else
            {
                string body = await response.Content.ReadAsStringAsync();
                this.outputHelper.WriteLine("Body");
                this.outputHelper.WriteLine(body);
                this.outputHelper.WriteLine("EndBody");
                this.outputHelper.WriteLine($"RequestUri {string.Concat(this.client.BaseAddress, url)}");
                this.outputHelper.WriteLine($"Statuscode {response.StatusCode}");
                this.outputHelper.WriteLine($"ReasonPhrase {response.ReasonPhrase}");
                foreach (var item in response.Headers)
                {
                    this.outputHelper.WriteLine($"Header Name: {item.Key} | Value: {string.Join(',', item.Value)}");
                }

                throw new InvalidOperationException(body);
            }
        }

        /// <summary>
        /// Checks the shared donation layout and form accessibility baseline.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task Get_DonationUsesAccessibleLocalizedLayout()
        {
            // Act
            var response = await this.client.GetAsync("/Donation");
            response.EnsureSuccessStatusCode();
            var document = await HtmlHelpers.GetDocumentAsync(response);

            // Assert
            Assert.Equal("en", document.DocumentElement.GetAttribute("lang"));

            string viewport = document.QuerySelector("meta[name='viewport']")?.GetAttribute("content");
            Assert.DoesNotContain("maximum-scale", viewport, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("user-scalable", viewport, StringComparison.OrdinalIgnoreCase);

            Assert.NotNull(document.QuerySelector("script[src^='/lib/jquery/jquery.min.js']"));
            Assert.NotNull(document.QuerySelector("script[src^='/lib/bootstrap/js/bootstrap.bundle.min.js']"));
            Assert.Null(document.QuerySelector("script[src^='https://ajax.aspnetcdn.com/ajax/jQuery']"));

            Assert.NotNull(document.QuerySelector("label[for='FoodBankId']"));
            Assert.NotNull(document.QuerySelector("label[for='Name']"));
            Assert.NotNull(document.QuerySelector("label[for='Email']"));
            Assert.NotNull(document.QuerySelector("label[for='Country']"));
            Assert.NotNull(document.QuerySelector("button#donationSubmit[type='submit']"));
            Assert.NotNull(document.QuerySelector("#total[role='status'][aria-live='polite']"));
            Assert.NotNull(document.QuerySelector("a.skip-link[href='#main-content']"));
            Assert.NotNull(document.QuerySelector("main#main-content[tabindex='-1']"));
            Assert.NotNull(document.QuerySelector("h1.text5"));
            Assert.NotNull(document.QuerySelector("button.navbar-toggler[aria-controls='primary-navigation']"));
            Assert.NotNull(document.QuerySelector("#primary-navigation.navbar-collapse"));
            Assert.NotNull(document.QuerySelector("#donation-validation-summary[role='alert'][aria-live='assertive'][tabindex='-1']"));
            Assert.Equal("Name-error", document.QuerySelector("#Name")?.GetAttribute("aria-describedby"));

            for (int index = 1; index <= 6; index++)
            {
                var quantity = document.QuerySelector($"#field{index}");
                Assert.False(string.IsNullOrWhiteSpace(quantity?.GetAttribute("aria-label")));
                Assert.Equal("0", quantity?.GetAttribute("min"));
                Assert.NotNull(document.QuerySelector($"button.more[aria-controls='field{index}']"));
                Assert.NotNull(document.QuerySelector($"button.less[aria-controls='field{index}']"));
            }
        }

        /// <summary>
        /// Checks that the robots file is available to crawlers.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task Get_RobotsTxtReturnsSuccess()
        {
            // Act
            var response = await this.client.GetAsync("/robots.txt");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("text/plain", response.Content.Headers.ContentType.MediaType);
        }

        /// <summary>
        /// Checks that tenant endpoint selection is composed by the application service provider.
        /// </summary>
        [Fact]
        public void Services_UseTenantEndpointSelector()
        {
            EndpointSelector firstSelector = this.factory.Services.GetRequiredService<EndpointSelector>();
            EndpointSelector secondSelector = this.factory.Services.GetRequiredService<EndpointSelector>();

            Assert.IsType<DoarTenantEndpointSelector>(firstSelector);
            Assert.Same(firstSelector, secondSelector);
        }

        /// <summary>
        /// Checks that the classic Application Insights SDK owns the telemetry pipeline.
        /// </summary>
        [Fact]
        public void Services_UseClassicApplicationInsights()
        {
            TelemetryConfiguration configuration = this.factory.Services.GetRequiredService<TelemetryConfiguration>();

            Assert.Equal(2, typeof(TelemetryClient).Assembly.GetName().Version.Major);
            Assert.Contains(configuration.TelemetryInitializers, initializer => initializer is Ignore404ErrorsTelemetryInitializer);
            Assert.Contains(configuration.TelemetryInitializers, initializer => initializer is HttpTelemetryInitializer);
            Assert.Contains(configuration.TelemetryInitializers, initializer => initializer is DonationFlowTelemetryInitializer);
            Assert.Contains(configuration.DefaultTelemetrySink.TelemetryProcessors, processor => processor is RemoveAzureStorageTelemetryFilter);
            Assert.Contains(configuration.DefaultTelemetrySink.TelemetryProcessors, processor => processor is FileNotFoundAzureStroageBlobFilter);
            Assert.Contains(configuration.DefaultTelemetrySink.TelemetryProcessors, processor => processor is WebApplicationStatusFilter);
        }

        /// <summary>
        /// Checks that only 404 responses override the request's success state.
        /// </summary>
        /// <param name="responseCode">The HTTP response code.</param>
        /// <param name="success">The expected request success state.</param>
        [Theory]
        [InlineData("404", true)]
        [InlineData("500", false)]
        [InlineData("200", false)]
        public void Telemetry_Only404OverridesRequestSuccess(string responseCode, bool success)
        {
            RequestTelemetry telemetry = new RequestTelemetry
            {
                ResponseCode = responseCode,
                Success = false,
            };

            new Ignore404ErrorsTelemetryInitializer().Initialize(telemetry);

            Assert.Equal(success, telemetry.Success);
        }

        /// <summary>
        /// Checks that slot-swap warmup is handled before tenant routing.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Get_SlotSwapWarmupReturnsSuccess()
        {
            var response = await this.client.GetAsync("/health/swap");

            response.EnsureSuccessStatusCode();
            Assert.Equal("OK", await response.Content.ReadAsStringAsync());
        }

        /// <summary>
        /// Checks that known automated probe paths are rejected before routing.
        /// </summary>
        /// <param name="path">The probe path to request.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Theory]
        [InlineData("/index.php")]
        [InlineData("/.git/config")]
        [InlineData("/swagger/index.html")]
        public async Task Get_KnownProbePathsReturnsNotFound(string path)
        {
            // Act
            var response = await this.client.GetAsync(path);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
