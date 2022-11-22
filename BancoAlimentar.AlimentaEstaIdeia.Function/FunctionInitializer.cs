namespace BancoAlimentar.AlimentaEstaIdeia.Function
{
    using Azure.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.Validation;
    using Microsoft.ApplicationInsights;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using System;
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
            configurationBuilder.AddAzureKeyVault(
                new Uri(configuration["VaultUri"]),
                new DefaultAzureCredential(new DefaultAzureCredentialOptions()
                {
                    AdditionallyAllowedTenants = { "*" },
                }));
            configuration = configurationBuilder.Build();

            DbContextOptionsBuilder<ApplicationDbContext> builder = new();
            builder.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"), b => b.MigrationsAssembly("BancoAlimentar.AlimentaEstaIdeia.Web"));
            ApplicationDbContext context = new ApplicationDbContext(builder.Options);
            IUnitOfWork unitOfWork = new UnitOfWork(context, telemetryClient, null, new NifApiValidator());
            return (unitOfWork, context, configuration);
        }
    }
}
