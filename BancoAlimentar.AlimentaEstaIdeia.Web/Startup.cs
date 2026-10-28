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
    using System.Threading.RateLimiting;
    using System.Threading.Tasks;
    using Azure.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Common.Services;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.Reporting;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.SiteHealth;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.Validation;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.ApplicationInsight;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Authentication;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Options;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.HostedServices;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Layout;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Middleware;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Services;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.StaticFileProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant.Naming;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.FunctionExecutionReports;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Features;
    using BancoAlimentar.AlimentaEstaIdeia.Web.JsonConverter;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Pages;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Pages.Tenants;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services.EasyPay;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services.FunctionExecutionReports;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services.Invoices;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry.Api;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry.Filtering;
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.Cookies;
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
    using Microsoft.AspNetCore.Mvc.Authorization;
    using Microsoft.AspNetCore.Mvc.Razor;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    using Microsoft.AspNetCore.RateLimiting;
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
    using Newtonsoft.Json;
    using NuGet.Packaging.Signing;
    using OpenTelemetry.Trace;
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
            services.AddWebTenancy(this.Configuration);
            services.AddApplicationPersistence(this.Configuration, this.webHostEnvironment);

            services.AddScoped<FunctionExecutionReportOptions>(serviceProvider =>
                FunctionExecutionReportConfiguration.Create(serviceProvider.GetRequiredService<IConfiguration>()));
            services.AddScoped<IFunctionExecutionReportReader, ConfiguredFunctionExecutionReportReader>();
            services.AddScoped<FunctionExecutionReportQueryService>();
            services.AddScoped<FunctionExecutionCommandsOptions>(serviceProvider =>
            {
                var options = new FunctionExecutionCommandsOptions();
                serviceProvider.GetRequiredService<IConfiguration>()
                    .GetSection(FunctionExecutionCommandsOptions.SectionName)
                    .Bind(options);
                return options;
            });
            services.AddScoped<IFunctionExecutionTriggerClient, AzureStorageQueueFunctionExecutionTriggerClient>();

            services.AddAntiforgery();
            services.AddSingleton<IAppVersionService, AppVersionService>();
            services.AddScoped<IDonationReportGenerationService, DonationReportGenerationService>();
            services.AddSingleton<DonationReportGenerationState>();
            services.AddSingleton<SiteHealthReportGenerationState>();
            services.AddSingleton<SiteHealthReportBlobStore>();
            services.AddScoped<ISiteHealthLogAnalyticsClient, SiteHealthLogAnalyticsClient>();
            services.AddScoped<ISiteHealthReportService, SiteHealthReportService>();
            services.AddFeatureManagement().AddFeatureFilter<TargetingFilter>();
            services.AddSingleton<ITargetingContextAccessor, TargetingContextAccessor>();
            services.AddSingleton<NifApiValidator, NifApiValidator>();
            services.AddPaymentServices(this.Configuration);
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

            services.AddWebIdentity(this.Configuration);

            services.AddDistributedMemoryCache();
            services.AddMemoryCache();
            services.AddSession();
            services.AddLocalization(options => options.ResourcesPath = "Resources");
            services.AddSingleton<ITempDataProvider, CookieTempDataProvider>();
            services.AddSingleton<IViewRenderService, ViewRenderService>();
            services.AddApplicationTelemetry(this.Configuration);
            ConfigureLocalization(services);
            services.AddMvc().AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix);
            this.ConfigureEnvironmentServices(services);
            services.AddTenantStaticFileSynchronization();

            this.ConfigureHealthChecks(services);
        }

        /// <summary>
        /// Configure the web application.
        /// </summary>
        /// <param name="app">A reference to the <see cref="IApplicationBuilder"/>.</param>
        /// <param name="env">A reference to the <see cref="IWebHostEnvironment"/>.</param>
        /// <param name="configuration">Telemetry configuration.</param>
        /// <param name="httpContextAccessor">HTTP context accessor.</param>
        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env,
            Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            ConfigureExceptionHandling(app, env);
            app.UseStatusCodePages();
            app.UseSession();
            RequestLocalizationOptions localizationOptions = app.ApplicationServices
                .GetRequiredService<IOptions<RequestLocalizationOptions>>()
                .Value;
            app.UseRequestLocalization(localizationOptions);
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            });

            app.Use(HandleSlotSwapWarmupAsync);
            app.UseHttpsRedirection();
            app.Use(BlockSuspiciousRequestsAsync);
            app.Use(RewriteReportPathsAsync);
            app.UseDoarMultitenancy();
            ConfigureStaticFiles(app, env, httpContextAccessor);
            app.UseRouting();
            app.UseCors(azureWebSiteOrigin);
            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => endpoints.MapHealthChecks("/status"));
            app.UseDonationTelemetryMiddleware();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
            });
        }

        private void ConfigureLocalization(IServiceCollection services)
        {
            services.Configure<RequestLocalizationOptions>(options =>
            {
                CultureInfo[] supportedCultures =
                {
                    new CultureInfo("pt"),
                    new CultureInfo("fr"),
                    new CultureInfo("en"),
                    new CultureInfo("es"),
                };

                options.DefaultRequestCulture = new RequestCulture(culture: "pt", uiCulture: "pt");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
            });
        }

        private void ConfigureEnvironmentServices(IServiceCollection services)
        {
            if (this.webHostEnvironment.IsDevelopment())
            {
                services.AddMiniProfiler(options =>
                {
                    options.RouteBasePath = "/profiler";
                    ((MemoryCacheStorage)options.Storage).CacheDuration = TimeSpan.FromMinutes(60);
                    options.SqlFormatter = new StackExchange.Profiling.SqlFormatters.InlineFormatter();
                    options.EnableDebugMode = true;
                }).AddEntityFramework();
            }

            if (!this.webHostEnvironment.IsProduction())
            {
                return;
            }

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
                .PersistKeysToAzureBlobStorage(
                    this.Configuration["AzureStorage:ConnectionString"],
                    "dataprotection",
                    "dataprotectionweb")
                .ProtectKeysWithAzureKeyVault(
                    new Uri(this.Configuration["DataProtectionKeyVaultKey"]),
                    new ManagedIdentityCredential(new ManagedIdentityCredentialOptions()));
        }

        private void ConfigureHealthChecks(IServiceCollection services)
        {
            IHealthChecksBuilder healthChecks = services.AddHealthChecks();
            healthChecks.AddSqlServer(this.Configuration.GetConnectionString("DefaultConnection"));
            healthChecks.AddDbContextCheck<ApplicationDbContext>();
        }

        private async Task BlockSuspiciousRequestsAsync(HttpContext context, Func<Task> next)
        {
            string path = context.Request.Path.Value ?? string.Empty;
            bool isPhpRequest = path.Contains(".php", StringComparison.OrdinalIgnoreCase);
            bool isGitRequest = path.StartsWith("/.git", StringComparison.OrdinalIgnoreCase);
            bool isSwaggerRequest = path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase);

            if (isPhpRequest || isGitRequest || isSwaggerRequest)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            await next();
        }

        private void ConfigureExceptionHandling(IApplicationBuilder app, IWebHostEnvironment environment)
        {
            if (environment.IsDevelopment())
            {
                app.UseMiniProfiler();
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
                return;
            }

            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        private void ConfigureStaticFiles(
            IApplicationBuilder app,
            IWebHostEnvironment environment,
            IHttpContextAccessor httpContextAccessor)
        {
            TenantStaticFileProvider tenantStaticFileProvider = new TenantStaticFileProvider(
                new PhysicalFileProvider(environment.WebRootPath),
                httpContextAccessor);
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = tenantStaticFileProvider,
                OnPrepareResponse = responseContext =>
                {
                    _ = responseContext.Context.GetTenant();
                },
            });
        }

        private async Task HandleSlotSwapWarmupAsync(HttpContext context, Func<Task> next)
        {
            string? path = context.Request.Path.Value;
            if (path != null && path.Equals("/health/swap", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "text/plain; charset=utf-8";
                await context.Response.WriteAsync("OK");
                return;
            }

            await next();
        }

        private async Task RewriteReportPathsAsync(HttpContext context, Func<Task> next)
        {
            string? path = context.Request.Path.Value;
            if (path != null)
            {
                if (path.Equals("/reports", StringComparison.OrdinalIgnoreCase)
                    || path.Equals("/reports/", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.Redirect(DonationReportPaths.PublicPath + "user-logins.html", permanent: true);
                    return;
                }

                string reportPath = DonationReportPaths.PublicPath.TrimEnd('/');
                if (path.Equals(reportPath, StringComparison.OrdinalIgnoreCase)
                    || path.Equals(reportPath + "/", StringComparison.OrdinalIgnoreCase))
                {
                    context.Request.Path = reportPath + "/index.html";
                }
            }

            await next();
        }
    }
}
