using BancoAlimentar.AlimentaEstaIdeia.Model;
using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
using BancoAlimentar.AlimentaEstaIdeia.Model.Initializer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;


namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests    
{
    public class ServicesFixture
    {
        private readonly ServiceCollection _serviceCollection;

        public ServicesFixture()
        {
            _serviceCollection = new ServiceCollection();

            _serviceCollection.AddScoped<DonationRepository>();
            _serviceCollection.AddMemoryCache();
            _serviceCollection.AddScoped<ProductCatalogueRepository>();
            _serviceCollection.AddScoped<FoodBankRepository>();
            _serviceCollection.AddScoped<DonationItemRepository>();
            _serviceCollection.AddScoped<InvoiceRepository>();
            _serviceCollection.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });
            _serviceCollection.AddIdentityCore<WebUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
                options.User.RequireUniqueEmail = true;
            })
                .AddRoles<ApplicationRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            ServiceProvider = _serviceCollection.BuildServiceProvider();

            var context = ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Database.EnsureCreated();
            UserManager = ServiceProvider.GetRequiredService<UserManager<WebUser>>();
            var roleManager = ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

            Task.Run(() => InitDatabase.Seed(context, UserManager, roleManager)).Wait();

        }

        public ServiceProvider ServiceProvider { get; private set; }        

        public UserManager<WebUser> UserManager { get; private set; }

    }
}
