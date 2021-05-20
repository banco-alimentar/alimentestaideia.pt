// -----------------------------------------------------------------------
// <copyright file="Startup.cs" company="Federa��o Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federa��o Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;
    using Azure.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Pages;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry;
    using DNTCaptcha.Core;
    using Microsoft.ApplicationInsights.DependencyCollector;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.AspNetCore.Localization;
    using Microsoft.AspNetCore.Mvc.Razor;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    public class Startup
    {
        private readonly IWebHostEnvironment webHostEnvironment;

        public Startup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            Configuration = configuration;
            this.webHostEnvironment = webHostEnvironment;
        }

        public static IAuthenticationSchemeProvider DefaultAuthenticationSchemeProvider { get; set; }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<DonationRepository>();
            services.AddScoped<ProductCatalogueRepository>();
            services.AddScoped<FoodBankRepository>();
            services.AddScoped<DonationItemRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection"), b => b.MigrationsAssembly("BancoAlimentar.AlimentaEstaIdeia.Web")));
            services.AddControllersWithViews()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.PropertyNamingPolicy = null;
                });
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddDatabaseDeveloperPageExceptionFilter();
            services.AddDefaultIdentity<WebUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
                options.User.RequireUniqueEmail = true;
            })
                .AddRoles<ApplicationRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();
            services.AddTransient<IEmailSender, EmailSender>();
            services.AddRazorPages(options =>
            {
                options.Conventions.AuthorizeAreaFolder("Admin", "/", "AdminArea");
                options.Conventions.AuthorizeAreaFolder("RoleManagement", "/", "RoleArea");
            }).AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix).AddDataAnnotationsLocalization();
            services.AddDistributedMemoryCache();
            services.AddMemoryCache();
            services.AddSession();
            AuthenticationBuilder authenticationBuilder = services.AddAuthentication();
            if (!string.IsNullOrEmpty(Configuration["Authentication:Google:ClientId"]) &&
                !string.IsNullOrEmpty(Configuration["Authentication:Google:ClientSecret"]))
            {
                authenticationBuilder.AddGoogle(options =>
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
                            Value = DateTime.UtcNow.ToString(),
                        });

                        ctx.Properties.StoreTokens(tokens);

                        return Task.CompletedTask;
                    };
                });
            }

            if (!string.IsNullOrEmpty(Configuration["Authentication:Facebook:AppId"]) &&
                !string.IsNullOrEmpty(Configuration["Authentication:Facebook:AppSecret"]))
            {
                authenticationBuilder.AddFacebook(facebookOptions =>
                {
                    facebookOptions.AppId = Configuration["Authentication:Facebook:AppId"];
                    facebookOptions.AppSecret = Configuration["Authentication:Facebook:AppSecret"];
                });
            }

            if (!string.IsNullOrEmpty(Configuration["Authentication:Microsoft:ClientId"]) &&
                !string.IsNullOrEmpty(Configuration["Authentication:Microsoft:ClientSecret"]))
            {
                authenticationBuilder.AddMicrosoftAccount(microsoftOptions =>
                {
                    microsoftOptions.ClientId = Configuration["Authentication:Microsoft:ClientId"];
                    microsoftOptions.ClientSecret = Configuration["Authentication:Microsoft:ClientSecret"];
                    microsoftOptions.SaveTokens = true;
                    microsoftOptions.Scope.Add("email");
                    microsoftOptions.Scope.Add("openid");
                    microsoftOptions.Scope.Add("profile");
                    microsoftOptions.Scope.Add("User.ReadBasic.All");
                    microsoftOptions.Events.OnCreatingTicket = ctx =>
                    {
                        List<AuthenticationToken> tokens = ctx.Properties.GetTokens().ToList();

                        tokens.Add(new AuthenticationToken()
                        {
                            Name = "TicketCreated",
                            Value = DateTime.UtcNow.ToString(),
                        });

                        ctx.Properties.StoreTokens(tokens);

                        return Task.CompletedTask;
                    };
                });
            }

            if (!string.IsNullOrEmpty(Configuration["Authentication:Twitter:ConsumerAPIKey"]) &&
                !string.IsNullOrEmpty(Configuration["Authentication:Twitter:ConsumerSecret"]))
            {
                authenticationBuilder.AddTwitter(twitterOptions =>
                {
                    twitterOptions.ConsumerKey = Configuration["Authentication:Twitter:ConsumerAPIKey"];
                    twitterOptions.ConsumerSecret = Configuration["Authentication:Twitter:ConsumerSecret"];
                    twitterOptions.RetrieveUserDetails = true;
                });
            }

            services.AddLocalization(options => options.ResourcesPath = "Resources");
            services.AddSingleton<ITempDataProvider, CookieTempDataProvider>();
            services.AddSingleton<IViewRenderService, ViewRenderService>();
            services.AddApplicationInsightsTelemetry(Configuration["APPINSIGHTS_CONNECTIONSTRING"]);
            services.ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((module, o) =>
            {
                module.EnableSqlCommandTextInstrumentation = true;
                module.EnableRequestIdHeaderInjectionInW3CMode = true;
                module.SetComponentCorrelationHttpHeaders = true;
            });
            services.AddSingleton<ITelemetryInitializer, Ignore404ErrorsTelemetryInitializer>();

            services.AddSingleton<ITelemetryInitializer, UserAuthenticationTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, DonationFlowTelemetryInitializer>();
            services.AddDNTCaptcha(options =>
            {
                options.UseCookieStorageProvider()
                    .AbsoluteExpiration(minutes: 7)
                    .ShowThousandsSeparators(false)
                    .WithEncryptionKey("myawesomekey2021and2020thatisanewyear!");
            });
            services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[]
                {
                    new CultureInfo("pt"),
                    new CultureInfo("fr"),
                    new CultureInfo("en"),
                    new CultureInfo("es"),
                };

                options.DefaultRequestCulture = new RequestCulture(culture: "pt", uiCulture: "pt");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;

                options.AddInitialRequestCultureProvider(new CustomRequestCultureProvider(async context =>
                {
                    return new ProviderCultureResult("pt");
                }));
            });
            services.AddMvc().AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix);
            if (this.webHostEnvironment.IsDevelopment())
            {
            }

            if (this.webHostEnvironment.IsProduction())
            {
                services.AddHsts(options =>
                {
                    options.Preload = true;
                    options.IncludeSubDomains = true;
                    options.MaxAge = TimeSpan.FromDays(60);
                    options.ExcludedHosts.Add("alimentaestaideia-developer.azurewebsites.net");
                    options.ExcludedHosts.Add("alimentaestaideia-preprod.azurewebsites.net");
                });

                services.AddHttpsRedirection(options =>
                {
                    options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
                    options.HttpsPort = 5001;
                });

                services.AddDataProtection()
                    .PersistKeysToAzureBlobStorage(Configuration["AzureStorage:ConnectionString"], "dataprotection", "dataprotectionweb")
                    .ProtectKeysWithAzureKeyVault(new Uri(Configuration["DataProtectionKeyVaultKey"]), new ManagedIdentityCredential());
            }

            services.AddAuthorization(options =>
            {
                var sp = services.BuildServiceProvider();
                var provider = sp.GetRequiredService<IAuthenticationSchemeProvider>();
                if (provider != null)
                {
                    var authenticationScheme = provider.GetAllSchemesAsync().Result.Select(p => p.Name).ToArray();

                    var policy = new AuthorizationPolicyBuilder(authenticationScheme)
                        .RequireAuthenticatedUser()
                        .RequireRole("Admin", "Manager")
                        .Build();

                    options.AddPolicy("AdminArea", policy);

                    policy = new AuthorizationPolicyBuilder(authenticationScheme)
                        .RequireAuthenticatedUser()
                        .RequireRole("SuperAdmin")
                        .Build();

                    options.AddPolicy("RoleArea", policy);
                }
            });
            var healthcheck = services.AddHealthChecks();
            AddHeathCheacks(healthcheck);
        }

        private void AddHeathCheacks(IHealthChecksBuilder healthcheck)
        {
            healthcheck.AddSqlServer(Configuration.GetConnectionString("DefaultConnection"));
            healthcheck.AddDbContextCheck<ApplicationDbContext>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseDeveloperExceptionPage();
                app.UseHsts();
            }

            app.UseSession();

            var supportedCultures = new[] { "en" };
            var supportedUICultures = new[] { "pt", "fr", "en", "es" };
            var localizationOptions = new RequestLocalizationOptions().SetDefaultCulture(supportedCultures[0])
                .AddSupportedCultures(supportedCultures)
                .AddSupportedUICultures(supportedUICultures);

            app.UseRequestLocalization(localizationOptions);

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            });
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseDonationTelemetryMiddleware();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Donation}/{action=Index}/{id?}");
            });
        }
    }
}