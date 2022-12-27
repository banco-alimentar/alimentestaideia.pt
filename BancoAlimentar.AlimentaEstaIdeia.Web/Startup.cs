// -----------------------------------------------------------------------
// <copyright file="Startup.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web
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
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.ApplicationInsight;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Authentication;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Options;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.HostedServices;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Layout;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Middleware;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.StaticFileProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant.Naming;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Features;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Pages;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Pages.Tenants;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry.Api;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry.Filtering;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
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
    using Microsoft.AspNetCore.Http.Features;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.AspNetCore.Localization;
    using Microsoft.AspNetCore.Mvc.ApplicationModels;
    using Microsoft.AspNetCore.Mvc.Razor;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.AspNetCore.Routing.Matching;
    using Microsoft.AspNetCore.StaticFiles;
    using Microsoft.AspNetCore.StaticFiles.Infrastructure;
    using Microsoft.Data.Sqlite;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Diagnostics;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;
    using Microsoft.FeatureManagement;
    using Microsoft.FeatureManagement.FeatureFilters;
    using StackExchange.Profiling.Storage;

    /// <summary>
    /// Startup class.
    /// </summary>
    public class Startup
    {
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly string azureWebSiteOrigin = "azure";

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="webHostEnvironment">Web hosting environment properties.</param>
        public Startup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            Configuration = configuration;
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

            // SAS Configuration
#pragma warning disable ASP0000 // Do not call 'IServiceCollection.BuildServiceProvider' in 'ConfigureServices'
            var defaultEndpointSelector = services.BuildServiceProvider().GetRequiredService<EndpointSelector>();
#pragma warning restore ASP0000 // Do not call 'IServiceCollection.BuildServiceProvider' in 'ConfigureServices'
            services.Replace(new ServiceDescriptor(
                                typeof(EndpointSelector),
                                sp => new DoarTenantEndpointSelector(
                                        defaultEndpointSelector,
                                        sp.GetRequiredService<EndpointDataSource>()),
                                ServiceLifetime.Singleton));
            services.AddTransient<IKeyVaultConfigurationManager, KeyVaultConfigurationManager>();
            services.AddSingleton<INamingStrategy, DomainNamingStrategy>();
            services.AddSingleton<INamingStrategy, PathNamingStrategy>();
            services.AddSingleton<INamingStrategy, SubdomainNamingStrategy>();
            services.AddSingleton<ITenantProvider, TenantProvider>();
            services.AddTransient<ITenantLayout, TenantLayout>();
            services.AddSingleton<LocalDevelopmentOverride, LocalDevelopmentOverride>();
            services.AddScoped<IInfrastructureUnitOfWork, InfrastructureUnitOfWork>();
            services.AddScoped<ProductCatalogueRepository>();
            services.AddScoped<FoodBankRepository>();
            services.AddScoped<DonationItemRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IStaticFileMapper, StaticFileMapper>();
            ServiceDescriptor serviceDescriptorConfiguration = services
                .Where(p => p.ServiceType == typeof(IConfiguration))
                .FirstOrDefault();
            services.Remove(serviceDescriptorConfiguration);
            services.AddTransient<IConfiguration, TenantConfigurationRoot>(
                (serviceProvider) =>
                {
                    return new TenantConfigurationRoot(
                        Configuration,
                        serviceProvider.GetRequiredService<IHttpContextAccessor>());
                });

            services.AddAntiforgery();
            services.AddSingleton<IAppVersionService, AppVersionService>();
            services.AddScoped<DonationRepository>();
            services.AddFeatureManagement().AddFeatureFilter<TargetingFilter>();
            services.AddSingleton<ITargetingContextAccessor, TargetingContextAccessor>();
            services.AddSingleton<NifApiValidator, NifApiValidator>();
            services.AddScoped<EasyPayBuilder>();
            services.AddScoped<PayPalBuilder>();
            services.AddScoped<IMail, Mail>();
            services.AddCors(options =>
            {
                options.AddPolicy(
                    name: azureWebSiteOrigin,
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
            if (!string.IsNullOrEmpty(Configuration["Authentication:Google:ClientId"]) &&
               !string.IsNullOrEmpty(Configuration["Authentication:Google:ClientSecret"]))
            {
                authenticationBuilder.AddGoogle();
                services.AddScoped<IPostConfigureOptions<GoogleOptions>, GooglePostConfigureOptions>();
            }

            if (!string.IsNullOrEmpty(Configuration["Authentication:Facebook:AppId"]) &&
                !string.IsNullOrEmpty(Configuration["Authentication:Facebook:AppSecret"]))
            {
                authenticationBuilder.AddFacebook();
                services.AddScoped<IPostConfigureOptions<FacebookOptions>, FacebookPostConfigureOptions>();
            }

            if (!string.IsNullOrEmpty(Configuration["Authentication:Microsoft:ClientId"]) &&
                !string.IsNullOrEmpty(Configuration["Authentication:Microsoft:ClientSecret"]))
            {
                authenticationBuilder.AddMicrosoftAccount();
                services.AddScoped<IPostConfigureOptions<MicrosoftAccountOptions>, MicrosoftAccountPostConfigureOptions>();
            }

            if (!string.IsNullOrEmpty(Configuration["Authentication:Twitter:ConsumerAPIKey"]) &&
                !string.IsNullOrEmpty(Configuration["Authentication:Twitter:ConsumerSecret"]))
            {
                authenticationBuilder.AddTwitter();
                services.AddScoped<IPostConfigureOptions<TwitterOptions>, Sas.ConfigurationProvider.TenantConfiguration.Authentication.TwitterPostConfigureOptions>();
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

            if (!string.IsNullOrEmpty(Configuration["ConnectionStrings:Infrastructure"]) ||
                !this.webHostEnvironment.IsDevelopment())
            {
                services.AddDbContext<InfrastructureDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("Infrastructure"), b =>
                    {
                        b.EnableRetryOnFailure();
                        b.MigrationsAssembly("BancoAlimentar.AlimentaEstaIdeia.Sas.Model");
                        b.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    }));

                // services.AddDistributedSqlServerCache(options =>
                //    {
                //        options.ConnectionString = Configuration.GetConnectionString("Infrastructure");
                //        options.SchemaName = "dbo";
                //        options.TableName = "DoarCache";
                //    });
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
                    Configuration.GetSection(TenantDevelopmentOptions.Section).Bind(devlopmentOptions);
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

            services.AddControllersWithViews().AddNewtonsoftJson();
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

            // services.Configure<RazorPagesOptions>(options =>
            // {
            //    options.RootDirectory = "/Tenants/Localhost/Pages";
            // });
            services.Configure<RazorViewEngineOptions>(options =>
            {
                // options.PageViewLocationFormats.Clear();
                // options.PageViewLocationFormats.Insert(0, "/Themes/mytheme/Pages/{1}/{0}.cshtml");
                // options.ViewLocationExpanders.Add(new MultisiteViewLocationExpander());
            });

            services.AddDistributedMemoryCache();
            services.AddMemoryCache();
            services.AddSession();
            services.AddLocalization(options => options.ResourcesPath = "Resources");
            services.AddSingleton<ITempDataProvider, CookieTempDataProvider>();
            services.AddSingleton<IViewRenderService, ViewRenderService>();
            services.AddApplicationInsightsTelemetry();
            services.AddApplicationInsightsTelemetryProcessor<RemoveAzureStorageTelemetryFilter>();
            services.AddApplicationInsightsTelemetryProcessor<FileNotFoundAzureStroageBlobFilter>();
            services.AddApplicationInsightsTelemetryProcessor<WebApplicationStatusFilter>();

            // services.AddApplicationInsightsTelemetry(options =>
            //            {
            //                options.InstrumentationKey = Configuration["APPINSIGHTS_CONNECTIONSTRING"];
            // #if DEBUG
            //                options.EnableAppServicesHeartbeatTelemetryModule = false;
            //                options.EnableAzureInstanceMetadataTelemetryModule = false;
            // #else
            //                options.EnableAppServicesHeartbeatTelemetryModule = true;
            //                options.EnableAzureInstanceMetadataTelemetryModule = true;
            // #endif
            //                /*
            //                options.EnableQuickPulseMetricStream = false;
            //                options.EnablePerformanceCounterCollectionModule = false;
            //                options.EnableEventCounterCollectionModule = true;
            //                */
            //            });
            services.Remove(services.Where(p => p.ServiceType == typeof(IOptions<TelemetryConfiguration>)).First());
            services.Remove(services.Where(p => p.ServiceType == typeof(TelemetryConfiguration)).First());

            services.AddScoped<IOptions<TelemetryConfiguration>, SasTelemetryConfiguration>();
            services.AddScoped<TelemetryConfiguration>(serviceProvider =>
            {
                return serviceProvider.GetRequiredService<IOptions<TelemetryConfiguration>>().Value;
            });
            services.Remove(services.Where(p => p.ServiceType == typeof(TelemetryClient)).First());
            services.AddScoped(ApplicationInsightsPostConfigureOptions.ConfigureTelemetryClient);

            services.ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((module, o) =>
            {
                module.EnableRequestIdHeaderInjectionInW3CMode = true;
                module.SetComponentCorrelationHttpHeaders = true;
            });
            services.AddSingleton<ITelemetryInitializer, Ignore404ErrorsTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, HttpTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, DonationFlowTelemetryInitializer>();
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

                options.AddInitialRequestCultureProvider(new CustomRequestCultureProvider(context =>
                {
                    return Task.FromResult(new ProviderCultureResult("pt"));
                }));
            });
            services.AddMvc().AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix);
            services.AddMiniProfiler(options =>
            {
                // All of this is optional. You can simply call .AddMiniProfiler() for all defaults

                // (Optional) Path to use for profiler URLs, default is /mini-profiler-resources
                options.RouteBasePath = "/profiler";

                // (Optional) Control storage
                // (default is 30 minutes in MemoryCacheStorage)
                // Note: MiniProfiler will not work if a SizeLimit is set on MemoryCache!
                //   See: https://github.com/MiniProfiler/dotnet/issues/501 for details
                (options.Storage as MemoryCacheStorage).CacheDuration = TimeSpan.FromMinutes(60);

                // (Optional) Control which SQL formatter to use, InlineFormatter is the default
                options.SqlFormatter = new StackExchange.Profiling.SqlFormatters.InlineFormatter();
                options.EnableDebugMode = true;
            }).AddEntityFramework();
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

            services.AddHostedService<TenantStaticSyncHostedService>();

            var healthcheck = services.AddHealthChecks();
            AddHeathCheacks(healthcheck);
        }

        /// <summary>
        /// Configure the web application.
        /// </summary>
        /// <param name="app">A rerfence to the <see cref="IApplicationBuilder"/>.</param>
        /// <param name="env">A rerfence to the <see cref="IWebHostBuilder"/>.</param>
        /// <param name="configuration">Telemetry configuration.</param>
        /// <param name="httpContextAccessor">Http context accesor.</param>
        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env,
            TelemetryConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            if (env.IsDevelopment())
            {
                app.UseMiniProfiler();
                app.UseDeveloperExceptionPage();

                // app.UseBrowserLink(); //https://github.com/dotnet/aspnetcore/issues/37747
                app.UseMigrationsEndPoint();
            }
            else if (env.IsStaging())
            {
                app.UseMiniProfiler();
                app.UseExceptionHandler("/Error");
                app.UseHsts();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // Production environment.
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
            app.UseDoarMultitenancy();
            app.UseStaticFiles(new StaticFileOptions()
            {
                // HttpsCompression = HttpsCompressionMode.Compress,
                OnPrepareResponse = new Action<StaticFileResponseContext>((responseContent) =>
                {
                    var tenant = responseContent.Context.GetTenant();
                }),
                FileProvider = new TenantStaticFileProvider(
                    new PhysicalFileProvider(env.WebRootPath),
                    httpContextAccessor),
            });

            app.UseRouting();
            app.UseCors(azureWebSiteOrigin);
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/status");
            });
            app.UseDonationTelemetryMiddleware();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
            });
        }

        private void AddHeathCheacks(IHealthChecksBuilder healthcheck)
        {
            healthcheck.AddSqlServer(Configuration.GetConnectionString("DefaultConnection"));
            healthcheck.AddDbContextCheck<ApplicationDbContext>();
        }
    }
}
