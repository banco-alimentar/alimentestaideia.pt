// -----------------------------------------------------------------------
// <copyright file="AccessTokenAuthenticationProvider.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Extensions
{
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Microsoft.Graph;

    /// <summary>
    /// Access token authentication provider.
    /// </summary>
    public class AccessTokenAuthenticationProvider : IAuthenticationProvider
    {
        private readonly string accessToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="AccessTokenAuthenticationProvider"/> class.
        /// </summary>
        /// <param name="accessToken">Access token.</param>
        public AccessTokenAuthenticationProvider(string accessToken)
        {
            this.accessToken = accessToken;
        }

        /// <summary>
        /// Adds the access token to the Authorization header value.
        /// </summary>
        /// <param name="request">Request message.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("bearer", this.accessToken);
            return Task.CompletedTask;
        }
    }
}
