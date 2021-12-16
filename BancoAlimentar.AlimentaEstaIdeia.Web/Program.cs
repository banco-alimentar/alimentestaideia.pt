// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web
{
    using System;
    using System.Threading.Tasks;
    using Azure.Extensions.AspNetCore.Configuration.Secrets;
    using Azure.Identity;
    using Azure.Security.KeyVault.Secrets;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Initializer;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Server.Kestrel.Https;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Default class for the entry point.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Entry point for the web application.
        /// </summary>
        /// <param name="args">Arguments.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public static async Task Main(string[] args)
       {
            var host = CreateHostBuilder(args).Build();
            await CreateDbIfNotExists(host);
            host.Run();
        }

        /// <summary>
        /// Create the host runtime.
        /// </summary>
        /// <param name="args">Entry point arguments.</param>
        /// <returns>A reference to the <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
             .ConfigureAppConfiguration((context, config) =>
             {
                 var builtConfig = config.Build();
                 if (context.HostingEnvironment.IsProduction() || context.HostingEnvironment.IsStaging())
                 {
                     var secretClient = new SecretClient(
                         new Uri(builtConfig["VaultUri"], UriKind.Absolute),
                         new DefaultAzureCredential());
                     config.AddAzureKeyVault(
                         secretClient,
                         new AzureKeyVaultConfigurationOptions()
                         {
                             ReloadInterval = TimeSpan.FromDays(1),
                         });
                     builtConfig = config.Build();
                     var connection = builtConfig["AppConfig"];
                     if (!string.IsNullOrEmpty(connection))
                     {
                         config.AddAzureAppConfiguration(options =>
                         {
                             options.Connect(connection).UseFeatureFlags(featureFlagOptions =>
                             {
                                 featureFlagOptions.Label = context.HostingEnvironment.EnvironmentName;
                             });
                         });
                     }
                 }
             })
             .ConfigureWebHostDefaults(webBuilder =>
             {
                 webBuilder.UseStartup<Startup>();
             });

        private static async Task CreateDbIfNotExists(IHost host)
        {
            using var scope = host.Services.CreateScope();
            var services = scope.ServiceProvider;
            try
            {
                var context = services.GetRequiredService<ApplicationDbContext>();
                var userManager = services.GetRequiredService<UserManager<WebUser>>();
                var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
                await InitDatabase.Seed(context, userManager, roleManager);
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred creating the DB.");
            }
        }
    }
}
