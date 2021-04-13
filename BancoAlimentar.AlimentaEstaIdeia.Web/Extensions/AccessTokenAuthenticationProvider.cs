namespace BancoAlimentar.AlimentaEstaIdeia.Web.Extensions
{
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Microsoft.Graph;

    public class AccessTokenAuthenticationProvider : IAuthenticationProvider
    {
        private readonly string accessToken;

        public AccessTokenAuthenticationProvider(string accessToken)
        {
            this.accessToken = accessToken;
        }

        public Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("bearer", this.accessToken);
            return Task.CompletedTask;
        }
    }
}
