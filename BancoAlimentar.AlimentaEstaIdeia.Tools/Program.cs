namespace BancoAlimentar.AlimentaEstaIdeia.Tools
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.Database;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.EasyPay;
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
            //ConsolidateConfirmedPaymentIdProd confirmedPaymentTool =
            //    new ConsolidateConfirmedPaymentIdProd(config.ApplicationDbContext, config.UnitOfWork);
            //confirmedPaymentTool.ExecuteTool();

            DeleteAllSubscriptionsTool deleteAllSubscriptionsTool = new DeleteAllSubscriptionsTool(
                config.ApplicationDbContext, 
                config.UnitOfWork, 
                Configuration);
            deleteAllSubscriptionsTool.ExecuteTool();
        }

        private static (IUnitOfWork UnitOfWork, ApplicationDbContext ApplicationDbContext) GetUnitOfWork(IConfiguration configuration)
        {
            DbContextOptionsBuilder<ApplicationDbContext> builder = new();
            builder.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"), b => b.MigrationsAssembly("BancoAlimentar.AlimentaEstaIdeia.Web"));
            ApplicationDbContext context = new ApplicationDbContext(builder.Options);
            IUnitOfWork unitOfWork = new UnitOfWork(context, new TelemetryClient(new TelemetryConfiguration("")), null);
            return (unitOfWork, context);
        }
    }
}
