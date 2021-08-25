using AngleSharp.Html.Dom;
using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
using BancoAlimentar.AlimentaEstaIdeia.Web;
using BancoAlimentar.AlimentaEstaIdeia.Web.IntegrationTests;
using BancoAlimentar.AlimentaEstaIdeia.Web.IntegrationTests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests.Account
{
    public class AccountTests : IClassFixture<CustomWebApplicationFactory<Startup>>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory<Startup>
            _factory;
        private UserManager<WebUser> _userManager;

        public AccountTests(CustomWebApplicationFactory<Startup> factory)
        {
            _factory = factory;
            _client = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var serviceProvider = services.BuildServiceProvider();
                    _userManager = serviceProvider.GetRequiredService<UserManager<WebUser>>();
                });
            })
            .CreateClient();
        }

        [Fact]
        public async Task Can_RegisterNewUser_And_LoginSuccessfully()
        {
            // Arrange
            var defaultPage = await _client.GetAsync("/Identity/Account/Register");
            var content = await HtmlHelpers.GetDocumentAsync(defaultPage);

            // Act
            var response = await _client.SendAsync(
                (IHtmlFormElement)content.QuerySelector("form[id='registerForm']"),
                (IHtmlButtonElement)content.QuerySelector("button[id='registerBtn']"),
                new Dictionary<string, string>
                {
                    ["Input.Email"] = "xyz@xyz.com",
                    ["Input.FullName"] = "XYZ",
                    ["Input.Password"] = "Test@123",
                    ["Input.ConfirmPassword"] = "Test@123",
                    ["Input.PhoneNumber"] = "1234567890",
                    ["Input.Nif"] = "123456789",
                    ["Input.CompanyName"] = "Xyz org",
                    ["Input.Address.Address1"] = "address 1",
                    ["Input.Address.Address2"] = "address 2",
                    ["Input.Address.City"] = "city",
                    ["Input.Address.PostalCode"] = "123456",
                    ["Input.Address.Country"] = "Test"
                });

            //Assert if new user was created.
            var user = await _userManager.FindByEmailAsync("xyz@xyz.com");
            Assert.NotNull(user);

            //Confirm newly created users email.
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var result = await _userManager.ConfirmEmailAsync(user, code);
            Assert.True(result.Succeeded);

            Assert.Equal("/Identity/Account/RegisterConfirmation", response.RequestMessage.RequestUri.AbsolutePath);

            defaultPage = await _client.GetAsync("/Identity/Account/Login");
            content = await HtmlHelpers.GetDocumentAsync(defaultPage);

            response = await _client.SendAsync(
                (IHtmlFormElement)content.QuerySelector("form[id='account']"),
                (IHtmlButtonElement)content.QuerySelector("button[id='loginBtn']"),
                new Dictionary<string, string>
                {
                    ["Input.Email"] = "xyz@xyz.com",
                    ["Input.Password"] = "Test@123",
                });
            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, defaultPage.StatusCode);
            Assert.Equal(_client.BaseAddress, response.RequestMessage.RequestUri);
            
        }

    }
}
