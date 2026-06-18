namespace BancoAlimentar.AlimentaEstaIdeia.Tools
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.Database;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.EasyPay;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.KeyVault;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.Reporting;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Data.SqlClient;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.IO;
    using System.Reflection;
    using System.Security.Cryptography.Xml;
    using System.Threading.Tasks;

    internal class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                await RunAsync(args);
            }
            catch (SqlException ex) when (ex.Number == 40615 || ex.Message.Contains("is not allowed to access the server", StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("Azure SQL blocked this connection (firewall rule missing or not yet active).");
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine();
                Console.Error.WriteLine("Fix: from the repo root, run:");
                Console.Error.WriteLine("  .\\scripts\\ensure-azure-sql-firewall.ps1");
                Console.Error.WriteLine();
                Console.Error.WriteLine("Then wait up to 5 minutes and retry.");
                Environment.ExitCode = 1;
            }
        }

        private static async Task RunAsync(string[] args)
        {
            IConfiguration Configuration = new ConfigurationBuilder()
                .AddUserSecrets(Assembly.GetExecutingAssembly())
                .AddJsonFile("appsettings.json",
                                optional: true,
                                reloadOnChange: true)
                .Build();
            var config = GetUnitOfWork(Configuration);

            if (args.Length > 0 && string.Equals(args[0], "backfill-donation-campaign-id", StringComparison.OrdinalIgnoreCase))
            {
                bool dryRun = true;
                for (int i = 1; i < args.Length; i++)
                {
                    if (string.Equals(args[i], "--execute", StringComparison.OrdinalIgnoreCase))
                    {
                        dryRun = false;
                    }
                    else if (string.Equals(args[i], "--dry-run", StringComparison.OrdinalIgnoreCase))
                    {
                        dryRun = true;
                    }
                }

                BackfillDonationCampaignIdTool tool = new BackfillDonationCampaignIdTool(
                    config.ApplicationDbContext,
                    config.UnitOfWork)
                {
                    DryRun = dryRun,
                };
                tool.ExecuteTool();
                return;
            }

            if (args.Length > 0 && string.Equals(args[0], "backfill-donation-periodo-oficial", StringComparison.OrdinalIgnoreCase))
            {
                bool dryRun = true;
                for (int i = 1; i < args.Length; i++)
                {
                    if (string.Equals(args[i], "--execute", StringComparison.OrdinalIgnoreCase))
                    {
                        dryRun = false;
                    }
                    else if (string.Equals(args[i], "--dry-run", StringComparison.OrdinalIgnoreCase))
                    {
                        dryRun = true;
                    }
                }

                BackfillDonationPeriodoOficialTool tool = new BackfillDonationPeriodoOficialTool(
                    config.ApplicationDbContext,
                    config.UnitOfWork)
                {
                    DryRun = dryRun,
                };
                tool.ExecuteTool();
                return;
            }

            if (args.Length > 0 && string.Equals(args[0], "generate-donation-report", StringComparison.OrdinalIgnoreCase))
            {
                string outputDirectory = args.Length > 1
                    ? Path.GetFullPath(args[1])
                    : GenerateDonationReportTool.DefaultOutputDirectory;

                await GenerateDonationReportTool.ExecuteAsync(
                    Configuration,
                    config.UnitOfWork,
                    config.ApplicationDbContext,
                    outputDirectory);
                return;
            }

            //CopyKeyVaultSecrets.Copy(new Uri("https://doarbancoalimentar.vault.azure.net/"), new Uri("https://doarbalisboa-dev.vault.azure.net/")).Wait();
            //DuplicateEasyPayAndPayPalConfig.Execute(new Uri("https://doarbancoalimentar.vault.azure.net/"), config.ApplicationDbContext).Wait();
            //ConsolidateDonationIdToPayment consolidateDonationIdToPayment = 
            //    new ConsolidateDonationIdToPayment(config.ApplicationDbContext, config.UnitOfWork);
            //consolidateDonationIdToPayment.ExecuteTool();
            // CopyKeyVaultSecrets.Copy(new Uri("https://alimentaestaideia-key.vault.azure.net/"), new Uri("https://doaralimentestaideia.vault.azure.net/")).Wait();
            //MigrationUserSubscriptionToSubscriptionUserIdColumn migrationUserSubscriptionToSubscriptionUserIdColumn =
            //    new MigrationUserSubscriptionToSubscriptionUserIdColumn(config.ApplicationDbContext, config.UnitOfWork);
            //migrationUserSubscriptionToSubscriptionUserIdColumn.ExecuteTool();

            //ConsolidateConfirmedPaymentIdProd confirmedPaymentTool =
            //    new ConsolidateConfirmedPaymentIdProd(config.ApplicationDbContext, config.UnitOfWork);
            //confirmedPaymentTool.ExecuteTool();

            //ConsolidatePaymentStatusMismatchProd paymentStatusMismatchTool =
              //  new ConsolidatePaymentStatusMismatchProd(config.ApplicationDbContext, config.UnitOfWork);
            ///paymentStatusMismatchTool.ExecuteTool();

            //ConsolidateNifInDonationTable confirmedPaymentTool =
            //    new ConsolidateNifInDonationTable(config.ApplicationDbContext, config.UnitOfWork);
            //confirmedPaymentTool.ExecuteTool();

            //DeleteAllSubscriptionsTool deleteAllSubscriptionsTool = new DeleteAllSubscriptionsTool(
            //    config.ApplicationDbContext, 
            //    config.UnitOfWork, 
            //    Configuration);
            //deleteAllSubscriptionsTool.ExecuteTool();
            //UpdatedNullCompletedDataTimePaymentsTool updatedNullCompletedDataTimePaymentsTool = new UpdatedNullCompletedDataTimePaymentsTool(
            //    config.ApplicationDbContext,
            //    config.UnitOfWork,
            //    Configuration);
            //updatedNullCompletedDataTimePaymentsTool.ExecuteTool();

            //ConsodilatePaymentsWithNullDonationId consodilatePaymentsWithNullDonationId = 
            //    new ConsodilatePaymentsWithNullDonationId(config.ApplicationDbContext, config.UnitOfWork);

            //consodilatePaymentsWithNullDonationId.ExecuteTool();

            //ConsolidateErrorOrWaitPaymentThatAreCompleted consolidateErrorOrWaitPaymentThatAreCompleted 
            //    =new ConsolidateErrorOrWaitPaymentThatAreCompleted(config.ApplicationDbContext, config.UnitOfWork);

            //consolidateErrorOrWaitPaymentThatAreCompleted.ExecuteTool();

        }

        private static (IUnitOfWork UnitOfWork, ApplicationDbContext ApplicationDbContext) GetUnitOfWork(IConfiguration configuration)
        {
            DbContextOptionsBuilder<ApplicationDbContext> builder = new();

            builder.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"), b => b.MigrationsAssembly("BancoAlimentar.AlimentaEstaIdeia.Web"));
            //builder.LogTo(Console.WriteLine);
            ApplicationDbContext context = new ApplicationDbContext(builder.Options);
            TelemetryConfiguration telemetryConfiguration = TelemetryConfiguration.CreateDefault();
            telemetryConfiguration.ConnectionString = configuration["APPINSIGHTS_CONNECTIONSTRING"];
            IUnitOfWork unitOfWork = new UnitOfWork(
                context,
                new TelemetryClient(telemetryConfiguration),
                null,
                new Repository.Validation.NifApiValidator());
            return (unitOfWork, context);
        }
    }
}
