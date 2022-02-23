namespace BancoAlimentar.AlimentaEstaIdeia.Tools
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.Database;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.EasyPay;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.KeyVault;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Reflection;

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

            DuplicateEasyPayAndPayPalConfig.Execute(new Uri("https://doarbancoalimentar.vault.azure.net/"), config.ApplicationDbContext).Wait();
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
        }

        private static (IUnitOfWork UnitOfWork, ApplicationDbContext ApplicationDbContext) GetUnitOfWork(IConfiguration configuration)
        {
            DbContextOptionsBuilder<ApplicationDbContext> builder = new();

            builder.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"), b => b.MigrationsAssembly("BancoAlimentar.AlimentaEstaIdeia.Web"));
            //builder.LogTo(Console.WriteLine);
            ApplicationDbContext context = new ApplicationDbContext(builder.Options);
            IUnitOfWork unitOfWork = new UnitOfWork(
                context, 
                new TelemetryClient(new TelemetryConfiguration("")), 
                null, 
                new Repository.Validation.NifApiValidator(),
                null);
            return (unitOfWork, context);
        }
    }
}
