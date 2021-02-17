using BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Data;
using BancoAlimentar.AlimentaEstaIdeia.Web.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BancoAlimentar.AlimentaEstaIdeia.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection")));
            
            services.AddDatabaseDeveloperPageExceptionFilter();
            services.AddDefaultIdentity<BancoAlimentarAlimentaEstaIdeiaWebUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<ApplicationDbContext>();
            services.AddRazorPages();

            services.AddAuthentication()
               .AddGoogle(options =>
               {
                   IConfigurationSection googleAuthNSection =
                       Configuration.GetSection("Authentication:Google");
                   options.ClientId = googleAuthNSection["ClientId"];
                   options.ClientSecret = googleAuthNSection["ClientSecret"];
                   options.ClaimActions.MapJsonKey("urn:google:picture", "picture", "url");
                   options.ClaimActions.MapJsonKey("urn:google:locale", "locale", "string");
                   options.SaveTokens = true;

                   options.Events.OnCreatingTicket = ctx =>
                   {
                       List<AuthenticationToken> tokens = ctx.Properties.GetTokens().ToList();

                       tokens.Add(new AuthenticationToken()
                       {
                           Name = "TicketCreated",
                           Value = DateTime.UtcNow.ToString()
                       });

                       ctx.Properties.StoreTokens(tokens);

                       return Task.CompletedTask;
                   };

               })
               //.AddFacebook(facebookOptions =>
               //{
               //    facebookOptions.AppId = Configuration["Authentication:Facebook:AppId"];
               //    facebookOptions.AppSecret = Configuration["Authentication:Facebook:AppSecret"];
               //})
                .AddMicrosoftAccount(microsoftOptions =>
                   {
                    microsoftOptions.ClientId = Configuration["Authentication:Microsoft:ClientId"];
                    microsoftOptions.ClientSecret = Configuration["Authentication:Microsoft:ClientSecret"];
                })
               //.AddTwitter(twitterOptions =>
               //{
               //    twitterOptions.ConsumerKey = Configuration["Authentication:Twitter:ConsumerAPIKey"];
               //    twitterOptions.ConsumerSecret = Configuration["Authentication:Twitter:ConsumerSecret"];
               //    twitterOptions.RetrieveUserDetails = true;
               //})
               ;
            services.AddApplicationInsightsTelemetry(Configuration["APPINSIGHTS_CONNECTIONSTRING"]);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}
