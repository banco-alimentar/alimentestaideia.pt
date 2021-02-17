using System;
using BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Data;
using BancoAlimentar.AlimentaEstaIdeia.Web.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: HostingStartup(typeof(BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.IdentityHostingStartup))]
namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => {
                //services.AddDbContext<BancoAlimentarAlimentaEstaIdeiaWebContext>(options =>
                //    options.UseSqlServer(
                //        context.Configuration.GetConnectionString("DefaultConnection")));

                //services.AddDefaultIdentity<BancoAlimentarAlimentaEstaIdeiaWebUser>(options => options.SignIn.RequireConfirmedAccount = true)
                //    .AddEntityFrameworkStores<BancoAlimentarAlimentaEstaIdeiaWebContext>();

                //services.AddIdentity<BancoAlimentarAlimentaEstaIdeiaWebUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
                //.AddEntityFrameworkStores<BancoAlimentarAlimentaEstaIdeiaWebContext>()
                //.AddDefaultTokenProviders();
            });
        }
    }
}