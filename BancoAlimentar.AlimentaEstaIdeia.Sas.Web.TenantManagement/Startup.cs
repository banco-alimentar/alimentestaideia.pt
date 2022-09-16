// -----------------------------------------------------------------------
// <copyright file="Startup.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Web.TenantManagement
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Autofac;
    using Azure.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Common.Services;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.Validation;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Authentication;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Options;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Middleware;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.StaticFileProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Repository;
    using Microsoft.ApplicationInsights.DependencyCollector;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.Facebook;
    using Microsoft.AspNetCore.Authentication.Google;
    using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
    using Microsoft.AspNetCore.Authentication.Twitter;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Localization;
    using Microsoft.AspNetCore.Mvc.Razor;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    using Microsoft.AspNetCore.StaticFiles;
    using Microsoft.Data.Sqlite;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Startup class.
    /// </summary>
    public class Startup
    {
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly string azureWebSiteOrigin = "azure";

        static Startup()
        {
            ServiceCollection = new ServiceCollection();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="webHostEnvironment">Web hosting environment properties.</param>
        public Startup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            this.Configuration = configuration;
            this.webHostEnvironment = webHostEnvironment;
        }

        /// <summary>
        /// Gets the service collection.
        /// </summary>
        public static IServiceCollection ServiceCollection { get; private set; }

        /// <summary>
        /// Gets the current configuration.
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// Configure the services in the asp.net core application.
        /// </summary>
        /// <param name="services">A reference to the <see cref="IServiceCollection"/>.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            // if (!string.IsNullOrEmpty(Configuration["AppConfig"]))
            // {
            //    services.AddAzureAppConfiguration();
            // }
            ServiceCollection = services;
            services.AddSingleton(services);

            services.AddScoped<IInfrastructureUnitOfWork, InfrastructureUnitOfWork>();
            services.AddScoped<ProductCatalogueRepository>();
            services.AddScoped<FoodBankRepository>();
            services.AddScoped<DonationItemRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddAntiforgery();
            services.AddSingleton<IAppVersionService, AppVersionService>();
            services.AddScoped<DonationRepository>();
            services.AddSingleton<NifApiValidator, NifApiValidator>();
            services.AddCors(options =>
            {
                options.AddPolicy(
                    name: this.azureWebSiteOrigin,
                    builder =>
                    {
                        builder.WithOrigins(
                            "https://alimentestaideia.pt/",
                            "https://www.alimentestaideia.pt/")
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                    });
            });

            AuthenticationBuilder authenticationBuilder = services.AddAuthentication();
            if (!string.IsNullOrEmpty(this.Configuration["Authentication:Google:ClientId"]) &&
                !string.IsNullOrEmpty(this.Configuration["Authentication:Google:ClientSecret"]))
            {
                authenticationBuilder.AddGoogle(options =>
                {
                    IConfigurationSection googleAuthNSection =
                        this.Configuration.GetSection("Authentication:Google");
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

            if (!string.IsNullOrEmpty(this.Configuration["Authentication:Facebook:AppId"]) &&
                !string.IsNullOrEmpty(this.Configuration["Authentication:Facebook:AppSecret"]))
            {
                authenticationBuilder.AddFacebook(facebookOptions =>
                {
                    facebookOptions.AppId = this.Configuration["Authentication:Facebook:AppId"];
                    facebookOptions.AppSecret = this.Configuration["Authentication:Facebook:AppSecret"];
                });
            }

            if (!string.IsNullOrEmpty(this.Configuration["Authentication:Microsoft:ClientId"]) &&
                !string.IsNullOrEmpty(this.Configuration["Authentication:Microsoft:ClientSecret"]))
            {
                authenticationBuilder.AddMicrosoftAccount(microsoftOptions =>
                {
                    microsoftOptions.ClientId = this.Configuration["Authentication:Microsoft:ClientId"];
                    microsoftOptions.ClientSecret = this.Configuration["Authentication:Microsoft:ClientSecret"];
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

            services.AddDbContextFactory<ApplicationDbContext>((serviceProvider, options) =>
            {
                IConfiguration config = serviceProvider.GetRequiredService<IConfiguration>();
                options.UseSqlServer(
                      config["ConnectionStrings:DefaultConnection"], b =>
                      {
                          b.EnableRetryOnFailure();
                          b.MigrationsAssembly("BancoAlimentar.AlimentaEstaIdeia.Web");
                          b.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                      });
            });

            if (!string.IsNullOrEmpty(this.Configuration["ConnectionStrings:Infrastructure"]) ||
                !this.webHostEnvironment.IsDevelopment())
            {
                services.AddDbContext<InfrastructureDbContext>(options =>
                options.UseSqlServer(
                    this.Configuration.GetConnectionString("Infrastructure"), b =>
                    {
                        b.EnableRetryOnFailure();
                        b.MigrationsAssembly("BancoAlimentar.AlimentaEstaIdeia.Sas.Model");
                        b.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    }));
            }
            else
            {
                services.AddScoped<InfrastructureDbContext, InfrastructureDbContext>((serviceProvider) =>
                {
                    DbContextOptionsBuilder<InfrastructureDbContext> options = new DbContextOptionsBuilder<InfrastructureDbContext>();
                    SqliteConnection connection = new SqliteConnection("Filename=:memory:");
                    connection.Open();
                    options.UseSqlite(connection);
                    InfrastructureDbContext infrastructureDbContext = new InfrastructureDbContext(options.Options);
                    TenantDevelopmentOptions devlopmentOptions = new TenantDevelopmentOptions();
                    this.Configuration.GetSection(TenantDevelopmentOptions.Section).Bind(devlopmentOptions);
                    infrastructureDbContext.Database.EnsureCreated();
                    infrastructureDbContext.Tenants.Add(new Tenant()
                    {
                        Name = devlopmentOptions.Name,
                        Created = DateTime.UtcNow,
                        Domains = new List<DomainIdentifier>()
                        {
                            new DomainIdentifier()
                            {
                                Created = DateTime.UtcNow,
                                DomainName = devlopmentOptions.DomainIdentifier,
                                Environment = "localhost",
                            },
                        },
                        InvoicingStrategy = Enum.Parse<InvoicingStrategy>(devlopmentOptions.InvoicingStrategy),
                        PaymentStrategy = Enum.Parse<PaymentStrategy>(devlopmentOptions.PaymentStrategy),
                        PublicId = Guid.NewGuid(),
                    });
                    infrastructureDbContext.SaveChanges();
                    return infrastructureDbContext;
                });
            }

            services.AddDbContext<ApplicationDbContext>(options =>
                 options.UseSqlServer(
                     this.Configuration.GetConnectionString("DefaultConnection"), b =>
                     {
                         b.EnableRetryOnFailure();
                         b.MigrationsAssembly("BancoAlimentar.AlimentaEstaIdeia.Web");
                         b.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                     }));
            services.AddControllersWithViews().AddNewtonsoftJson();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddDatabaseDeveloperPageExceptionFilter();
            services.AddDefaultIdentity<IdentityUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
                options.User.RequireUniqueEmail = true;
            })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<InfrastructureDbContext>()
                .AddDefaultTokenProviders();
            services.AddRazorPages(options =>
            {
                options.Conventions.AuthorizeAreaFolder("Admin", "/", "AdminArea");
                options.Conventions.AuthorizeAreaFolder("RoleManagement", "/", "RoleArea");
            }).AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix).AddDataAnnotationsLocalization();
            services.AddDistributedMemoryCache();
            services.AddMemoryCache();
            services.AddSession();
            services.AddLocalization(options => options.ResourcesPath = "Resources");
            services.AddSingleton<ITempDataProvider, CookieTempDataProvider>();
            services.AddLocalization(options => options.ResourcesPath = "Resources");
            services.AddSingleton<ITempDataProvider, CookieTempDataProvider>();
            services.AddApplicationInsightsTelemetry(options =>
            {
                options.ConnectionString = this.Configuration["APPINSIGHTS_CONNECTIONSTRING"];
            });
            services.ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((module, o) =>
            {
                module.EnableRequestIdHeaderInjectionInW3CMode = true;
                module.SetComponentCorrelationHttpHeaders = true;
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

                options.AddInitialRequestCultureProvider(new CustomRequestCultureProvider(
                    context => Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult("pt"))));
            });
            services.AddMvc().AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix);

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
                    .PersistKeysToAzureBlobStorage(this.Configuration["AzureStorage:ConnectionString"], "dataprotection", "dataprotectionweb")
                    .ProtectKeysWithAzureKeyVault(new Uri(this.Configuration["DataProtectionKeyVaultKey"]), new ManagedIdentityCredential());
            }

            services.AddAuthorization(options =>
            {
                // var authenticationScheme = provider.GetAllSchemesAsync().Result.Select(p => p.Name).ToArray();
                string[] authenticationScheme = new string[]
                {
                        "Identity.Application",
                        "Identity.External",

                        // "Identity.TwoFactorRememeberMe",
                        "Identity.TwoFactorUserId",
                        "Google",
                        "Microsoft",
                };

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
            });
        }

        /// <summary>
        /// Configure the web application.
        /// </summary>
        /// <param name="app">A rerfence to the <see cref="IApplicationBuilder"/>.</param>
        /// <param name="env">A rerfence to the <see cref="IWebHostBuilder"/>.</param>
        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();

                app.UseDeveloperExceptionPage();
            }

            app.UseStatusCodePages();
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
            app.UseCors(this.azureWebSiteOrigin);
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
            });
        }
    }
}
