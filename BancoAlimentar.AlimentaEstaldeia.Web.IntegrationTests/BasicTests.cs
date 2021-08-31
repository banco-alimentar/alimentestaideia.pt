// -----------------------------------------------------------------------
// <copyright file="BasicTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.IntegrationTests
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Xunit;

    /// <summary>
    /// Class for basic 200 HTTP status code tests.
    /// </summary>
    public class BasicTests
        : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="BasicTests"/> class.
        /// </summary>
        /// <param name="factory">Factory class.</param>
        public BasicTests(WebApplicationFactory<Startup> factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// Checks if for those endpoints we get a 200-209 status code.
        /// </summary>
        /// <param name="url">The url to check against.</param>
        /// <returns>A <see cref="Task"/>.</returns>
        [Theory]
        [InlineData("/")]
        [InlineData("/Index")]
        [InlineData("/Donation")]
        [InlineData("/Payment")]
        [InlineData("/Maintenance")]
        [InlineData("/Identity/Account/Register")]
        [InlineData("/Identity/Account/Login")]
        [InlineData("/Identity/Account/Login?donate=true")]
        [InlineData("/Identity/Account/ForgotPassword")]
        [InlineData("/Identity/Account/ResendEmailConfirmation")]
        public async Task Get_EndpointsReturnSuccessAndCorrectContentType(string url)
        {
            // Arrange
            var client = this.factory.CreateClient();

            // Act
            var response = await client.GetAsync(url);

            // Assert
            if (response.IsSuccessStatusCode)
            {
                response.EnsureSuccessStatusCode(); // Status Code 200-299
                Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType.ToString());
            }
            else
            {
                string body = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException(body);
            }
        }
    }
}
