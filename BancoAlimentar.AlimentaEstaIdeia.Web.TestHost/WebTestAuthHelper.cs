// -----------------------------------------------------------------------
// <copyright file="WebTestAuthHelper.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.TestHost
{
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using AngleSharp.Html.Dom;
    using BancoAlimentar.AlimentaEstaIdeia.Testing.Common;
    using Microsoft.AspNetCore.Mvc.Testing;

    /// <summary>
    /// Signs in users through the Identity UI for integration tests.
    /// </summary>
    public static class WebTestAuthHelper
    {
        /// <summary>
        /// Creates an HTTP client and signs in with the given credentials.
        /// </summary>
        /// <param name="factory">Web application factory.</param>
        /// <param name="email">User email.</param>
        /// <param name="password">User password.</param>
        /// <param name="allowAutoRedirect">Whether to follow redirects.</param>
        /// <returns>Authenticated HTTP client.</returns>
        public static async Task<HttpClient> CreateAuthenticatedClientAsync(
            WebApplicationFactory<Program> factory,
            string email,
            string password,
            bool allowAutoRedirect = true)
        {
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = allowAutoRedirect,
            });
            await LoginAsync(client, email, password);
            return client;
        }

        /// <summary>
        /// Signs in through the login page form.
        /// </summary>
        /// <param name="client">HTTP client.</param>
        /// <param name="email">User email.</param>
        /// <param name="password">User password.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task LoginAsync(HttpClient client, string email, string password)
        {
            var loginPage = await client.GetAsync("/Identity/Account/Login");
            loginPage.EnsureSuccessStatusCode();
            var content = await HtmlHelpers.GetDocumentAsync(loginPage);
            var response = await client.SendAsync(
                (IHtmlFormElement)content.QuerySelector("form[id='account']"),
                (IHtmlButtonElement)content.QuerySelector("button[id='loginBtn']"),
                new Dictionary<string, string>
                {
                    ["Input.Email"] = email,
                    ["Input.Password"] = password,
                });
            response.EnsureSuccessStatusCode();
        }
    }
}
