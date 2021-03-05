[assembly: Microsoft.AspNetCore.Hosting.HostingStartup(typeof(BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.AdminHostingStartup))]

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin
{
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Hosting;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;

    public class AdminHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) =>
            {
                services.AddAuthorization(options =>
                {
                    options.AddPolicy("AdminArea", builder => builder.RequireRole("Admin", "Manager"));
                });
            });
        }
    }
}
