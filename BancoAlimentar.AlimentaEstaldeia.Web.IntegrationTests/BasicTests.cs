// -----------------------------------------------------------------------
// <copyright file="BasicTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.IntegrationTests
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Class for basic 200 HTTP status code tests.
    /// </summary>
    public class BasicTests
        : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly HttpClient client;
        private readonly WebApplicationFactory<Startup> factory;
        private readonly ITestOutputHelper outputHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="BasicTests"/> class.
        /// </summary>
        /// <param name="factory">Factory class.</param>
        /// <param name="outputHelper">Test output helper.</param>
        public BasicTests(WebApplicationFactory<Startup> factory, ITestOutputHelper outputHelper)
        {
            this.factory = factory;
            this.outputHelper = outputHelper;
            this.client = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var serviceProvider = services.BuildServiceProvider();
                });
            })
            .CreateClient();
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
                this.outputHelper.WriteLine("Body");
                this.outputHelper.WriteLine(body);
                this.outputHelper.WriteLine("EndBody");
                this.outputHelper.WriteLine($"RequestUri {string.Concat(client.BaseAddress, url)}");
                this.outputHelper.WriteLine($"Statuscode {response.StatusCode}");
                this.outputHelper.WriteLine($"ReasonPhrase {response.ReasonPhrase}");
                foreach (var item in response.Headers)
                {
                    this.outputHelper.WriteLine($"Header Name: {item.Key} | Value: {string.Join(',', item.Value)}");
                }

                throw new InvalidOperationException(body);
            }
        }
    }
}
