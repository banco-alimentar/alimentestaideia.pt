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

            invoice.Donation.DonationAmount = 12;            
            x.ConvertAmountToText();
            Assert.AreEqual(x.DonationAmountToText, "Doze Euros");

            invoice.Donation.DonationAmount = 12.5;
            x.ConvertAmountToText();
            Assert.AreEqual(x.DonationAmountToText, "Doze Euros e Cinquenta cêntimos");
        }
    }
}

