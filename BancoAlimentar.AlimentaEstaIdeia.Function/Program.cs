// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function
{
    using System.Reflection;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Azure.Functions.Worker.Builder;
    using Microsoft.Azure.Functions.Worker.Configuration;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    /// <summary>
    /// Program class.
    /// </summary>
    public class Program
    {
        private static ConfigurationManager Configuration { get; set; }

        /// <summary>
        /// Main method.
        /// </summary>
        public static void Main(string[] args)
        {
            var builder = FunctionsApplication.CreateBuilder(args);

            builder.Configuration.AddEnvironmentVariables()
                .AddUserSecrets(Assembly.GetExecutingAssembly(), true)
                .AddJsonFile(
                    "appsettings.json",
                    optional: true,
                    reloadOnChange: true);

            builder.ConfigureFunctionsWebApplication();
            Configuration = builder.Configuration;

            // Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
            builder.Services
                .AddApplicationInsightsTelemetryWorkerService()
                .ConfigureFunctionsApplicationInsights();
            builder.Services.AddMemoryCache();
            builder.Services.AddSingleton<IConfiguration>(Configuration);
            builder.Services.AddTransient<IKeyVaultConfigurationManager, KeyVaultConfigurationManager>();
            builder.Services.AddDbContext<InfrastructureDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("Infrastructure"), b =>
                    {
                        b.EnableRetryOnFailure();
                        b.MigrationsAssembly("BancoAlimentar.AlimentaEstaIdeia.Sas.Model");
                        b.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    }));

            builder.Build().Run();
        }
    }
}