namespace BancoAlimentar.AlimentaEstaIdeia.RepositoryTests
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Linq;


    [TestClass()]
    public class DeleteDonationTest
    {
        IConfiguration Configuration { get; set; }
        ServiceCollection ServiceCollection { get; set; }

        ServiceProvider ServiceProvider { get; set; }

        public DeleteDonationTest()
        {
            ServiceCollection = new ServiceCollection();

            // the type specified here is just so the secrets library can 
            // find the UserSecretId we added in the csproj file
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .AddUserSecrets<DeleteDonationTest>()
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

                //var payment = context.MultiBankPayments.Where(p => p.Donation == item).FirstOrDefault();
                //if (payment != null)
                //{
                //    context.Entry(payment).State = EntityState.Deleted;
                //}

                //var paymentCreditCard = context.CreditCardPayments.Where(p => p.Donation == item).FirstOrDefault();
                //if (paymentCreditCard != null)
                //{
                //    context.Entry(paymentCreditCard).State = EntityState.Deleted;
                //}
            }            //context.SaveChanges();
        }
    }
}
