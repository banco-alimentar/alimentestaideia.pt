using AngleSharp.Html.Dom;
using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
using BancoAlimentar.AlimentaEstaIdeia.Repository;
using BancoAlimentar.AlimentaEstaIdeia.Web;
using BancoAlimentar.AlimentaEstaIdeia.Web.IntegrationTests;
using BancoAlimentar.AlimentaEstaIdeia.Web.IntegrationTests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    public class DonationTests : IClassFixture<CustomWebApplicationFactory<Startup>>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory<Startup>
            _factory;
        private DonationRepository _donationRepository;
        private UserManager<WebUser> _userManager;

        public DonationTests(CustomWebApplicationFactory<Startup> factory)
        {
            _factory = factory;
            _client = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var serviceProvider = services.BuildServiceProvider();
                    _donationRepository = serviceProvider.GetRequiredService<DonationRepository>();
                    _userManager = serviceProvider.GetRequiredService<UserManager<WebUser>>();
                });
            })
            .CreateClient();
        }

        [Fact]
        public async Task Can_AnonymousUser_Donate_WithoutReceipt()
        {
            // Arrange
            var defaultPage = await _client.GetAsync("/Donation");
            var content = await HtmlHelpers.GetDocumentAsync(defaultPage);

            var email = "testname1@test.com";
            // Act
            var response = await _client.SendAsync(
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
                    ["Country"] = "Test",
                    ["WantsReceipt"] = "false",
                    ["AcceptsTerms"] = "true",
                });

            response.EnsureSuccessStatusCode();

            //verify if anonymous user was created.
            var user = await _userManager.FindByEmailAsync(email);
            Assert.NotNull(user);

            //Verify if it was able to create a donation for this user.
            var userDonations = _donationRepository.GetUserDonation(user.Id);
            Assert.Single(userDonations);

            //Verify if it was able to redirect to Payment page.
            Assert.Equal(HttpStatusCode.OK, defaultPage.StatusCode);
            Assert.Equal("/Payment", response.RequestMessage.RequestUri.AbsolutePath);           

        }

        [Fact]
        public async Task Can_AnonymousUser_Donate_WithReceipt()
        {
            var defaultPage = await _client.GetAsync("/Donation");
            var content = await HtmlHelpers.GetDocumentAsync(defaultPage);
            var email = "testname2@test.com";

            // Act
            var response = await _client.SendAsync(
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
                    ["Nif"] = "123456789",
                    ["Country"] = "Test",
                    ["WantsReceipt"] = "true",
                    ["AcceptsTerms"] = "true",
                });

            response.EnsureSuccessStatusCode();

            //verify if anonymous user was created.
            var user = await _userManager.FindByEmailAsync(email);
            Assert.NotNull(user);

            //Verify if it was able to create a donation for this user.
            var userDonations = _donationRepository.GetUserDonation(user.Id);
            Assert.Single(userDonations);

            //Verify if it was able to redirect to Payment page.
            Assert.Equal(HttpStatusCode.OK, defaultPage.StatusCode);
            Assert.Equal("/Payment", response.RequestMessage.RequestUri.AbsolutePath);
        }

        [Fact]
        public async Task AnonymousUser_Cannot_Donate_WithMissingRequiredFields()
        {
            var defaultPage = await _client.GetAsync("/Donation");
            var content = await HtmlHelpers.GetDocumentAsync(defaultPage);
            var email = "testname3@test.com";

            // Act
            var response = await _client.SendAsync(
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
                    ["Country"] = "Test",
                    ["WantsReceipt"] = "true",
                    ["AcceptsTerms"] = "false",
                });


            //verify if anonymous user was created.
            var user = await _userManager.FindByEmailAsync(email);
            Assert.NotNull(user);

            //Verify there are no donations for this user.
            var userDonations = _donationRepository.GetUserDonation(user.Id);
            Assert.True(userDonations.Count == 0);

            //Verify if it stays on the donation page.
            Assert.Equal(HttpStatusCode.OK, defaultPage.StatusCode);
            Assert.Equal("/Donation", response.RequestMessage.RequestUri.AbsolutePath);
        }

        [Fact]
        public async Task Can_Redirect_To_MaintenancePage_When_MaintenenceIsEnabled()
        {
            var client = _factory.WithWebHostBuilder(builder =>
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
