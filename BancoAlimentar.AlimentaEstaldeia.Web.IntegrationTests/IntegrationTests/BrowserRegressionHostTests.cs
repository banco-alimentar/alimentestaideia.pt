// <copyright file="BrowserRegressionHostTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Easypay.Rest.Client.Api;
    using Easypay.Rest.Client.Client;
    using Easypay.Rest.Client.Model;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>Checks the local browser controls without Chrome, SMTP, or cloud resources.</summary>
    [Trait("Category", "Offline")]
    public class BrowserRegressionHostTests
    {
        /// <summary>Real uploads are served locally and removed with their owned host.</summary>
        /// <returns>The asynchronous test.</returns>
        [Fact]
        public async Task UploadedImages_AreIsolatedServedAndDisposed()
        {
            string root;
            await using (var application = await BrowserTestApplication.StartAsync())
            {
                root = application.UploadRoot;
                await using var other = await BrowserTestApplication.StartAsync();
                Assert.NotEqual(root, other.UploadRoot);
                using var scope = application.Services.CreateScope();
                var images = scope.ServiceProvider.GetRequiredService<ReferralImageService>();
                byte[] bytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jvXcAAAAASUVORK5CYII=");
                using var stream = new MemoryStream(bytes);
                var file = new FormFile(stream, 0, bytes.Length, "ImageUpload", "pixel.png")
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "image/png",
                };
                string key = await images.UploadAsync(987, file);
                using var client = new HttpClient(new HttpClientHandler { UseProxy = false });
                using var response = await client.GetAsync(new Uri(application.BaseAddress, images.ResolveUrl(key)));
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(bytes, await response.Content.ReadAsByteArrayAsync());
                using var missing = await client.GetAsync(new Uri(other.BaseAddress, images.ResolveUrl(key)));
                Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
                Assert.True(File.Exists(Path.Combine(root, key)));
            }

            Assert.False(Directory.Exists(root));
        }

        /// <summary>Expiry rejects an authentic token without delaying or replacing Identity validation.</summary>
        /// <returns>The asynchronous test.</returns>
        [Fact]
        public async Task ExpireIdentityTokens_InvalidatesOnlyTheOwnedHostsTokens()
        {
            await using var application = await BrowserTestApplication.StartAsync();
            await using var other = await BrowserTestApplication.StartAsync();
            using var scope = application.Services.CreateScope();
            using var otherScope = other.Services.CreateScope();
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<WebUser>>();
            var otherManager = otherScope.ServiceProvider.GetRequiredService<UserManager<WebUser>>();
            var user = await manager.FindByEmailAsync(application.Configuration["SeleniumTest:Local:Site:Username"]);
            var otherUser = await otherManager.FindByEmailAsync(other.Configuration["SeleniumTest:Local:Site:Username"]);
            string token = await manager.GeneratePasswordResetTokenAsync(user);
            string otherToken = await otherManager.GeneratePasswordResetTokenAsync(otherUser);
            string provider = manager.Options.Tokens.PasswordResetTokenProvider;
            Assert.True(await manager.VerifyUserTokenAsync(user, provider, "ResetPassword", token));

            application.ExpireIdentityTokens();

            Assert.False(await manager.VerifyUserTokenAsync(user, provider, "ResetPassword", token));
            Assert.True(await otherManager.VerifyUserTokenAsync(otherUser, otherManager.Options.Tokens.PasswordResetTokenProvider, "ResetPassword", otherToken));
        }

        /// <summary>The real SDK's configured provider is the owned loopback simulator.</summary>
        /// <returns>The asynchronous test.</returns>
        [Fact]
        public async Task PaymentSimulation_IsLoopbackAndRejectsUnknownPayments()
        {
            await using var application = await BrowserTestApplication.StartAsync();
            Assert.Equal(application.BaseAddress.GetLeftPart(UriPartial.Authority), application.Configuration["Easypay:BaseUrl"]);
            using var client = new HttpClient(new HttpClientHandler { UseProxy = false }) { BaseAddress = application.BaseAddress };
            using var unknown = await client.GetAsync("/2.0/single/" + Guid.NewGuid());
            Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
            string payload = EasyPayWebhookPayloadBuilder.BuildMBWayPaymentNotification(Guid.NewGuid(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            using var invalid = await client.PostAsync("/easypay/payment", new StringContent(payload, Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.Forbidden, invalid.StatusCode);
        }

        /// <summary>The real generated SDK can create and inspect simulated single payments.</summary>
        /// <returns>The asynchronous test.</returns>
        [Fact]
        public async Task PaymentSimulation_SupportsRealSdkCheckoutAndPaidLookup()
        {
            await using var application = await BrowserTestApplication.StartAsync();
            var sdk = new SinglePaymentApi(new Easypay.Rest.Client.Client.Configuration { BasePath = new Uri(application.BaseAddress, "2.0").AbsoluteUri });
            var response = await sdk.SinglePostWithHttpInfoAsync(new SinglePostRequest
            {
                Key = Guid.NewGuid().ToString(),
                Value = 5,
                Method = SinglePaymentMethods.Mbw,
            });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.True(response.Data != null, "The loopback provider response must deserialize through the real SDK. " + response.ErrorText + "; " + response.RawContent);
            Guid id = Guid.Parse(response.Data.Id);
            Assert.Equal("pending", (await sdk.SingleIdGetAsync(id)).Method.Status);
            application.Payments.MarkPaid(id);
            Assert.Equal("paid", (await sdk.SingleIdGetAsync(id)).Method.Status);
        }

        /// <summary>Even a direct PayPal callback cannot reach a real provider from the browser host.</summary>
        /// <returns>The asynchronous test.</returns>
        [Fact]
        public async Task PayPalCallback_IsRejectedByLocalTransportWithoutChangingDonation()
        {
            await using var application = await BrowserTestApplication.StartAsync();
            using var scope = application.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            int id = await context.Donations.Where(donation => donation.PaymentStatus == PaymentStatus.WaitingPayment).Select(donation => donation.Id).FirstAsync();
            using var client = new HttpClient(new HttpClientHandler { UseProxy = false, AllowAutoRedirect = false });

            using var response = await client.GetAsync(new Uri(application.BaseAddress, "/Payment?handler=ReferencePayedViaPayPal&donationId=" + id + "&token=browser-test-order"));

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal(1, application.Payments.RejectedPayPalRequests);
            Assert.Equal(PaymentStatus.WaitingPayment, (await context.Donations.AsNoTracking().SingleAsync(donation => donation.Id == id)).PaymentStatus);
        }

        /// <summary>Invalid reset links return a safe client response, never a development exception.</summary>
        /// <param name="query">Missing or malformed token query.</param>
        /// <returns>The asynchronous test.</returns>
        [Theory]
        [InlineData("")]
        [InlineData("?code=")]
        [InlineData("?code=a")]
        [InlineData("?code=%25%25%25")]
        public async Task InvalidResetLink_ReturnsSafeBadRequest(string query)
        {
            await using var application = await BrowserTestApplication.StartAsync();
            using var client = new HttpClient(new HttpClientHandler { UseProxy = false });
            using var response = await client.GetAsync(new Uri(application.BaseAddress, "/Identity/Account/ResetPassword" + query));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            string body = await response.Content.ReadAsStringAsync();
            Assert.Contains("code", body);
            Assert.DoesNotContain("Exception", body);
        }
    }
}
