// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden
{
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Components;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Decisions;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Execution;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Hosting;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Entry point.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Runs the app.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>A task.</returns>
        public static async Task Main(string[] args)
        {
            WebApplication app = Build(args);
            await app.Services.GetRequiredService<DecisionStore>().EnsureCreatedAsync(CancellationToken.None);
            ILogger logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(Program));
            TargetConnectionInfo target = app.Services.GetRequiredService<TargetConnectionInfo>();
            CleanProdHardenOptions options = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<CleanProdHardenOptions>>().Value;
            logger.LogInformation("Target: {DataSource} / {Database}. Apply enabled: {AllowApply}. Decision store: {Path}", target.DataSource ?? "(not configured)", target.InitialCatalog ?? "(default)", options.AllowApply, options.ResolveDecisionStorePath());
            await app.RunAsync();
        }

        /// <summary>
        /// Configures services and the request pipeline.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>The app.</returns>
        internal static WebApplication Build(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            IConfigurationSection section = builder.Configuration.GetSection(CleanProdHardenOptions.SectionName);
            builder.Services.Configure<CleanProdHardenOptions>(section);
            var options = new CleanProdHardenOptions();
            section.Bind(options);

            string? connectionString = builder.Configuration.GetConnectionString(TargetConnectionInfo.ConnectionStringName);
            var target = new TargetConnectionInfo(connectionString);
            builder.Services.AddSingleton(target);
            builder.Services.AddDbContextFactory<ApplicationDbContext>(dbOptions =>
            {
                if (target.IsConfigured)
                {
                    dbOptions.UseSqlServer(connectionString, sql => sql.CommandTimeout(120));
                }
                else
                {
                    dbOptions.UseSqlServer();
                }
            });

            string storePath = options.ResolveDecisionStorePath();
            Directory.CreateDirectory(Path.GetDirectoryName(storePath)!);
            builder.Services.AddDbContextFactory<DecisionStoreContext>(dbOptions => dbOptions.UseSqlite($"Data Source={storePath}"));

            builder.Services.AddSingleton<DuplicateGroupService>();
            builder.Services.AddSingleton<TargetDatabaseService>();
            builder.Services.AddSingleton<GroupActionExecutor>();
            builder.Services.AddSingleton<DecisionStore>();
            builder.Services.AddSingleton<ReviewWorkflow>();
            builder.Services.AddScoped<ReviewerSession>();

            builder.Services.AddRazorComponents().AddInteractiveServerComponents();

            WebApplication app = builder.Build();
            app.UseMiddleware<LoopbackOnlyMiddleware>();
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error", createScopeForErrors: true);
            }

            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
            app.UseAntiforgery();
            app.MapStaticAssets();
            app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
            return app;
        }
    }
}
