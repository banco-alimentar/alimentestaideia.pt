// -----------------------------------------------------------------------
// <copyright file="IdentityAuthTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests.Account
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using AngleSharp.Html.Dom;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Testing.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.AspNetCore.WebUtilities;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Integration tests for Identity login, registration, and password reset flows.
    /// </summary>
    public class IdentityAuthTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string NewPasswordAfterReset = "IntegrationResetOnly2!";
        private readonly CustomWebApplicationFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="IdentityAuthTests"/> class.
        /// </summary>
        /// <param name="factory">Web application factory.</param>
        public IdentityAuthTests(CustomWebApplicationFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// Wrong password keeps the user on the login page with a generic error.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Login_KeepsForm_WhenPasswordIsWrong()
        {
            var email = $"login-wrong-{Guid.NewGuid():N}@integration.test";
            var webFactory = this.CreateAuthFactory();
            using (var scope = webFactory.Services.CreateScope())
            {
                await IntegrationTestDataSeeder.EnsureUserAsync(
                    scope.ServiceProvider,
                    email,
                    IntegrationTestCredentials.DefaultPassword);
            }

            var client = webFactory.CreateClient();
            var loginPage = await client.GetAsync("/Identity/Account/Login");
            loginPage.EnsureSuccessStatusCode();
            var content = await HtmlHelpers.GetDocumentAsync(loginPage);

            var response = await client.SendAsync(
                (IHtmlFormElement)content.QuerySelector("form[id='account']"),
                (IHtmlButtonElement)content.QuerySelector("button[id='loginBtn']"),
                new Dictionary<string, string>
                {
                    ["Input.Email"] = email,
                    ["Input.Password"] = "Definitely-Wrong-Password1!",
                });

            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("Invalid Login Attempt", html);
            Assert.Contains("form id=\"account\"", html, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Unconfirmed accounts cannot sign in and see the same generic login error.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Login_KeepsForm_WhenEmailIsNotConfirmed()
        {
            var email = $"login-unconfirmed-{Guid.NewGuid():N}@integration.test";
            var webFactory = this.CreateAuthFactory();
            using (var scope = webFactory.Services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<WebUser>>();
                await userManager.CreateAsync(
                    new WebUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = false,
                        FullName = "Unconfirmed User",
                    },
                    IntegrationTestCredentials.DefaultPassword);
            }

            var client = webFactory.CreateClient();
            var loginPage = await client.GetAsync("/Identity/Account/Login");
            loginPage.EnsureSuccessStatusCode();
            var content = await HtmlHelpers.GetDocumentAsync(loginPage);

            var response = await client.SendAsync(
                (IHtmlFormElement)content.QuerySelector("form[id='account']"),
                (IHtmlButtonElement)content.QuerySelector("button[id='loginBtn']"),
                new Dictionary<string, string>
                {
                    ["Input.Email"] = email,
                    ["Input.Password"] = IntegrationTestCredentials.DefaultPassword,
                });

            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("Invalid Login Attempt", html);
            Assert.Contains("form id=\"account\"", html, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Confirmed user requesting password reset is redirected and receives an email.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task ForgotPassword_RedirectsAndSendsEmail_WhenUserIsConfirmed()
        {
            var email = $"forgot-confirmed-{Guid.NewGuid():N}@integration.test";
            var webFactory = this.CreateAuthFactory();
            using (var scope = webFactory.Services.CreateScope())
            {
                await IntegrationTestDataSeeder.EnsureUserAsync(
                    scope.ServiceProvider,
                    email,
                    IntegrationTestCredentials.DefaultPassword);
            }

            var client = webFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
            var forgotPage = await client.GetAsync("/Identity/Account/ForgotPassword");
            forgotPage.EnsureSuccessStatusCode();
            var content = await HtmlHelpers.GetDocumentAsync(forgotPage);

            var response = await client.SendAsync(
                (IHtmlFormElement)content.QuerySelector("form[method='post']"),
                new Dictionary<string, string>
                {
                    ["Input.Email"] = email,
                });

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/ForgotPasswordConfirmation", response.Headers.Location?.ToString());

            var tracker = webFactory.Services.GetRequiredService<StubMailTracker>();
            Assert.Equal(1, tracker.SendMailCalls);
        }

        /// <summary>
        /// Unknown email still redirects to confirmation without sending mail (no enumeration).
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task ForgotPassword_RedirectsWithoutEmail_WhenUserDoesNotExist()
        {
            var webFactory = this.CreateAuthFactory();
            var client = webFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
            var forgotPage = await client.GetAsync("/Identity/Account/ForgotPassword");
            forgotPage.EnsureSuccessStatusCode();
            var content = await HtmlHelpers.GetDocumentAsync(forgotPage);

            var response = await client.SendAsync(
                (IHtmlFormElement)content.QuerySelector("form[method='post']"),
                new Dictionary<string, string>
                {
                    ["Input.Email"] = $"missing-{Guid.NewGuid():N}@integration.test",
                });

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/ForgotPasswordConfirmation", response.Headers.Location?.ToString());

            var tracker = webFactory.Services.GetRequiredService<StubMailTracker>();
            Assert.Equal(0, tracker.SendMailCalls);
        }

        /// <summary>
        /// Unconfirmed user is redirected to the confirmation page without sending reset email.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task ForgotPassword_RedirectsWithNote_WhenEmailIsNotConfirmed()
        {
            var email = $"forgot-unconfirmed-{Guid.NewGuid():N}@integration.test";
            var webFactory = this.CreateAuthFactory();
            using (var scope = webFactory.Services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<WebUser>>();
                await userManager.CreateAsync(
                    new WebUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = false,
                        FullName = "Unconfirmed User",
                    },
                    IntegrationTestCredentials.DefaultPassword);
            }

            var client = webFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
            var forgotPage = await client.GetAsync("/Identity/Account/ForgotPassword");
            forgotPage.EnsureSuccessStatusCode();
            var content = await HtmlHelpers.GetDocumentAsync(forgotPage);

            var response = await client.SendAsync(
                (IHtmlFormElement)content.QuerySelector("form[method='post']"),
                new Dictionary<string, string>
                {
                    ["Input.Email"] = email,
                });

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("WithEmailNotConfirmedNote", response.Headers.Location?.ToString());

            var tracker = webFactory.Services.GetRequiredService<StubMailTracker>();
            Assert.Equal(0, tracker.SendMailCalls);
        }

        /// <summary>
        /// Valid reset token updates the password and allows sign-in with the new password.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task ResetPassword_UpdatesPasswordAndAllowsLogin()
        {
            var email = $"reset-flow-{Guid.NewGuid():N}@integration.test";
            var webFactory = this.CreateAuthFactory();
            string encodedCode;
            using (var scope = webFactory.Services.CreateScope())
            {
                var user = await IntegrationTestDataSeeder.EnsureUserAsync(
                    scope.ServiceProvider,
                    email,
                    IntegrationTestCredentials.DefaultPassword);
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<WebUser>>();
                var code = await userManager.GeneratePasswordResetTokenAsync(user);
                encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            }

            var client = webFactory.CreateClient();
            var resetPage = await client.GetAsync($"/Identity/Account/ResetPassword?code={encodedCode}");
            resetPage.EnsureSuccessStatusCode();
            var content = await HtmlHelpers.GetDocumentAsync(resetPage);

            var resetResponse = await client.SendAsync(
                (IHtmlFormElement)content.QuerySelector("form[method='post']"),
                new Dictionary<string, string>
                {
                    ["Input.Email"] = email,
                    ["Input.Password"] = NewPasswordAfterReset,
                    ["Input.ConfirmPassword"] = NewPasswordAfterReset,
                });

            Assert.Equal("/Identity/Account/ResetPasswordConfirmation", resetResponse.RequestMessage.RequestUri.AbsolutePath);

            var loginPage = await client.GetAsync("/Identity/Account/Login");
            var loginContent = await HtmlHelpers.GetDocumentAsync(loginPage);
            var loginResponse = await client.SendAsync(
                (IHtmlFormElement)loginContent.QuerySelector("form[id='account']"),
                (IHtmlButtonElement)loginContent.QuerySelector("button[id='loginBtn']"),
                new Dictionary<string, string>
                {
                    ["Input.Email"] = email,
                    ["Input.Password"] = NewPasswordAfterReset,
                });

            loginResponse.EnsureSuccessStatusCode();
            Assert.Equal(webFactory.CreateClient().BaseAddress, loginResponse.RequestMessage.RequestUri);
        }

        /// <summary>
        /// Weak password on register keeps the form visible with validation errors.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Register_KeepsForm_WhenPasswordIsTooWeak()
        {
            var email = $"register-weak-{Guid.NewGuid():N}@integration.test";
            var client = this.CreateAuthFactory().CreateClient();
            var registerPage = await client.GetAsync("/Identity/Account/Register");
            registerPage.EnsureSuccessStatusCode();
            var content = await HtmlHelpers.GetDocumentAsync(registerPage);

            var response = await client.SendAsync(
                (IHtmlFormElement)content.QuerySelector("form[id='registerForm']"),
                (IHtmlButtonElement)content.QuerySelector("button[id='registerBtn']"),
                IdentityAuthTestHelper.BuildValidRegisterForm(email, password: "weak"));

            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("registerForm", html);
            Assert.Contains("validation-summary", html, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Duplicate email on register keeps the form visible with identity errors.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Register_KeepsForm_WhenEmailIsDuplicate()
        {
            var email = $"register-dup-{Guid.NewGuid():N}@integration.test";
            var client = this.CreateAuthFactory().CreateClient();

            await this.RegisterUserAsync(client, email);

            var registerPage = await client.GetAsync("/Identity/Account/Register");
            var content = await HtmlHelpers.GetDocumentAsync(registerPage);
            var response = await client.SendAsync(
                (IHtmlFormElement)content.QuerySelector("form[id='registerForm']"),
                (IHtmlButtonElement)content.QuerySelector("button[id='registerBtn']"),
                IdentityAuthTestHelper.BuildValidRegisterForm(email));

            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("registerForm", html);
            Assert.Contains("already taken", html, StringComparison.OrdinalIgnoreCase);
        }

        private WebApplicationFactory<Program> CreateAuthFactory()
        {
            return this.factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    IntegrationTestMailConfiguration.AddTrackedStubMail(services);
                });
            });
        }

        private async Task RegisterUserAsync(HttpClient client, string email)
        {
            var registerPage = await client.GetAsync("/Identity/Account/Register");
            registerPage.EnsureSuccessStatusCode();
            var content = await HtmlHelpers.GetDocumentAsync(registerPage);
            var response = await client.SendAsync(
                (IHtmlFormElement)content.QuerySelector("form[id='registerForm']"),
                (IHtmlButtonElement)content.QuerySelector("button[id='registerBtn']"),
                IdentityAuthTestHelper.BuildValidRegisterForm(email));

            Assert.Equal("/Identity/Account/RegisterConfirmation", response.RequestMessage.RequestUri.AbsolutePath);
        }
    }
}
