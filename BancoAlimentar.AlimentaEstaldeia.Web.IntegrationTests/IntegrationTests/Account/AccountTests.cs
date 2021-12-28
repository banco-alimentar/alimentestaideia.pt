// -----------------------------------------------------------------------
// <copyright file="AccountTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests.Account
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using AngleSharp.Html.Dom;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using BancoAlimentar.AlimentaEstaIdeia.Web.IntegrationTests;
    using BancoAlimentar.AlimentaEstaIdeia.Web.IntegrationTests.Helpers;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Class for the account tests.
    /// </summary>
    public class AccountTests : IClassFixture<CustomWebApplicationFactory<Startup>>
    {
        private readonly HttpClient client;
        private readonly CustomWebApplicationFactory<Startup> factory;
        private readonly ITestOutputHelper outputHelper;
        private UserManager<WebUser> userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountTests"/> class.
        /// </summary>
        /// <param name="factory">Factory class.</param>
        /// <param name="outputHelper">Test output helper.</param>
        public AccountTests(CustomWebApplicationFactory<Startup> factory, ITestOutputHelper outputHelper)
        {
            this.factory = factory;
            this.outputHelper = outputHelper;
            this.client = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var serviceProvider = services.BuildServiceProvider();
                    this.userManager = serviceProvider.GetRequiredService<UserManager<WebUser>>();
                });
            })
            .CreateClient();
        }

        /// <summary>
        /// Checks if a user can register and login after the registration process.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task Can_RegisterNewUser_And_LoginSuccessfully()
        {
            // Arrange
            var defaultPage = await this.client.GetAsync("/Identity/Account/Register");
            var content = await HtmlHelpers.GetDocumentAsync(defaultPage);

            // Act
            var response = await this.client.SendAsync(
                (IHtmlFormElement)content.QuerySelector("form[id='registerForm']"),
                (IHtmlButtonElement)content.QuerySelector("button[id='registerBtn']"),
                new Dictionary<string, string>
                {
                    ["Input.Email"] = "xyz@xyz.com",
                    ["Input.FullName"] = "XYZ",
                    ["Input.Password"] = "Test@123",
                    ["Input.ConfirmPassword"] = "Test@123",
                    ["Input.PhoneNumber"] = "1234567890",
                    ["Input.Nif"] = "196807050",
                    ["Input.CompanyName"] = "Xyz org",
                    ["Input.Address.Address1"] = "address 1",
                    ["Input.Address.Address2"] = "address 2",
                    ["Input.Address.City"] = "city",
                    ["Input.Address.PostalCode"] = "123456",
                    ["Input.Address.Country"] = "Test",
                });

            // Assert if new user was created.
            var user = await this.userManager.FindByEmailAsync("xyz@xyz.com");
            Assert.NotNull(user);

            // Confirm newly created users email.
            var code = await this.userManager.GenerateEmailConfirmationTokenAsync(user);
            var result = await this.userManager.ConfirmEmailAsync(user, code);
            Assert.True(result.Succeeded);

            if (response.RequestMessage.RequestUri.AbsolutePath != "/Identity/Account/RegisterConfirmation")
            {
                this.outputHelper.WriteLine(await response.Content.ReadAsStringAsync());
            }

            Assert.Equal("/Identity/Account/RegisterConfirmation", response.RequestMessage.RequestUri.AbsolutePath);

            defaultPage = await this.client.GetAsync("/Identity/Account/Login");
            content = await HtmlHelpers.GetDocumentAsync(defaultPage);

            response = await this.client.SendAsync(
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
            Assert.Equal(this.client.BaseAddress, response.RequestMessage.RequestUri);
        }
    }
}
