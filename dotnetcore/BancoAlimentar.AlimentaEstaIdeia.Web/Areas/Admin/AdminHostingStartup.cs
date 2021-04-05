[assembly: Microsoft.AspNetCore.Hosting.HostingStartup(typeof(BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.AdminHostingStartup))]

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin
{
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Authorization;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Configuration;
    using Microsoft.EntityFrameworkCore;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Microsoft.AspNetCore.Identity;

    public class AdminHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) =>
            {

                services.AddAuthorization(options =>
                {
                    IAuthenticationSchemeProvider provider = Startup.DefaultAuthenticationSchemeProvider;
                    if (provider != null)
                    {
                        var authenticationScheme = (provider.GetAllSchemesAsync().Result).Select(p => p.Name).ToArray();

                        var policy = new AuthorizationPolicyBuilder(authenticationScheme)
                            .RequireAuthenticatedUser()
                            .RequireRole("Admin", "Manager")
                            .Build();

                        options.AddPolicy("AdminArea", policy);
                    }
                });
            });
        }
    }
}
