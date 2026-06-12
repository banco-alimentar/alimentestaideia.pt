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
