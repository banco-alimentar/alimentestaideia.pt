// -----------------------------------------------------------------------
// <copyright file="AdminFunctionExecutionReportsRunNowTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using AngleSharp.Html.Dom;
    using BancoAlimentar.AlimentaEstaIdeia.Testing.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services.FunctionExecutionReports;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Xunit;

    /// <summary>
    /// End-to-end tests for the secure Admin Run Now action.
    /// </summary>
    public sealed class AdminFunctionExecutionReportsRunNowTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string AdminEmail = "function-run-now-admin@test.com";
        private const string UserEmail = "function-run-now-user@test.com";
        private const string Password = IntegrationTestCredentials.DefaultPassword;

        private readonly CustomWebApplicationFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminFunctionExecutionReportsRunNowTests"/> class.
        /// </summary>
        /// <param name="factory">Web application factory.</param>
        public AdminFunctionExecutionReportsRunNowTests(CustomWebApplicationFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// An anonymous caller cannot invoke a function through the Admin action.
        /// </summary>
        [Fact]
        public async Task RunNow_RedirectsToLogin_WhenAnonymous()
        {
            HttpClient client = this.factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });

            HttpResponseMessage response = await client.PostAsync(
                "/Admin/FunctionExecutionReports?handler=Run",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["functionKey"] = "GenerateDonationReportFunction",
                }));

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }

        /// <summary>
        /// An authenticated non-admin caller cannot invoke the Admin action.
        /// </summary>
        [Fact]
        public async Task RunNow_ReturnsForbidden_WhenCallerIsNotAdmin()
        {
            using (var scope = this.factory.Services.CreateScope())
            {
                await IntegrationTestDataSeeder.EnsureUserAsync(scope.ServiceProvider, UserEmail, Password);
            }

            HttpClient client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                this.factory,
                UserEmail,
                Password,
                allowAutoRedirect: false);
            HttpResponseMessage response = await client.PostAsync(
                "/Admin/FunctionExecutionReports?handler=Run",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["functionKey"] = "GenerateDonationReportFunction",
                }));

            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        }

        /// <summary>
        /// The overview exposes one CSRF-protected Run Now form for every catalog function.
        /// </summary>
        [Fact]
        public async Task Overview_RendersRunNowFormForEveryCatalogFunction()
        {
            using (var scope = this.factory.Services.CreateScope())
            {
                await IntegrationTestDataSeeder.EnsureSuperAdminUserAsync(scope.ServiceProvider, AdminEmail, Password);
            }

            HttpClient client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(this.factory, AdminEmail, Password);
            var document = await HtmlHelpers.GetDocumentAsync(
                await client.GetAsync("/Admin/FunctionExecutionReports"));

            string html = document.DocumentElement.OuterHtml;
            Assert.Equal(5, document.QuerySelectorAll("form[action*='handler=Run']").Length);
            int requestVerificationTokenCount = document.QuerySelectorAll(
                "form[action*='handler=Run'] input[name='__RequestVerificationToken']").Length;
            Assert.Equal(5, requestVerificationTokenCount);
            Assert.Contains("GenerateDonationReportFunction", html, StringComparison.Ordinal);
            Assert.Contains("GenerateSiteHealthReportFunction", html, StringComparison.Ordinal);
            Assert.Contains("DeleteOldSubscriptionFunction", html, StringComparison.Ordinal);
            Assert.Contains("MultiBancoPaymentNotificationFunction", html, StringComparison.Ordinal);
            Assert.Contains("UpdateSubscriptions", html, StringComparison.Ordinal);
        }

        /// <summary>
        /// A valid Admin POST dispatches only the selected catalog function and shows success feedback.
        /// </summary>
        [Fact]
        public async Task RunNow_DispatchesSelectedCatalogFunctionAndShowsSuccess()
        {
            var trigger = new RecordingFunctionExecutionTriggerClient
            {
                Result = FunctionExecutionTriggerResult.Succeeded("Run accepted."),
            };
            var webFactory = this.CreateFactory(trigger);
            await this.EnsureSuperAdminAsync(webFactory);

            HttpClient client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(webFactory, AdminEmail, Password);
            var document = await HtmlHelpers.GetDocumentAsync(
                await client.GetAsync("/Admin/FunctionExecutionReports"));
            IHtmlFormElement form = this.GetRunForm(document, "UpdateSubscriptions");

            HttpResponseMessage response = await client.SendAsync(
                form,
                new Dictionary<string, string>
                {
                    ["functionKey"] = "UpdateSubscriptions",
                });
            string html = await response.Content.ReadAsStringAsync();

            response.EnsureSuccessStatusCode();
            Assert.Contains("Run accepted.", html, StringComparison.Ordinal);
            Assert.Equal(new[] { "UpdateSubscriptions" }, trigger.FunctionKeys);
        }

        /// <summary>
        /// A valid Admin POST renders failure feedback when the trigger client reports a failure.
        /// </summary>
        [Fact]
        public async Task RunNow_ShowsFailureFeedback_WhenTriggerFails()
        {
            var trigger = new RecordingFunctionExecutionTriggerClient
            {
                Result = FunctionExecutionTriggerResult.Failed("Function host unavailable."),
            };
            var webFactory = this.CreateFactory(trigger);
            await this.EnsureSuperAdminAsync(webFactory);

            HttpClient client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(webFactory, AdminEmail, Password);
            var document = await HtmlHelpers.GetDocumentAsync(
                await client.GetAsync("/Admin/FunctionExecutionReports"));
            IHtmlFormElement form = this.GetRunForm(document, "GenerateSiteHealthReportFunction");

            HttpResponseMessage response = await client.SendAsync(
                form,
                new Dictionary<string, string>
                {
                    ["functionKey"] = "GenerateSiteHealthReportFunction",
                });
            string html = await response.Content.ReadAsStringAsync();

            response.EnsureSuccessStatusCode();
            Assert.Contains("Function host unavailable.", html, StringComparison.Ordinal);
            Assert.Equal(new[] { "GenerateSiteHealthReportFunction" }, trigger.FunctionKeys);
        }

        /// <summary>
        /// A catalog key that is not present in the server catalog is rejected without dispatch.
        /// </summary>
        [Fact]
        public async Task RunNow_RejectsInvalidFunctionKeyWithoutDispatch()
        {
            var trigger = new RecordingFunctionExecutionTriggerClient();
            var webFactory = this.CreateFactory(trigger);
            await this.EnsureSuperAdminAsync(webFactory);

            HttpClient client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                webFactory,
                AdminEmail,
                Password,
                allowAutoRedirect: false);
            var document = await HtmlHelpers.GetDocumentAsync(
                await client.GetAsync("/Admin/FunctionExecutionReports"));
            IHtmlFormElement form = this.GetRunForm(document, "GenerateDonationReportFunction");

            HttpResponseMessage response = await client.SendAsync(
                form,
                new Dictionary<string, string>
                {
                    ["functionKey"] = "DeleteAllDataAndRun",
                });

            Assert.True(
                response.StatusCode == HttpStatusCode.BadRequest
                    || response.StatusCode == HttpStatusCode.NotFound
                    || response.StatusCode == HttpStatusCode.OK,
                $"Unexpected status: {response.StatusCode}");
            Assert.Empty(trigger.FunctionKeys);
        }

        /// <summary>
        /// A POST without the anti-forgery token is rejected before the trigger client is called.
        /// </summary>
        [Fact]
        public async Task RunNow_RejectsMissingCsrfTokenWithoutDispatch()
        {
            var trigger = new RecordingFunctionExecutionTriggerClient();
            var webFactory = this.CreateFactory(trigger);
            await this.EnsureSuperAdminAsync(webFactory);

            HttpClient client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                webFactory,
                AdminEmail,
                Password,
                allowAutoRedirect: false);
            HttpResponseMessage response = await client.PostAsync(
                "/Admin/FunctionExecutionReports?handler=Run",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["functionKey"] = "UpdateSubscriptions",
                }));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Empty(trigger.FunctionKeys);
        }

        private IHtmlFormElement GetRunForm(AngleSharp.Dom.IDocument document, string functionKey)
        {
            foreach (IHtmlFormElement form in document.QuerySelectorAll("form[action*='handler=Run']"))
            {
                if (form.QuerySelector($"input[name='functionKey'][value='{functionKey}']") != null)
                {
                    return form;
                }
            }

            throw new InvalidOperationException($"Run Now form was not found for {functionKey}.");
        }

        private WebApplicationFactory<Program> CreateFactory(RecordingFunctionExecutionTriggerClient trigger)
        {
            return this.factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                services.RemoveAll<IFunctionExecutionTriggerClient>();
                services.AddSingleton<IFunctionExecutionTriggerClient>(trigger);
            }));
        }

        private async Task EnsureSuperAdminAsync(WebApplicationFactory<Program> webFactory)
        {
            using var scope = webFactory.Services.CreateScope();
            await IntegrationTestDataSeeder.EnsureSuperAdminUserAsync(scope.ServiceProvider, AdminEmail, Password);
        }

        private sealed class RecordingFunctionExecutionTriggerClient : IFunctionExecutionTriggerClient
        {
            private readonly List<string> functionKeys = new List<string>();

            /// <summary>Gets or sets the result returned by the fake trigger client.</summary>
            public FunctionExecutionTriggerResult Result { get; set; } = FunctionExecutionTriggerResult.Succeeded("Run accepted.");

            /// <summary>Gets the function keys dispatched by the Admin request.</summary>
            public IReadOnlyList<string> FunctionKeys => this.functionKeys;

            /// <inheritdoc />
            public Task<FunctionExecutionTriggerResult> TriggerAsync(
                string functionKey,
                CancellationToken cancellationToken = default)
            {
                this.functionKeys.Add(functionKey);
                return Task.FromResult(this.Result);
            }
        }
    }
}
