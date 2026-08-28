// -----------------------------------------------------------------------
// <copyright file="AdminEmailTestTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;
    using AngleSharp.Html.Dom;
    using BancoAlimentar.AlimentaEstaIdeia.Testing.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Integration tests for the admin email diagnostics page.
    /// </summary>
    public class AdminEmailTestTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string AdminEmail = "integration-email-admin@test.com";
        private const string AdminPassword = IntegrationTestCredentials.DefaultPassword;
        private readonly CustomWebApplicationFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminEmailTestTests"/> class.
        /// </summary>
        /// <param name="factory">Web application factory.</param>
        public AdminEmailTestTests(CustomWebApplicationFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// Anonymous users cannot open the email diagnostics page.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Get_RedirectsToLogin_WhenNotAuthenticated()
        {
            var client = this.factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });

            var response = await client.GetAsync("/Admin/EmailTest");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }

        /// <summary>
        /// An administrator can view safe email settings and send a test email.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Post_SendsTestEmail_WhenEmailIsEnabled()
        {
            var webFactory = this.factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["IsEmailEnabled"] = "true",
                        ["EmailFrom"] = "sender@integration.test",
                        ["Smtp:Host"] = "smtp.integration.test",
                        ["Smtp:Port"] = "25",
                        ["Smtp:UseCredentials"] = "false",
                        ["Smtp:EnableSsl"] = "false",
                    });
                });
                builder.ConfigureServices(services =>
                {
                    IntegrationTestMailConfiguration.AddTrackedStubMail(services);
                });
            });

            using (var scope = webFactory.Services.CreateScope())
            {
                await IntegrationTestDataSeeder.EnsureAdminUserAsync(
                    scope.ServiceProvider,
                    AdminEmail,
                    AdminPassword);
            }

            var client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                webFactory,
                AdminEmail,
                AdminPassword);
            var getResponse = await client.GetAsync("/Admin/EmailTest");
            getResponse.EnsureSuccessStatusCode();
            var document = await HtmlHelpers.GetDocumentAsync(getResponse);
            var form = (IHtmlFormElement)document.QuerySelector("form[action*='handler=Send']");

            Assert.Contains("Email sending is enabled", await getResponse.Content.ReadAsStringAsync());

            var postResponse = await client.SendAsync(
                form,
                new Dictionary<string, string>
                {
                    ["TestEmailAddress"] = "email-test-recipient@integration.test",
                });

            postResponse.EnsureSuccessStatusCode();
            var html = await postResponse.Content.ReadAsStringAsync();
            Assert.Contains("Test email sent to email-test-recipient@integration.test", html);

            var tracker = webFactory.Services.GetRequiredService<StubMailTracker>();
            Assert.Equal(1, tracker.SendMailCalls);
            Assert.Equal("email-test-recipient@integration.test", tracker.LastRecipient);
            Assert.Contains("test email", tracker.LastSubject, System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// An administrator sees the mail service error details when sending fails.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Post_ShowsSendErrorDetails_WhenEmailSendFails()
        {
            var webFactory = this.factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["IsEmailEnabled"] = "true",
                        ["EmailFrom"] = "sender@integration.test",
                        ["Smtp:Host"] = "smtp.integration.test",
                        ["Smtp:Port"] = "25",
                        ["Smtp:UseCredentials"] = "false",
                        ["Smtp:EnableSsl"] = "false",
                    });
                });
                builder.ConfigureServices(services =>
                {
                    IntegrationTestMailConfiguration.AddTrackedStubMail(services);
                });
            });

            using (var scope = webFactory.Services.CreateScope())
            {
                await IntegrationTestDataSeeder.EnsureAdminUserAsync(
                    scope.ServiceProvider,
                    AdminEmail,
                    AdminPassword);
            }

            var tracker = webFactory.Services.GetRequiredService<StubMailTracker>();
            tracker.SendMailSucceeds = false;
            tracker.SendMailError = "SmtpException: Connection refused";

            var client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                webFactory,
                AdminEmail,
                AdminPassword);
            var getResponse = await client.GetAsync("/Admin/EmailTest");
            getResponse.EnsureSuccessStatusCode();
            var document = await HtmlHelpers.GetDocumentAsync(getResponse);
            var form = (IHtmlFormElement)document.QuerySelector("form[action*='handler=Send']");

            var postResponse = await client.SendAsync(
                form,
                new Dictionary<string, string>
                {
                    ["TestEmailAddress"] = "email-test-recipient@integration.test",
                });

            postResponse.EnsureSuccessStatusCode();
            var html = await postResponse.Content.ReadAsStringAsync();
            Assert.Contains("The test email could not be sent", html);
            Assert.Contains("SmtpException: Connection refused", html);
        }
    }
}
