namespace BancoAlimentar.AlimentaEstaIdeia.RepositoryTests
{
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
                .AddUserSecrets<DonationItemRepositoryTests>();

            Configuration = builder.Build();

            ServiceCollection.AddScoped<DonationRepository>();
            ServiceCollection.AddScoped<ProductCatalogueRepository>();
            ServiceCollection.AddScoped<FoodBankRepository>();
            ServiceCollection.AddScoped<DonationItemRepository>();
            ServiceCollection.AddDbContext<ApplicationDbContext>(options =>
               options.UseSqlServer(
                   Configuration.GetConnectionString("DefaultConnection"), b => b.MigrationsAssembly("BancoAlimentar.AlimentaEstaIdeia.Web")));

            ServiceProvider = ServiceCollection.BuildServiceProvider();
        }

        [TestMethod()]
        public void DeleteFoodBankOneTest()
        {
            ApplicationDbContext context = ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var donations = context.Donations.Include(p => p.DonationItems).Where(p => p.FoodBank.Id == 1).ToList();
            foreach (var item in donations)
            {
                context.Entry(item).State = EntityState.Deleted;
                foreach (var donationItem in item.DonationItems)
                {
                    context.Entry(donationItem).State = EntityState.Deleted;
                }

                
            }

            

            //context.SaveChanges();
        }
    }
}
