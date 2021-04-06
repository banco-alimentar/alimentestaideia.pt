using Microsoft.VisualStudio.TestTools.UnitTesting;
using BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BancoAlimentar.AlimentaEstaIdeia.Repository;
using Microsoft.EntityFrameworkCore;
using BancoAlimentar.AlimentaEstaIdeia.Model;
using Microsoft.AspNetCore.Identity;
using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
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

            InvoiceModel x = new InvoiceModel(null, context, stringLocalizer);

            Invoice invoice = new Invoice();
            x.Invoice = invoice;
            invoice.Donation = new Donation();

            IList<CurrencyDesc> MyCurrencyDesc = new ReadOnlyCollection<CurrencyDesc>
                (new[] {
                     new CurrencyDesc(2.5, "dois Euros e cinquenta Cêntimos"),
                    new CurrencyDesc(2.05, "dois Euros e cinco Cêntimos"),
                    new CurrencyDesc(12.12, "doze Euros e doze Cêntimos"),
                    new CurrencyDesc(23.2, "vinte e três Euros e vinte Cêntimos"),
                    new CurrencyDesc(2, "dois Euros"),
                    new CurrencyDesc(2, "dois Euros"),
                    new CurrencyDesc(0.23, "vinte e três Cêntimos"),
                    new CurrencyDesc(2.2, "dois Euros e vinte Cêntimos"),
                    new CurrencyDesc(1, "um Euro"),
                    new CurrencyDesc(2323, "dois mil trezentos e vinte e três Euros"),
                    new CurrencyDesc(1.1, "um Euro e dez Cêntimos"),
                    new CurrencyDesc(2.1, "dois Euros e dez Cêntimos"),
                    new CurrencyDesc(1.5, "um Euro e cinquenta Cêntimos"),
                    new CurrencyDesc(0.1, "dez Cêntimos"),
                    new CurrencyDesc(0.98, "noventa e oito Cêntimos"),
                    new CurrencyDesc(13239.12, "treze mil duzentos e trinta e nove Euros e doze Cêntimos"),
                    new CurrencyDesc(20000, "vinte mil Euros"),
                    new CurrencyDesc(1234567.89, "um milhão duzentos e trinta e quatro mil quinhentos e sessenta e sete Euros e oitenta e nove Cêntimos"),
                    new CurrencyDesc(23, "vinte e três Euros"),
                    new CurrencyDesc(3, "três Euros"),
                    new CurrencyDesc(4, "quatro Euros"),
                    new CurrencyDesc(5, "cinco Euros"),
                    new CurrencyDesc(9.99, "nove Euros e noventa e nove Cêntimos")
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

