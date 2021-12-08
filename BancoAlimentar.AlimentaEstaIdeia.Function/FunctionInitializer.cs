namespace BancoAlimentar.AlimentaEstaIdeia.Function
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Pages;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc.Razor;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Diagnostics;
    using System.Reflection;

    internal class FunctionInitializer
    {
        /// <summary>
        /// Create the unit of work.
        /// </summary>
        /// <param name="telemetryClient">Telemetry client.</param>
        /// <returns></returns>
        public static (IUnitOfWork UnitOfWork, ApplicationDbContext ApplicationDbContext, IConfiguration configuration) GetUnitOfWork(
            TelemetryClient telemetryClient)
        {
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddUserSecrets(Assembly.GetExecutingAssembly(), true)
                .AddJsonFile("appsettings.json",
                                optional: true,
                                reloadOnChange: true);

            IConfiguration configuration = configurationBuilder
                .Build();
            configurationBuilder.AddAzureKeyVault(configuration["VaultUri"]);
            configuration = configurationBuilder.Build();

            DbContextOptionsBuilder<ApplicationDbContext> builder = new();
            builder.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"), b => b.MigrationsAssembly("BancoAlimentar.AlimentaEstaIdeia.Web"));
            ApplicationDbContext context = new ApplicationDbContext(builder.Options);
            IUnitOfWork unitOfWork = new UnitOfWork(context, telemetryClient, null);
            return (unitOfWork, context, configuration);
        }

        public static ServiceProvider GetServiceCollection(IConfiguration configuration, TelemetryClient telemetryClient)
        {
            ServiceCollection services = new ServiceCollection();

            services.AddScoped<DonationRepository>();
            services.AddMemoryCache();
            services.AddScoped<ProductCatalogueRepository>();
            services.AddScoped<FoodBankRepository>();
            services.AddScoped<DonationItemRepository>();
            services.AddScoped<InvoiceRepository>();
            services.AddScoped<EasyPayBuilder>();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddSingleton<IMemoryCache, MemoryCache>();
            services.AddApplicationInsightsTelemetryWorkerService(configuration["APPINSIGHTS_CONNECTIONSTRING"]);

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"), b =>
                    {
                        b.MigrationsAssembly("BancoAlimentar.AlimentaEstaIdeia.Web");
                        b.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    }));
            services.AddIdentityCore<WebUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
                options.User.RequireUniqueEmail = true;
            })
                .AddRoles<ApplicationRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();
            services.AddSingleton(telemetryClient);
            services.AddControllersWithViews().WithRazorPagesAtContentRoot().AddNewtonsoftJson();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddRazorPages(options =>
            {
                options.Conventions.AuthorizeAreaFolder("Admin", "/", "AdminArea");
                options.Conventions.AuthorizeAreaFolder("RoleManagement", "/", "RoleArea");
            }).AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix).AddDataAnnotationsLocalization();
            services.AddSingleton<IViewRenderService, ViewRenderService>();
            services.AddMvc().AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix);
            services.AddScoped<IMail, Mail>();
            var diagnosticSource = new DiagnosticListener("FunctionHosting");
            services.AddSingleton<DiagnosticListener>(diagnosticSource);

            return services.BuildServiceProvider();            
        }
    }
}
