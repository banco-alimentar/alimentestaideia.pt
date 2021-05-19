using Microsoft.VisualStudio.TestTools.UnitTesting;
using BancoAlimentar.AlimentaEstaIdeia.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BancoAlimentar.AlimentaEstaIdeia.Model;
using Microsoft.EntityFrameworkCore;

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    [TestClass()]
    public class DonationItemRepositoryTests
    {
        IConfiguration Configuration { get; set; }
        ServiceCollection ServiceCollection { get; set; }

        ServiceProvider ServiceProvider { get; set; }

        public DonationItemRepositoryTests()
        {
            ServiceCollection = new ServiceCollection();

            // the type specified here is just so the secrets library can 
            // find the UserSecretId we added in the csproj file
            var builder = new ConfigurationBuilder()
                .AddUserSecrets<DonationItemRepositoryTests>()
                .AddEnvironmentVariables();

            Configuration = builder.Build();

            var connectionString = Configuration.GetConnectionString("DefaultConnection")
                ?? Environment.GetEnvironmentVariable("ConnectionStrings:DefaultConnection", EnvironmentVariableTarget.User);

            ServiceCollection.AddScoped<DonationRepository>();
            ServiceCollection.AddScoped<ProductCatalogueRepository>();
            ServiceCollection.AddScoped<FoodBankRepository>();
            ServiceCollection.AddScoped<DonationItemRepository>();
            ServiceCollection.AddDbContext<ApplicationDbContext>(options =>
               options.UseSqlServer(
                   connectionString, b => b.MigrationsAssembly("BancoAlimentar.AlimentaEstaIdeia.Web")));

            ServiceProvider = ServiceCollection.BuildServiceProvider();
        }

        [TestMethod()]
        public void GetDonationItemsTest()
        {
            DonationItemRepository repository = ServiceProvider.GetRequiredService<DonationItemRepository>();
            ICollection<DonationItem> items = repository.GetDonationItems("1:1;2:2;3:3;4:6;5:10;6:13;");

            Assert.IsNotNull(items);
            CollectionAssert.AllItemsAreNotNull((System.Collections.ICollection)items);

            Assert.IsTrue(items.Count == 6);
        }
    }
}