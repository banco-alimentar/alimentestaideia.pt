using BancoAlimentar.AlimentaEstaIdeia.Model;
using BancoAlimentar.AlimentaEstaIdeia.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BancoAlimentar.AlimentaEstaIdeia.RepositoryTests
{
    [TestClass()]
    public class InvoiceRepositoryTests
    {
        IConfiguration Configuration { get; set; }
        ServiceCollection ServiceCollection { get; set; }

        ServiceProvider ServiceProvider { get; set; }

        public InvoiceRepositoryTests()
        {
            ServiceCollection = new ServiceCollection();

            // the type specified here is just so the secrets library can 
            // find the UserSecretId we added in the csproj file
            var builder = new ConfigurationBuilder()
                .AddUserSecrets<DonationItemRepositoryTests>();

            Configuration = builder.Build();

            ServiceCollection.AddScoped<DonationRepository>();
            ServiceCollection.AddScoped<ProductCatalogueRepository>();
            ServiceCollection.AddScoped<FoodBankRepository>();
            ServiceCollection.AddScoped<DonationItemRepository>();
            ServiceCollection.AddScoped<InvoiceRepository>();
            ServiceCollection.AddDbContext<ApplicationDbContext>(options =>
               options.UseSqlServer(
                   Configuration.GetConnectionString("DefaultConnection"), b => b.MigrationsAssembly("BancoAlimentar.AlimentaEstaIdeia.Web")));

            ServiceProvider = ServiceCollection.BuildServiceProvider();
        }

        [TestMethod()]
        public void TestInvoiceSequence()
        {
            InvoiceRepository invoiceRepository = ServiceProvider.GetRequiredService<InvoiceRepository>();
            invoiceRepository.FindInvoiceByDonation(203, null);
        }
    }
}
