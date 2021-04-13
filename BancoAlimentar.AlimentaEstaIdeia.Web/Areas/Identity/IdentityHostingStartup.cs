using Microsoft.AspNetCore.Hosting;

[assembly: HostingStartup(typeof(BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.IdentityHostingStartup))]

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => {
                // services.AddDbContext<BancoAlimentarAlimentaEstaIdeiaWebContext>(options =>
                //    options.UseSqlServer(
                //        context.Configuration.GetConnectionString("DefaultConnection")));

                // services.AddDefaultIdentity<WebUser>(options => options.SignIn.RequireConfirmedAccount = true)
                //    .AddEntityFrameworkStores<BancoAlimentarAlimentaEstaIdeiaWebContext>();

                // services.AddIdentity<WebUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
                // .AddEntityFrameworkStores<BancoAlimentarAlimentaEstaIdeiaWebContext>()
                // .AddDefaultTokenProviders();
            });
        }
    }
}