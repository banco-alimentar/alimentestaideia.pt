// -----------------------------------------------------------------------
// <copyright file="ReferralImageTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Xunit;

    /// <summary>
    /// Integration tests for referral campaign images.
    /// </summary>
    public class ReferralImageTests : IClassFixture<CustomWebApplicationFactory>
    {
        private static readonly byte[] MinimalPng = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

        private readonly CustomWebApplicationFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferralImageTests"/> class.
        /// </summary>
        /// <param name="factory">Web application factory.</param>
        public ReferralImageTests(CustomWebApplicationFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// Visiting a referral link shows the campaign image on the donation page.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Get_ValidCodeWithImage_ShowsImageOnDonationPage()
        {
            const string imageUrl = "https://cdn.integration.test/referrals/campaign.png";
            var webFactory = this.factory.WithWebHostBuilder(_ => { });
            IntegrationTestDataSeeder.ReferralSeed referralSeed;
            using (var scope = webFactory.Services.CreateScope())
            {
                referralSeed = await IntegrationTestDataSeeder.SeedActiveReferralAsync(
                    scope.ServiceProvider,
                    imageUrl: imageUrl);
            }

            var client = webFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = true,
            });

            var response = await client.GetAsync($"/Referral?text={referralSeed.Code}");

            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
            Assert.Equal("/Donation", response.RequestMessage?.RequestUri?.AbsolutePath);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("referral-campaign-image", html);
            Assert.Contains(imageUrl, html);
            Assert.Contains("Integration Referral Campaign", html);
        }

        /// <summary>
        /// Visiting a referral link shows the campaign tag line on the donation page.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Get_ValidCodeWithTagLine_ShowsTagLineOnDonationPage()
        {
            const string tagLine = "Together we can feed more families.";
            var webFactory = this.factory.WithWebHostBuilder(_ => { });
            IntegrationTestDataSeeder.ReferralSeed referralSeed;
            using (var scope = webFactory.Services.CreateScope())
            {
                referralSeed = await IntegrationTestDataSeeder.SeedActiveReferralAsync(
                    scope.ServiceProvider,
                    tagLine: tagLine);
            }

            var client = webFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = true,
            });

            var response = await client.GetAsync($"/Referral?text={referralSeed.Code}");

            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("referral-campaign-tagline", html);
            Assert.Contains(tagLine, html);
        }

        /// <summary>
        /// Campaign edit shows a QR code for the referral donation link.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task CampaignEdit_ShowsReferralQrCode()
        {
            var webFactory = this.factory.WithWebHostBuilder(_ => { });
            IntegrationTestDataSeeder.ReferralSeed referralSeed;
            using (var scope = webFactory.Services.CreateScope())
            {
                referralSeed = await IntegrationTestDataSeeder.SeedActiveReferralAsync(scope.ServiceProvider);
            }

            var client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                webFactory,
                referralSeed.OwnerEmail,
                IntegrationTestCredentials.DefaultPassword);

            var editPageResponse = await client.GetAsync($"/Identity/Account/Manage/CampaignEdit?id={referralSeed.ReferralId}");
            editPageResponse.EnsureSuccessStatusCode();
            var editPageHtml = await editPageResponse.Content.ReadAsStringAsync();
            Assert.Contains("data:image/png;base64,", editPageHtml);
            Assert.Contains($"/Referral/{referralSeed.Code}", editPageHtml);
        }

        /// <summary>
        /// Campaign owners can save a tag line from CampaignEdit.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task CampaignEdit_SavesTagLine()
        {
            const string tagLine = "Help us reach our goal this month.";
            var webFactory = this.factory.WithWebHostBuilder(_ => { });
            IntegrationTestDataSeeder.ReferralSeed referralSeed;
            using (var scope = webFactory.Services.CreateScope())
            {
                referralSeed = await IntegrationTestDataSeeder.SeedActiveReferralAsync(scope.ServiceProvider);
            }

            var client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                webFactory,
                referralSeed.OwnerEmail,
                IntegrationTestCredentials.DefaultPassword);

            var editPageResponse = await client.GetAsync($"/Identity/Account/Manage/CampaignEdit?id={referralSeed.ReferralId}");
            editPageResponse.EnsureSuccessStatusCode();
            var editPageHtml = await editPageResponse.Content.ReadAsStringAsync();
            var antiForgeryToken = this.ExtractAntiForgeryToken(editPageHtml);
            Assert.False(string.IsNullOrEmpty(antiForgeryToken));

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(antiForgeryToken), "__RequestVerificationToken");
            form.Add(new StringContent(referralSeed.ReferralId.ToString()), "Referral.Id");
            form.Add(new StringContent(referralSeed.Code), "Referral.Code");
            form.Add(new StringContent("Integration Referral Campaign"), "Referral.Name");
            form.Add(new StringContent(tagLine), "Referral.TagLine");
            form.Add(new StringContent("true"), "Referral.Active");
            form.Add(new StringContent("true"), "Referral.IsPublic");
            form.Add(new StringContent("false"), "RemoveImage");

            var postResponse = await client.PostAsync(
                $"/Identity/Account/Manage/CampaignEdit?id={referralSeed.ReferralId}",
                form);

            postResponse.EnsureSuccessStatusCode();

            using (var scope = webFactory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var referral = await context.Referrals.AsNoTracking()
                    .FirstAsync(r => r.Id == referralSeed.ReferralId);
                Assert.Equal(tagLine, referral.TagLine);
            }
        }

        /// <summary>
        /// Campaign owners can upload an image from CampaignEdit.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Get_ValidReferralLink_IncrementsLinkOpenCount()
        {
            var webFactory = this.factory.WithWebHostBuilder(_ => { });
            IntegrationTestDataSeeder.ReferralSeed referralSeed;
            using (var scope = webFactory.Services.CreateScope())
            {
                referralSeed = await IntegrationTestDataSeeder.SeedActiveReferralAsync(scope.ServiceProvider);
            }

            var client = webFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });

            var firstResponse = await client.GetAsync($"/Referral?text={referralSeed.Code}");
            var secondResponse = await client.GetAsync($"/Referral?text={referralSeed.Code}");

            Assert.Equal(System.Net.HttpStatusCode.Redirect, firstResponse.StatusCode);
            Assert.Equal(System.Net.HttpStatusCode.Redirect, secondResponse.StatusCode);

            using (var scope = webFactory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var referral = await context.Referrals.AsNoTracking()
                    .FirstAsync(r => r.Id == referralSeed.ReferralId);
                Assert.Equal(2, referral.LinkOpenCount);
            }
        }

        /// <summary>
        /// Campaign detail shows link opens and donation evolution chart.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task CampaignDetail_ShowsLinkOpenCountAndDonationChart()
        {
            var webFactory = this.factory.WithWebHostBuilder(_ => { });
            IntegrationTestDataSeeder.ReferralSeed referralSeed;
            using (var scope = webFactory.Services.CreateScope())
            {
                referralSeed = await IntegrationTestDataSeeder.SeedActiveReferralAsync(scope.ServiceProvider);
                await IntegrationTestDataSeeder.SeedPaidDonationWithoutInvoiceAsync(
                    scope.ServiceProvider,
                    Guid.NewGuid(),
                    referralId: referralSeed.ReferralId,
                    donationDate: DateTime.UtcNow.AddDays(-2));
                await IntegrationTestDataSeeder.SeedPaidDonationWithoutInvoiceAsync(
                    scope.ServiceProvider,
                    Guid.NewGuid(),
                    referralId: referralSeed.ReferralId,
                    donationDate: DateTime.UtcNow.AddDays(-1));
            }

            var anonymousClient = webFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
            await anonymousClient.GetAsync($"/Referral?text={referralSeed.Code}");

            var client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                webFactory,
                referralSeed.OwnerEmail,
                IntegrationTestCredentials.DefaultPassword);

            var detailResponse = await client.GetAsync($"/Identity/Account/Manage/CampaignDetail?id={referralSeed.ReferralId}");
            detailResponse.EnsureSuccessStatusCode();
            var html = await detailResponse.Content.ReadAsStringAsync();

            Assert.Contains("campaignDonationChart", html);
            Assert.Contains("chart.js", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("id=\"referral-link-open-count\">1<", html);
            Assert.Contains("\"amounts\":[5,10]", html);
        }

        /// <summary>
        /// Campaign owners can upload an image from CampaignEdit.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task CampaignEdit_UploadsImage_AndPersistsImageUrl()
        {
            var webFactory = this.CreateLocalStorageFactory();
            IntegrationTestDataSeeder.ReferralSeed referralSeed;
            using (var scope = webFactory.Services.CreateScope())
            {
                referralSeed = await IntegrationTestDataSeeder.SeedActiveReferralAsync(scope.ServiceProvider);
            }

            var client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                webFactory,
                referralSeed.OwnerEmail,
                IntegrationTestCredentials.DefaultPassword);

            var editPageResponse = await client.GetAsync($"/Identity/Account/Manage/CampaignEdit?id={referralSeed.ReferralId}");
            editPageResponse.EnsureSuccessStatusCode();
            var editPageHtml = await editPageResponse.Content.ReadAsStringAsync();
            var antiForgeryToken = this.ExtractAntiForgeryToken(editPageHtml);
            Assert.False(string.IsNullOrEmpty(antiForgeryToken));

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(antiForgeryToken), "__RequestVerificationToken");
            form.Add(new StringContent(referralSeed.ReferralId.ToString()), "Referral.Id");
            form.Add(new StringContent(referralSeed.Code), "Referral.Code");
            form.Add(new StringContent("Integration Referral Campaign"), "Referral.Name");
            form.Add(new StringContent(string.Empty), "Referral.TagLine");
            form.Add(new StringContent("true"), "Referral.Active");
            form.Add(new StringContent("true"), "Referral.IsPublic");
            form.Add(new StringContent("false"), "RemoveImage");

            var imageContent = new ByteArrayContent(MinimalPng);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(imageContent, "ImageUpload", "campaign.png");

            var postResponse = await client.PostAsync(
                $"/Identity/Account/Manage/CampaignEdit?id={referralSeed.ReferralId}",
                form);

            postResponse.EnsureSuccessStatusCode();
            Assert.Contains(
                "CampaignsHistory",
                postResponse.RequestMessage?.RequestUri?.AbsolutePath,
                StringComparison.OrdinalIgnoreCase);

            using (var scope = webFactory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var referral = await context.Referrals.AsNoTracking()
                    .FirstAsync(r => r.Id == referralSeed.ReferralId);
                Assert.False(string.IsNullOrWhiteSpace(referral.ImageUrl));
                Assert.StartsWith($"uploads/referrals/{referralSeed.ReferralId}/", referral.ImageUrl, StringComparison.Ordinal);
                Assert.EndsWith(".png", referral.ImageUrl, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Campaign owners can save changes without re-uploading an image.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task CampaignEdit_UpdatesActive_WithoutImageUpload()
        {
            var webFactory = this.factory.WithWebHostBuilder(_ => { });
            IntegrationTestDataSeeder.ReferralSeed referralSeed;
            using (var scope = webFactory.Services.CreateScope())
            {
                referralSeed = await IntegrationTestDataSeeder.SeedActiveReferralAsync(scope.ServiceProvider);
            }

            var client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                webFactory,
                referralSeed.OwnerEmail,
                IntegrationTestCredentials.DefaultPassword);

            var editPageResponse = await client.GetAsync($"/Identity/Account/Manage/CampaignEdit?id={referralSeed.ReferralId}");
            editPageResponse.EnsureSuccessStatusCode();
            var editPageHtml = await editPageResponse.Content.ReadAsStringAsync();
            var antiForgeryToken = this.ExtractAntiForgeryToken(editPageHtml);
            Assert.False(string.IsNullOrEmpty(antiForgeryToken));

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(antiForgeryToken), "__RequestVerificationToken");
            form.Add(new StringContent(referralSeed.ReferralId.ToString()), "Referral.Id");
            form.Add(new StringContent(referralSeed.Code), "Referral.Code");
            form.Add(new StringContent("Integration Referral Campaign"), "Referral.Name");
            form.Add(new StringContent(string.Empty), "Referral.TagLine");
            form.Add(new StringContent("false"), "Referral.Active");
            form.Add(new StringContent("true"), "Referral.IsPublic");
            form.Add(new StringContent("false"), "RemoveImage");

            var postResponse = await client.PostAsync(
                $"/Identity/Account/Manage/CampaignEdit?id={referralSeed.ReferralId}",
                form);

            postResponse.EnsureSuccessStatusCode();
            Assert.Contains(
                "CampaignsHistory",
                postResponse.RequestMessage?.RequestUri?.AbsolutePath,
                StringComparison.OrdinalIgnoreCase);

            using (var scope = webFactory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var referral = await context.Referrals.AsNoTracking()
                    .FirstAsync(r => r.Id == referralSeed.ReferralId);
                Assert.False(referral.Active);
            }
        }

        private WebApplicationFactory<Program> CreateLocalStorageFactory()
        {
            return this.factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<ReferralImageService>();
                    services.AddScoped<ReferralImageService>(serviceProvider =>
                    {
                        IConfiguration hostConfiguration = serviceProvider.GetRequiredService<IConfiguration>();
                        IConfiguration localImageConfiguration = new ConfigurationBuilder()
                            .AddConfiguration(hostConfiguration)
                            .AddInMemoryCollection(new Dictionary<string, string>
                            {
                                ["AzureStorage:ConnectionString"] = "#{AzureStorage--ConnectionString}#",
                            })
                            .Build();

                        return new ReferralImageService(
                            localImageConfiguration,
                            serviceProvider.GetRequiredService<IWebHostEnvironment>());
                    });
                });
            });
        }

        private string ExtractAntiForgeryToken(string html)
        {
            const string marker = "name=\"__RequestVerificationToken\"";
            var markerIndex = html.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex < 0)
            {
                return null;
            }

            var valueIndex = html.IndexOf("value=\"", markerIndex, StringComparison.OrdinalIgnoreCase);
            if (valueIndex < 0)
            {
                return null;
            }

            valueIndex += "value=\"".Length;
            var valueEnd = html.IndexOf('"', valueIndex);
            return valueEnd < 0 ? null : html.Substring(valueIndex, valueEnd - valueIndex);
        }
    }
}
