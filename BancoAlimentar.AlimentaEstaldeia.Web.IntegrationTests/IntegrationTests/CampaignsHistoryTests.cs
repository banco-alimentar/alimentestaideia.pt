// -----------------------------------------------------------------------
// <copyright file="CampaignsHistoryTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using AngleSharp.Html.Dom;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Testing.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Integration tests for the referral campaigns history manage page.
    /// </summary>
    public class CampaignsHistoryTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string CampaignsHistoryPath = "/Identity/Account/Manage/CampaignsHistory";

        private readonly CustomWebApplicationFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="CampaignsHistoryTests"/> class.
        /// </summary>
        /// <param name="factory">Web application factory.</param>
        public CampaignsHistoryTests(CustomWebApplicationFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// Anonymous users are redirected to login.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Get_RedirectsToLogin_WhenNotAuthenticated()
        {
            var client = this.factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });

            var response = await client.GetAsync(CampaignsHistoryPath);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Identity/Account/Login", response.Headers.Location?.ToString());
        }

        /// <summary>
        /// Authenticated users without campaigns see the empty state and create form.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Get_ShowsEmptyState_WhenUserHasNoCampaigns()
        {
            var email = $"campaigns-empty-{Guid.NewGuid():N}@integration.test";
            using (var scope = this.factory.Services.CreateScope())
            {
                await IntegrationTestDataSeeder.EnsureUserAsync(
                    scope.ServiceProvider,
                    email,
                    IntegrationTestCredentials.DefaultPassword);
            }

            var client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                this.factory,
                email,
                IntegrationTestCredentials.DefaultPassword);

            var response = await client.GetAsync(CampaignsHistoryPath);

            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("id=\"create_section\"", html);
            Assert.Contains("Available Campaigns", html);
            Assert.Contains("No campaigns found.", html);
        }

        /// <summary>
        /// Authenticated owners see their campaigns listed on the page.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Get_ShowsOwnedCampaigns_WhenAuthenticated()
        {
            IntegrationTestDataSeeder.ReferralSeed referralSeed;
            using (var scope = this.factory.Services.CreateScope())
            {
                referralSeed = await IntegrationTestDataSeeder.SeedActiveReferralAsync(scope.ServiceProvider);
            }

            var client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                this.factory,
                referralSeed.OwnerEmail,
                IntegrationTestCredentials.DefaultPassword);

            var response = await client.GetAsync(CampaignsHistoryPath);

            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains(referralSeed.Code, html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("No campaigns found.", html);
        }

        /// <summary>
        /// Authenticated users can create a new referral campaign from the history page.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Post_CreatesCampaign_WhenCodeIsAvailable()
        {
            var email = $"campaigns-create-{Guid.NewGuid():N}@integration.test";
            var campaignCode = $"new{Guid.NewGuid():N}".Substring(0, 12);
            using (var scope = this.factory.Services.CreateScope())
            {
                await IntegrationTestDataSeeder.EnsureUserAsync(
                    scope.ServiceProvider,
                    email,
                    IntegrationTestCredentials.DefaultPassword);
            }

            var client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                this.factory,
                email,
                IntegrationTestCredentials.DefaultPassword);

            var getResponse = await client.GetAsync(CampaignsHistoryPath);
            getResponse.EnsureSuccessStatusCode();
            var content = await HtmlHelpers.GetDocumentAsync(getResponse);

            var createForm = content.QuerySelector("form#create_section");
            var submitButton = content.QuerySelector("form#create_section button[type='submit']");
            var codeInput = content.QuerySelector("form#create_section input[name='code']") as IHtmlInputElement;
            Assert.NotNull(createForm);
            Assert.NotNull(submitButton);
            Assert.NotNull(codeInput);
            codeInput.Value = campaignCode;

            var postResponse = await client.SendAsync(
                (IHtmlFormElement)createForm,
                (IHtmlButtonElement)submitButton);

            postResponse.EnsureSuccessStatusCode();
            var html = await postResponse.Content.ReadAsStringAsync();
            Assert.Contains(campaignCode, html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("No campaigns found.", html);

            using var assertScope = this.factory.Services.CreateScope();
            var context = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await context.Users.FirstAsync(u => u.Email == email);
            var referral = await context.Referrals.AsNoTracking()
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Code == campaignCode);
            Assert.NotNull(referral);
            Assert.Equal(user.Id, referral.User.Id);
            Assert.True(referral.Active);
            Assert.True(referral.IsPublic);
        }

        /// <summary>
        /// Posting an already active campaign code shows the duplicate campaign message.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Post_ShowsAlreadyExists_WhenCodeIsActive()
        {
            var duplicateCode = $"dup{Guid.NewGuid():N}".Substring(0, 12);
            IntegrationTestDataSeeder.ReferralSeed referralSeed;
            using (var scope = this.factory.Services.CreateScope())
            {
                referralSeed = await IntegrationTestDataSeeder.SeedActiveReferralAsync(
                    scope.ServiceProvider,
                    code: duplicateCode);
            }

            var otherUserEmail = $"campaigns-dup-{Guid.NewGuid():N}@integration.test";
            using (var scope = this.factory.Services.CreateScope())
            {
                await IntegrationTestDataSeeder.EnsureUserAsync(
                    scope.ServiceProvider,
                    otherUserEmail,
                    IntegrationTestCredentials.DefaultPassword);
            }

            var client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                this.factory,
                otherUserEmail,
                IntegrationTestCredentials.DefaultPassword);

            var getResponse = await client.GetAsync(CampaignsHistoryPath);
            getResponse.EnsureSuccessStatusCode();
            var content = await HtmlHelpers.GetDocumentAsync(getResponse);

            var createForm = content.QuerySelector("form#create_section");
            var submitButton = content.QuerySelector("form#create_section button[type='submit']");
            var codeInput = content.QuerySelector("form#create_section input[name='code']") as IHtmlInputElement;
            Assert.NotNull(createForm);
            Assert.NotNull(submitButton);
            Assert.NotNull(codeInput);
            codeInput.Value = referralSeed.Code;

            var postResponse = await client.SendAsync(
                (IHtmlFormElement)createForm,
                (IHtmlButtonElement)submitButton);

            postResponse.EnsureSuccessStatusCode();
            var html = await postResponse.Content.ReadAsStringAsync();
            Assert.Contains("The campaign with the specified code exists already", html);
        }
    }
}
