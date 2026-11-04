// -----------------------------------------------------------------------
// <copyright file="BrowserTestApplicationTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using AngleSharp.Html.Dom;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Testing.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;
    using Xunit;

    /// <summary>
    /// Exercises the isolated browser application's public contract and real dependencies.
    /// </summary>
    [Trait("Category", "Offline")]
    public class BrowserTestApplicationTests
    {
        /// <summary>
        /// The browser host must exist without relying on an external application process.
        /// </summary>
        [Fact]
        public void BrowserHost_ExposesAsyncDisposableApplication()
        {
            Type applicationType = typeof(CustomWebApplicationFactory).Assembly.GetType(
                "BancoAlimentar.AlimentaEstaIdeia.Web.TestHost.BrowserTestApplication");

            Assert.NotNull(applicationType);
            Assert.True(typeof(IAsyncDisposable).IsAssignableFrom(applicationType));
        }

        /// <summary>
        /// A real Kestrel request renders forgot password with the owned relational database.
        /// </summary>
        /// <returns>A task representing the operation.</returns>
        [Fact]
        public async Task StartAsync_ForgotPassword_IsReachableOverLoopback()
        {
            await using var application = await BrowserTestApplication.StartAsync();
            using var handler = new HttpClientHandler { AllowAutoRedirect = false, UseProxy = false };
            using var client = new HttpClient(handler) { BaseAddress = application.BaseAddress };

            Assert.Equal(Uri.UriSchemeHttp, application.BaseAddress.Scheme);
            Assert.True(application.BaseAddress.IsLoopback);
            Assert.True(application.BaseAddress.Port > 0);

            using var response = await client.GetAsync("/Identity/Account/ForgotPassword");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("name=\"Input.Email\"", await response.Content.ReadAsStringAsync());
            using var scope = application.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            application.AssertOwnsDatabase(context);
            Assert.Equal("Microsoft.EntityFrameworkCore.Sqlite", context.Database.ProviderName);
            Assert.True(context.Database.IsRelational());
            Assert.True(await context.Users.AnyAsync());
        }

        /// <summary>
        /// Local configuration resolves the real mail service and independent relational databases.
        /// </summary>
        /// <returns>A task representing the operation.</returns>
        [Fact]
        public async Task StartAsync_UsesDevelopmentCaptureAndSeparateSqliteDatabases()
        {
            await using var application = await BrowserTestApplication.StartAsync(smtpPort: 2526);
            using var scope = application.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var infrastructure = scope.ServiceProvider.GetRequiredService<InfrastructureDbContext>();

            Assert.True(scope.ServiceProvider.GetRequiredService<IHostEnvironment>().IsDevelopment());
            Assert.True(application.Configuration.GetValue<bool>("LocalEmailCapture:Enabled"));
            Assert.True(application.Configuration.GetValue<bool>("LocalEmailCapture:UseInMemoryInfrastructure"));
            Assert.True(application.Services.GetRequiredService<TelemetryConfiguration>().DisableTelemetry);
            Assert.False(application.Services.GetRequiredService<IOptions<ApplicationInsightsServiceOptions>>().Value.EnableQuickPulseMetricStream);
            Assert.True(application.Configuration.GetValue<bool>("OTEL_SDK_DISABLED"));
            Assert.Equal(2526, application.Configuration.GetValue<int>("LocalEmailCapture:SmtpPort"));
            Assert.IsType<Mail>(scope.ServiceProvider.GetRequiredService<IMail>());
            Assert.Equal("Microsoft.EntityFrameworkCore.Sqlite", context.Database.ProviderName);
            Assert.Equal("Microsoft.EntityFrameworkCore.Sqlite", infrastructure.Database.ProviderName);
            Assert.NotEqual(context.Database.GetConnectionString(), infrastructure.Database.GetConnectionString());
            Assert.True(await infrastructure.Tenants.AnyAsync());
            Assert.True(await context.FoodBanks.AnyAsync());
            Assert.True(await context.Campaigns.AnyAsync());
            Assert.True(await context.ProductCatalogues.AnyAsync());
        }

        /// <summary>
        /// Browser entry points and their local assets are served by Kestrel without redirects.
        /// </summary>
        /// <returns>A task representing the operation.</returns>
        [Fact]
        public async Task Get_PublicPagesAndStaticAssets_ReturnSuccess()
        {
            await using var application = await BrowserTestApplication.StartAsync();
            using var client = CreateClient(application);

            foreach (string path in new[] { "/", "/Identity/Account/Login", "/css/site.css", "/lib/jquery/jquery.min.js", "/Identity/lib/jquery-validation/dist/jquery.validate.js", "/robots.txt" })
            {
                using var response = await client.GetAsync(path);
                Assert.True(response.StatusCode == HttpStatusCode.OK, $"GET {path} returned {response.StatusCode}.");
                Assert.NotEmpty(await response.Content.ReadAsByteArrayAsync());
                Assert.Null(response.Headers.Location);
            }

            using var missing = await client.GetAsync("/Identity/lib/not-a-real-script.js");
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        }

        /// <summary>
        /// The browser host preserves the authentication guards on campaign and admin entry points.
        /// </summary>
        /// <param name="path">Protected page path.</param>
        /// <returns>A task representing the operation.</returns>
        [Theory]
        [InlineData("/Identity/Account/Manage/CampaignsHistory")]
        [InlineData("/Admin/Referrals")]
        public async Task Get_ProtectedPage_RequiresRealIdentityLogin(string path)
        {
            await using var application = await BrowserTestApplication.StartAsync();
            using var client = CreateClient(application);

            using var response = await client.GetAsync(path);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Identity/Account/Login", response.Headers.Location?.OriginalString);
        }

        /// <summary>
        /// Restricting authentication to the real local cookie preserves admin and super-admin role checks.
        /// </summary>
        /// <param name="account">Seeded account configuration section.</param>
        /// <param name="path">Protected page path.</param>
        /// <returns>A task representing the operation.</returns>
        [Theory]
        [InlineData("Site", "/Admin/Referrals")]
        [InlineData("Site", "/RoleManagement/Roles")]
        [InlineData("Admin", "/RoleManagement/Roles")]
        [InlineData("Admin", "/Admin/ReloadSettings")]
        public async Task Get_InsufficientRole_RedirectsAuthenticatedUserToAccessDenied(string account, string path)
        {
            await using var application = await BrowserTestApplication.StartAsync();
            string email = application.Configuration[$"SeleniumTest:Local:{account}:Username"];
            using (var scope = application.Services.CreateScope())
            {
                var manager = scope.ServiceProvider.GetRequiredService<UserManager<WebUser>>();
                var user = await manager.FindByEmailAsync(email);
                Assert.NotNull(user);
                Assert.False(await manager.IsInRoleAsync(user, "SuperAdmin"));
                Assert.Equal(account == "Admin", await manager.IsInRoleAsync(user, "Admin"));
            }

            using var client = CreateClient(application);
            await WebTestAuthHelper.LoginAsync(client, email, application.Configuration[$"SeleniumTest:Local:{account}:Password"]);
            using var authenticated = await client.GetAsync("/Identity/Account/Manage/CampaignsHistory");
            Assert.Equal(HttpStatusCode.OK, authenticated.StatusCode);

            using var response = await client.GetAsync(path);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
            var redirect = new Uri(application.BaseAddress, response.Headers.Location);
            Assert.Equal("/Identity/Account/AccessDenied", redirect.AbsolutePath);
            Assert.Contains(path, Uri.UnescapeDataString(redirect.Query));
        }

        /// <summary>
        /// A real super-admin cookie satisfies RoleArea on role management and restricted admin pages.
        /// </summary>
        /// <returns>A task representing the operation.</returns>
        [Fact]
        public async Task Get_SuperAdminRole_CanAccessRoleAreaPages()
        {
            await using var application = await BrowserTestApplication.StartAsync();
            string email = $"superadmin-{Guid.NewGuid():N}@example.test";
            using (var scope = application.Services.CreateScope())
            {
                application.AssertOwnsDatabase(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
                await IntegrationTestDataSeeder.EnsureSuperAdminUserAsync(scope.ServiceProvider, email, IntegrationTestCredentials.DefaultPassword);
            }

            using var client = CreateClient(application);
            await WebTestAuthHelper.LoginAsync(client, email, IntegrationTestCredentials.DefaultPassword);

            using var roles = await client.GetAsync("/RoleManagement/Roles");
            Assert.Equal(HttpStatusCode.OK, roles.StatusCode);
            Assert.Contains("SuperAdmin", await roles.Content.ReadAsStringAsync());
            using var settings = await client.GetAsync("/Admin/ReloadSettings");
            Assert.Equal(HttpStatusCode.OK, settings.StatusCode);
        }

        /// <summary>
        /// Real browser POST endpoints retain antiforgery protection.
        /// </summary>
        /// <returns>A task representing the operation.</returns>
        [Fact]
        public async Task Post_ForgotPasswordWithoutAntiforgery_IsRejected()
        {
            await using var application = await BrowserTestApplication.StartAsync();
            using var client = CreateClient(application);
            using var payload = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.Email"] = $"unknown-{Guid.NewGuid():N}@example.test",
            });

            using var response = await client.PostAsync("/Identity/Account/ForgotPassword", payload);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        /// <summary>
        /// SQLite enforces unique referral codes even when writes bypass application normalization.
        /// </summary>
        /// <param name="changeCase">Whether to attempt an uppercase duplicate.</param>
        /// <returns>A task representing the operation.</returns>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Insert_DuplicateReferralCode_IsRejectedByDatabase(bool changeCase)
        {
            await using var application = await BrowserTestApplication.StartAsync();
            string code = application.Configuration["SeleniumTest:Local:ReferralMetricsCode"];
            using var scope = application.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            string duplicateCode = changeCase ? code.ToUpperInvariant() : code;

            var exception = await Assert.ThrowsAnyAsync<DbException>(() => context.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO Referrals (Code, Active, IsPublic, CreateDate, LinkOpenCount) VALUES ({duplicateCode}, 1, 0, {DateTime.UtcNow}, 0)"));

            Assert.Contains("UNIQUE constraint failed", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(1, await context.Referrals.CountAsync(referral => referral.Code == code));
            var repository = scope.ServiceProvider.GetRequiredService<IUnitOfWork>().ReferralRepository;
            Assert.Equal(code, repository.GetByCode(code.ToUpperInvariant()).Code);
        }

        /// <summary>
        /// SQLite rejects opening events that reference nonexistent campaigns.
        /// </summary>
        /// <returns>A task representing the operation.</returns>
        [Fact]
        public async Task SaveChanges_OrphanReferralOpening_IsRejectedByForeignKey()
        {
            await using var application = await BrowserTestApplication.StartAsync();
            using var scope = application.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.ReferralLinkOpens.Add(new ReferralLinkOpen
            {
                ReferralId = int.MaxValue,
                OpenedAtUtc = DateTime.UtcNow,
            });

            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

            Assert.IsAssignableFrom<DbException>(exception.InnerException);
            Assert.Contains("FOREIGN KEY constraint failed", exception.InnerException.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Deleting a campaign directly in SQL cascades to its opening events.
        /// </summary>
        /// <returns>A task representing the operation.</returns>
        [Fact]
        public async Task Delete_Referral_CascadesOpeningEventsInDatabase()
        {
            await using var application = await BrowserTestApplication.StartAsync();
            int referralId;
            using (var scope = application.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var referral = new Referral
                {
                    Code = $"cascade{Guid.NewGuid():N}",
                    Active = true,
                    CreateDate = DateTime.UtcNow,
                    LinkOpens = new List<ReferralLinkOpen> { new ReferralLinkOpen { OpenedAtUtc = DateTime.UtcNow } },
                };
                context.Referrals.Add(referral);
                await context.SaveChangesAsync();
                referralId = referral.Id;
                Assert.Equal(1, await context.ReferralLinkOpens.CountAsync(open => open.ReferralId == referralId));
                Assert.Equal(1, await context.Referrals.Where(item => item.Id == referralId).ExecuteDeleteAsync());
            }

            using var verificationScope = application.Services.CreateScope();
            var verification = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.False(await verification.ReferralLinkOpens.AnyAsync(open => open.ReferralId == referralId));
        }

        /// <summary>
        /// Repository relationships and paid-only queries translate against real SQLite fixtures.
        /// </summary>
        /// <returns>A task representing the operation.</returns>
        [Fact]
        public async Task ReferralRepository_LoadsRelationshipsAndOnlyPaidMetrics()
        {
            await using var application = await BrowserTestApplication.StartAsync();
            using var scope = application.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            string code = application.Configuration["SeleniumTest:Local:ReferralMetricsCode"];
            int referralId = await context.Referrals.Where(referral => referral.Code == code).Select(referral => referral.Id).SingleAsync();
            var repository = scope.ServiceProvider.GetRequiredService<IUnitOfWork>().ReferralRepository;

            var referral = repository.GetFullReferral(referralId);
            var paid = repository.GetPaidDonationsByReferralCode(code.ToUpperInvariant());

            Assert.NotNull(referral.User);
            Assert.Equal(application.Configuration["SeleniumTest:Local:Site:Username"], referral.User.Email);
            Assert.Equal(3, referral.Donations.Count);
            Assert.All(referral.Donations, donation => Assert.NotNull(donation.User));
            Assert.Equal(2, paid.Count);
            Assert.All(paid, donation => Assert.Equal(PaymentStatus.Payed, donation.PaymentStatus));
            Assert.Equal(25d, paid.Sum(donation => donation.DonationAmount));
            var pending = Assert.Single(referral.Donations, donation => donation.PaymentStatus != PaymentStatus.Payed);
            Assert.Equal(99d, pending.DonationAmount);
            Assert.True(pending.DonationDate > paid.Max(donation => donation.DonationDate));
            Assert.Equal(12, referral.LinkOpenCount);
            Assert.Contains(repository.GetTopList(10, 30), item => item.Id == referral.Id);
            Assert.Equal(referral.Id, repository.GetFullReferral(referral.User.Id, referral.Id).Id);
            Assert.Null(repository.GetFullReferral("another-user", referral.Id));
        }

        /// <summary>
        /// HTTP requests and verification scopes share the same persistent relational store.
        /// </summary>
        /// <returns>A task representing the operation.</returns>
        [Fact]
        public async Task Get_Referral_PersistsOneOpeningAcrossScopes()
        {
            await using var application = await BrowserTestApplication.StartAsync();
            string code = application.Configuration["SeleniumTest:Local:ReferralMetricsCode"];
            int originalOpenings;
            int originalEvents;
            int referralId;
            using (var scope = application.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var referral = await context.Referrals.SingleAsync(item => item.Code == code);
                referralId = referral.Id;
                originalOpenings = referral.LinkOpenCount;
                originalEvents = await context.ReferralLinkOpens.CountAsync(open => open.ReferralId == referralId);
            }

            using var client = CreateClient(application);
            using var response = await client.GetAsync($"/Referral?text={code.ToUpperInvariant()}");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/Donation", response.Headers.Location?.OriginalString);
            using var verificationScope = application.Services.CreateScope();
            var verification = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(originalOpenings + 1, (await verification.Referrals.FindAsync(referralId)).LinkOpenCount);
            Assert.Equal(originalEvents + 1, await verification.ReferralLinkOpens.CountAsync(open => open.ReferralId == referralId));
            var repository = verificationScope.ServiceProvider.GetRequiredService<IUnitOfWork>().ReferralRepository;
            Assert.Equal(originalEvents + 1, repository.GetLinkOpensGroupedByDay(referralId).Sum(day => day.Count));
        }

        /// <summary>
        /// A real Identity user remains available to later scopes and can log in over HTTP.
        /// </summary>
        /// <returns>A task representing the operation.</returns>
        [Fact]
        public async Task Identity_CreateUserAcrossScopes_CanAuthenticateOverHttp()
        {
            await using var application = await BrowserTestApplication.StartAsync();
            string email = $"identity-{Guid.NewGuid():N}@example.test";
            string userId;
            using (var scope = application.Services.CreateScope())
            {
                var manager = scope.ServiceProvider.GetRequiredService<UserManager<WebUser>>();
                var user = new WebUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FullName = "Browser integration user",
                };
                var result = await manager.CreateAsync(user, IntegrationTestCredentials.DefaultPassword);
                Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(error => error.Code)));
                userId = user.Id;
            }

            using (var scope = application.Services.CreateScope())
            {
                var manager = scope.ServiceProvider.GetRequiredService<UserManager<WebUser>>();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                application.AssertOwnsDatabase(context);
                var user = await manager.FindByEmailAsync(email);
                Assert.NotNull(user);
                Assert.Equal(userId, user.Id);
                Assert.True(await manager.CheckPasswordAsync(user, IntegrationTestCredentials.DefaultPassword));
                Assert.Equal(userId, (await context.Users.SingleAsync(item => item.Email == email)).Id);
            }

            using var client = CreateClient(application);
            await WebTestAuthHelper.LoginAsync(client, email, IntegrationTestCredentials.DefaultPassword);
            using var response = await client.GetAsync("/Identity/Account/Manage/CampaignsHistory");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("create_section", await response.Content.ReadAsStringAsync());
        }

        /// <summary>
        /// Seeded administrators reach the real paid-only metrics page through Identity login.
        /// </summary>
        /// <returns>A task representing the operation.</returns>
        [Fact]
        public async Task Get_AdminReferralMetrics_ShowsPaidTotalsAndSeededImage()
        {
            await using var application = await BrowserTestApplication.StartAsync();
            using var client = CreateClient(application);
            await WebTestAuthHelper.LoginAsync(
                client,
                application.Configuration["SeleniumTest:Local:Admin:Username"],
                application.Configuration["SeleniumTest:Local:Admin:Password"]);

            using var response = await client.GetAsync("/Admin/Referrals");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var document = await HtmlHelpers.GetDocumentAsync(response);
            string code = application.Configuration["SeleniumTest:Local:ReferralMetricsCode"];
            var row = Assert.Single(document.QuerySelectorAll("tbody tr"), item =>
                item.QuerySelectorAll("a").Any(link => link.TextContent.Trim() == code));
            var cells = row.QuerySelectorAll("td");
            Assert.Equal("12", cells[4].TextContent.Trim());
            Assert.Equal("6.00", cells[5].TextContent.Trim());
            Assert.Equal("2", cells[6].TextContent.Trim());
            Assert.Equal("25.00 €", cells[7].TextContent.Trim());
            using var scope = application.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var donations = await context.Donations.AsNoTracking().Where(donation => donation.ReferralEntity.Code == code).ToListAsync();
            DateTime lastPaid = donations.Where(donation => donation.PaymentStatus == PaymentStatus.Payed).Max(donation => donation.DonationDate);
            DateTime lastPending = donations.Where(donation => donation.PaymentStatus != PaymentStatus.Payed).Max(donation => donation.DonationDate);
            var culture = CultureInfo.GetCultureInfo("en-US");
            Assert.Equal(lastPaid.ToString("g", culture), cells[8].TextContent.Trim());
            Assert.NotEqual(lastPending.ToString("g", culture), cells[8].TextContent.Trim());
            var image = Assert.IsAssignableFrom<IHtmlImageElement>(cells[0].QuerySelector("img"));
            var imageUri = new Uri(application.BaseAddress, image.GetAttribute("src"));
            Assert.Equal(application.BaseAddress.Authority, imageUri.Authority);
            using var imageResponse = await client.GetAsync(imageUri);
            Assert.Equal(HttpStatusCode.OK, imageResponse.StatusCode);
            Assert.StartsWith("image/", imageResponse.Content.Headers.ContentType?.MediaType);
        }

        /// <summary>
        /// Two concurrent hosts isolate both databases and reject each other's verification contexts.
        /// </summary>
        /// <returns>A task representing the operation.</returns>
        [Fact]
        public async Task TwoHosts_IsolateApplicationAndInfrastructureData()
        {
            await using var first = await BrowserTestApplication.StartAsync();
            await using var second = await BrowserTestApplication.StartAsync();
            Assert.NotEqual(first.BaseAddress, second.BaseAddress);
            string marker = $"isolation{Guid.NewGuid():N}";
            using (var firstScope = first.Services.CreateScope())
            {
                var context = firstScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var infrastructure = firstScope.ServiceProvider.GetRequiredService<InfrastructureDbContext>();
                first.AssertOwnsDatabase(context);
                Assert.Throws<InvalidOperationException>(() => second.AssertOwnsDatabase(context));
                context.Referrals.Add(new Referral { Code = marker, Active = true, CreateDate = DateTime.UtcNow });
                infrastructure.DomainIdentifiers.Add(new DomainIdentifier { DomainName = marker, Environment = "Development", Created = DateTime.UtcNow });
                await context.SaveChangesAsync();
                await infrastructure.SaveChangesAsync();
            }

            using (var secondScope = second.Services.CreateScope())
            {
                var context = secondScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var infrastructure = secondScope.ServiceProvider.GetRequiredService<InfrastructureDbContext>();
                second.AssertOwnsDatabase(context);
                Assert.Throws<InvalidOperationException>(() => first.AssertOwnsDatabase(context));
                Assert.False(await context.Referrals.AnyAsync(referral => referral.Code == marker));
                Assert.False(await infrastructure.DomainIdentifiers.AnyAsync(domain => domain.DomainName == marker));
            }

            using var verificationScope = first.Services.CreateScope();
            Assert.True(await verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Referrals.AnyAsync(referral => referral.Code == marker));
            Assert.True(await verificationScope.ServiceProvider.GetRequiredService<InfrastructureDbContext>().DomainIdentifiers.AnyAsync(domain => domain.DomainName == marker));
        }

        /// <summary>
        /// Disposing one host releases its HTTP listener while another host remains usable.
        /// </summary>
        /// <returns>A task representing the operation.</returns>
        [Fact]
        public async Task DisposeAsync_StopsOnlyItsOwnListenerAndRejectsOwnershipChecks()
        {
            await using var first = await BrowserTestApplication.StartAsync();
            await using var second = await BrowserTestApplication.StartAsync();
            using var scope = first.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            using var firstClient = CreateClient(first);
            using var secondClient = CreateClient(second);
            using var initial = await firstClient.GetAsync("/Identity/Account/Login");
            Assert.Equal(HttpStatusCode.OK, initial.StatusCode);

            await first.DisposeAsync();

            Assert.Throws<InvalidOperationException>(() => first.AssertOwnsDatabase(context));
            await Assert.ThrowsAsync<HttpRequestException>(async () =>
            {
                using var response = await firstClient.GetAsync("/Identity/Account/Login");
            });
            using var remaining = await secondClient.GetAsync("/Identity/Account/Login");
            Assert.Equal(HttpStatusCode.OK, remaining.StatusCode);
            await first.DisposeAsync();
        }

        private static HttpClient CreateClient(BrowserTestApplication application)
        {
            var handler = new HttpClientHandler { AllowAutoRedirect = false, UseProxy = false };
            var client = new HttpClient(handler)
            {
                BaseAddress = application.BaseAddress,
                Timeout = TimeSpan.FromSeconds(20),
            };
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US");
            return client;
        }
    }
}
