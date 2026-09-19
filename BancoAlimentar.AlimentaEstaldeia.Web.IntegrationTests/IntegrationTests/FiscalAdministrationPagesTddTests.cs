// -----------------------------------------------------------------------
// <copyright file="FiscalAdministrationPagesTddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Xunit;

    /// <summary>
    /// Defines integration entrypoint contracts for fiscal administration pages.
    /// </summary>
    public sealed class FiscalAdministrationPagesTddTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="FiscalAdministrationPagesTddTests"/> class.
        /// </summary>
        /// <param name="factory">The shared integration host.</param>
        public FiscalAdministrationPagesTddTests(CustomWebApplicationFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// Fiscal configuration is an Admin-only surface. Anonymous callers cannot create an
        /// issuer profile or communicate/close a fiscal series.
        /// </summary>
        [Theory]
        [InlineData("/Admin/Fiscal/IssuerProfiles?handler=Create")]
        [InlineData("/Admin/Fiscal/Series?handler=Configure")]
        [InlineData("/Admin/Fiscal/Series?handler=Close")]
        [InlineData("/Admin/Fiscal/SaftExport?handler=Export")]
        public async Task FiscalAdministrationPosts_RedirectAnonymousCallersToLogin(string path)
        {
            HttpClient client = this.factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });

            HttpResponseMessage response = await client.PostAsync(path, new FormUrlEncodedContent(new Dictionary<string, string>()));

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }
    }
}
