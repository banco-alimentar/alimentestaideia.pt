namespace BancoAlimentar.AlimentaEstaIdeia.Tools
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.Database;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.KeyVault;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Reflection;
    using System.Security.Cryptography.Xml;

    internal class Program
    {
        static void Main(string[] args)
        {
            IConfiguration Configuration = new ConfigurationBuilder()
                .AddUserSecrets(Assembly.GetExecutingAssembly())
                .AddJsonFile("appsettings.json",
                                optional: true,
                                reloadOnChange: true)
                .Build();
            var config = GetUnitOfWork(Configuration);

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

            //ConsolidateNifInDonationTable confirmedPaymentTool =
            //    new ConsolidateNifInDonationTable(config.ApplicationDbContext, config.UnitOfWork);
            //confirmedPaymentTool.ExecuteTool();

            //DeleteAllSubscriptionsTool deleteAllSubscriptionsTool = new DeleteAllSubscriptionsTool(
            //    config.ApplicationDbContext, 
            //    config.UnitOfWork, 
            //    Configuration);
            //deleteAllSubscriptionsTool.ExecuteTool();

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
