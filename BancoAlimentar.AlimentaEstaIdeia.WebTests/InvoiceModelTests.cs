using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BancoAlimentar.AlimentaEstaIdeia.Repository;
using Microsoft.EntityFrameworkCore;
using BancoAlimentar.AlimentaEstaIdeia.Model;
using Microsoft.Extensions.Localization;
using Moq;
using System.Collections.ObjectModel;

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage.Tests
{
    [TestClass()]
    public class InvoiceModelTests
    {

        IConfiguration Configuration { get; set; }
        ServiceCollection ServiceCollection { get; set; }

        ServiceProvider ServiceProvider { get; set; }

        public InvoiceModelTests()
        {
            ServiceCollection = new ServiceCollection();

            // the type specified here is just so the secrets library can 
            // find the UserSecretId we added in the csproj file
            var builder = new ConfigurationBuilder()
                .AddUserSecrets<InvoiceModelTests>();

            Configuration = builder.Build();

            ServiceCollection.AddScoped<DonationRepository>();
            ServiceCollection.AddScoped<ProductCatalogueRepository>();
            ServiceCollection.AddScoped<FoodBankRepository>();
            ServiceCollection.AddScoped<DonationItemRepository>();
            ServiceCollection.AddScoped<IUnitOfWork, UnitOfWork>();
            ServiceCollection.AddApplicationInsightsTelemetryWorkerService(Configuration["APPINSIGHTS_CONNECTIONSTRING"]);
            ServiceCollection.AddDbContext<ApplicationDbContext>(options =>
               options.UseSqlServer(
                   Configuration.GetConnectionString("DefaultConnection"), b => b.MigrationsAssembly("BancoAlimentar.AlimentaEstaIdeia.Web")));

            ServiceProvider = ServiceCollection.BuildServiceProvider();
        }

        private  struct CurrencyDesc
        {
            public Double val;
            public String desc;
            public CurrencyDesc(Double _val,String _desc)
            {
                val = _val;
                desc = _desc;
            }
        }

        [TestMethod()]
        public void ConvertAmountToTextTest()
        {
            IUnitOfWork context = this.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var stringLocalizerFactoryMock = new Mock<IStringLocalizerFactory>();
            IStringLocalizerFactory stringLocalizer = stringLocalizerFactoryMock.Object;

            InvoiceModel x = new InvoiceModel(context, stringLocalizer);

            Invoice invoice = new Invoice();
            x.Invoice = invoice;
            invoice.Donation = new Donation();

            IList<CurrencyDesc> MyCurrencyDesc = new ReadOnlyCollection<CurrencyDesc>
                (new[] {
                    new CurrencyDesc(2.5 , "dois euros e cinquenta cêntimos"),
                    new CurrencyDesc(2.05 , "dois euros e cinco cêntimos"),
                    new CurrencyDesc(12.12 , "doze euros e doze cêntimos"),
                    new CurrencyDesc(23.2 , "vinte e três euros e vinte cêntimos"),
                    new CurrencyDesc(2 , "dois euros"),
                    new CurrencyDesc(2 , "dois euros"),
                    new CurrencyDesc(0.23 , "vinte e três cêntimos"),
                    new CurrencyDesc(2.2 , "dois euros e vinte cêntimos"),
                    new CurrencyDesc(1 , "um euro"),
                    new CurrencyDesc(2323 , "dois mil trezentos e vinte e três euros"),
                    new CurrencyDesc(1.1 , "um euro e dez cêntimos"),
                    new CurrencyDesc(2.1 , "dois euros e dez cêntimos"),
                    new CurrencyDesc(1.5 , "um euro e cinquenta cêntimos"),
                    new CurrencyDesc(0.1 , "dez cêntimos"),
                    new CurrencyDesc(0.98 , "noventa e oito cêntimos"),
                    new CurrencyDesc(13239.12 , "treze mil duzentos e trinta e nove euros e doze cêntimos"),
                    new CurrencyDesc(20000 , "vinte mil euros"),
                    new CurrencyDesc(1234567.89 , "um milhão duzentos e trinta e quatro mil quinhentos e sessenta e sete euros e oitenta e nove cêntimos"),
                    new CurrencyDesc(23 , "vinte e três euros"),
                    new CurrencyDesc(3 , "três euros"),
                    new CurrencyDesc(4 , "quatro euros"),
                    new CurrencyDesc(5 , "cinco euros"),
                    new CurrencyDesc(9.99 , "nove euros e noventa e nove cêntimos")
                });

            foreach (CurrencyDesc cd in MyCurrencyDesc)
            {
                invoice.Donation.DonationAmount = cd.val;
                x.ConvertAmountToText();
                Assert.AreEqual(x.DonationAmountToText, cd.desc);
            }

        }
    }
}

