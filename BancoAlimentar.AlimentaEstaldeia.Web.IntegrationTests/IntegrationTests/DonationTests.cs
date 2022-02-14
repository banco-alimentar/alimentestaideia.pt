// -----------------------------------------------------------------------
// <copyright file="DonationTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using AngleSharp.Html.Dom;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Testing.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using BancoAlimentar.AlimentaEstaIdeia.Web.IntegrationTests;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Class to test the donation process.
    /// </summary>
    public class DonationTests : IClassFixture<CustomWebApplicationFactory<Startup>>
    {
        private readonly HttpClient client;
        private readonly CustomWebApplicationFactory<Startup> factory;
        private readonly ITestOutputHelper outputHelper;
        private DonationRepository donationRepository;
        private UserManager<WebUser> userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="DonationTests"/> class.
        /// </summary>
        /// <param name="factory">Factory class.</param>
        /// <param name="outputHelper">Test output helper.</param>
        public DonationTests(CustomWebApplicationFactory<Startup> factory, ITestOutputHelper outputHelper)
        {
            this.factory = factory;
            this.outputHelper = outputHelper;
            this.client = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var serviceProvider = services.BuildServiceProvider();
                    this.donationRepository = serviceProvider.GetRequiredService<DonationRepository>();
                    this.userManager = serviceProvider.GetRequiredService<UserManager<WebUser>>();
                });
            })
            .CreateClient();
        }

        /// <summary>
        /// Checks if an anonymous user can make a donation without a receipt.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task Can_AnonymousUser_Donate_WithoutReceipt()
        {
            // Arrange
            var defaultPage = await this.client.GetAsync("/Donation");
            var content = await HtmlHelpers.GetDocumentAsync(defaultPage);
            var email = "testname1@test.com";

            // Act
            var response = await this.client.SendAsync(
                (IHtmlFormElement)content.QuerySelector("form[id='donationForm']"),
                (IHtmlInputElement)content.QuerySelector("input[id='submit']"),
                new Dictionary<string, string>
                {
                    ["DonatedItems"] = "1:1,2:1,3:1,4:1,5:1,6:1",
                    ["FoodBankId"] = "1",
                    ["Name"] = "Test Name",
                    ["Amount"] = "1",
                    ["CompanyName"] = "Test Company",
                    ["Email"] = email,
                    ["Country"] = "Portugal",
                    ["WantsReceipt"] = "false",
                    ["AcceptsTerms"] = "true",
                });

            response.EnsureSuccessStatusCode();

            // verify if anonymous user was created.
            var user = await this.userManager.FindByEmailAsync(email);
            Assert.NotNull(user);

            // Verify if it was able to create a donation for this user.
            var userDonations = this.donationRepository.GetUserDonation(user.Id);
            Assert.Single(userDonations);

            // Verify if it was able to redirect to Payment page.
            Assert.Equal(HttpStatusCode.OK, defaultPage.StatusCode);
            Assert.Equal("/Payment", response.RequestMessage.RequestUri.AbsolutePath);
        }

        /// <summary>
        /// Checks if an annonymous user can make a donation with a receipt.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task Can_AnonymousUser_Donate_WithReceipt()
        {
            var defaultPage = await this.client.GetAsync("/Donation");
            var content = await HtmlHelpers.GetDocumentAsync(defaultPage);
            var email = "testname2@test.com";

            // Act
            var response = await this.client.SendAsync(
                (IHtmlFormElement)content.QuerySelector("form[id='donationForm']"),
                (IHtmlInputElement)content.QuerySelector("input[id='submit']"),
                new Dictionary<string, string>
                {
                    ["DonatedItems"] = "1:1,2:1,3:1,4:1,5:1,6:1",
                    ["FoodBankId"] = "1",
                    ["Name"] = "Test Name",
                    ["Amount"] = "1",
                    ["CompanyName"] = "Test Company",
                    ["Email"] = email,
                    ["Address"] = "Test Address",
                    ["PostalCode"] = "123456",
                    ["Nif"] = "196807050",
                    ["Country"] = "Portugal",
                    ["WantsReceipt"] = "true",
                    ["AcceptsTerms"] = "true",
                });

            response.EnsureSuccessStatusCode();

            // verify if anonymous user was created.
            var user = await this.userManager.FindByEmailAsync(email);
            Assert.NotNull(user);

            // Verify if it was able to create a donation for this user.
            var userDonations = this.donationRepository.GetUserDonation(user.Id);
            Assert.Single(userDonations);

            // Verify if it was able to redirect to Payment page.
            Assert.Equal(HttpStatusCode.OK, defaultPage.StatusCode);
            Assert.Equal("/Payment", response.RequestMessage.RequestUri.AbsolutePath);
        }

        /// <summary>
        /// Checks if a donation attempt fails ModelState validation if coutry is incorrect.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task Cannot_Donate_With_Invalid_Country()
        {
            var defaultPage = await this.client.GetAsync("/Donation");
            var content = await HtmlHelpers.GetDocumentAsync(defaultPage);
            var email = "testname232@test.com";

            // Act
            var response = await this.client.SendAsync(
                (IHtmlFormElement)content.QuerySelector("form[id='donationForm']"),
                (IHtmlInputElement)content.QuerySelector("input[id='submit']"),
                new Dictionary<string, string>
                {
                    ["DonatedItems"] = "1:1,2:1,3:1,4:1,5:1,6:1",
                    ["FoodBankId"] = "1",
                    ["Name"] = "Test Name",
                    ["Amount"] = "1",
                    ["CompanyName"] = "Test Company",
                    ["Email"] = email,
                    ["Address"] = "Test Address",
                    ["PostalCode"] = "123456",
                    ["Nif"] = "196807050",
                    ["Country"] = "Test",
                    ["WantsReceipt"] = "true",
                    ["AcceptsTerms"] = "true",
                });

            response.EnsureSuccessStatusCode();

            // verify if anonymous user was created.
            var user = await this.userManager.FindByEmailAsync(email);
            Assert.Null(user);
        }

        /// <summary>
        /// Checks if an anonymous user can not make a donation with missing fileds.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task AnonymousUser_Cannot_Donate_WithMissingRequiredFields()
        {
            var defaultPage = await this.client.GetAsync("/Donation");
            var content = await HtmlHelpers.GetDocumentAsync(defaultPage);
            var email = "testname3@test.com";

            // Act
            var response = await this.client.SendAsync(
                (IHtmlFormElement)content.QuerySelector("form[id='donationForm']"),
                (IHtmlInputElement)content.QuerySelector("input[id='submit']"),
                new Dictionary<string, string>
                {
                    ["DonatedItems"] = "1:1,2:1,3:1,4:1,5:1,6:1",
                    ["FoodBankId"] = "1",
                    ["Name"] = "Test Name",
                    ["Email"] = email,
                    ["Amount"] = "1",
                    ["CompanyName"] = "Test Company",
                    ["Country"] = "Portugal",
                    ["WantsReceipt"] = "true",
                    ["AcceptsTerms"] = "false",
                });

            // verify if anonymous user was created.
            var user = await this.userManager.FindByEmailAsync(email);
            Assert.Null(user);

            // Verify if it stays on the donation page.
            Assert.Equal(HttpStatusCode.OK, defaultPage.StatusCode);
            Assert.Equal("/Donation", response.RequestMessage.RequestUri.AbsolutePath);
        }

        /// <summary>
        /// Checks if donation page is being redirected to maintenance when enabled.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task Can_Redirect_To_MaintenancePage_When_MaintenenceIsEnabled()
        {
            var client = this.factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.IntegrationTesting.json", true).Build();
                });
            }).CreateClient();

            var response = await client.GetAsync("/Donation");

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            Assert.Equal("/Maintenance", response.RequestMessage.RequestUri.AbsolutePath);
        }
    }
}
