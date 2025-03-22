namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tests
{
    using Autofac.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Initializer;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.Validation;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Options;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Middleware;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant.Naming;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Initializer;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Data.Sqlite;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Diagnostics;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// This class defines shared services class fixture for unit tests.
    /// </summary>
    public class ServicesFixture
    {
        private readonly ServiceCollection serviceCollection;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServicesFixture"/> class.
        /// </summary>
        public ServicesFixture()
        {
            var jsongConfig = new ConfigurationBuilder()
              .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
              .AddJsonFile("appsettings.json").Build();

            // the type specified here is just so the secrets library can
            // find the UserSecretId we added in the csproj file
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<ServicesFixture>(optional: true)
                .AddEnvironmentVariables();

            this.Configuration = builder.Build();

            this.serviceCollection = new ServiceCollection();
            this.ServiceCollection = serviceCollection;

            var mock = new Mock<IWebHostEnvironment>();
            mock.Setup(m => m.EnvironmentName).Returns("localhost");

            this.serviceCollection.AddSingleton<IWebHostEnvironment>(mock.Object);
            this.serviceCollection.AddSingleton<TenantInitializationService>();
            this.serviceCollection.AddScoped<DonationRepository>();
            this.serviceCollection.AddMemoryCache();
            this.serviceCollection.AddScoped<ProductCatalogueRepository>();
            this.serviceCollection.AddScoped<FoodBankRepository>();
            this.serviceCollection.AddScoped<DonationItemRepository>();
            this.serviceCollection.AddScoped<InvoiceRepository>();
            this.serviceCollection.AddScoped<EasyPayBuilder>();
            this.serviceCollection.AddSingleton<NifApiValidator>();
            this.serviceCollection.AddSingleton<IConfiguration>(this.Configuration);
            this.serviceCollection.AddScoped<IUnitOfWork, UnitOfWork>();

            this.serviceCollection.AddSingleton<INamingStrategy, DomainNamingStrategy>();
            this.serviceCollection.AddSingleton<INamingStrategy, PathNamingStrategy>();
            this.serviceCollection.AddSingleton<INamingStrategy, SubdomainNamingStrategy>();
            this.serviceCollection.AddSingleton<ITenantProvider, TenantProvider>();
            this.serviceCollection.AddScoped<IInfrastructureUnitOfWork, InfrastructureUnitOfWork>();

            this.serviceCollection.AddSingleton<IMemoryCache, MemoryCache>();
            this.serviceCollection.AddApplicationInsightsTelemetryWorkerService(options =>
            {
                options.ConnectionString = this.Configuration["APPINSIGHTS_CONNECTIONSTRING"];
            });

            this.serviceCollection.AddDbContext<ApplicationDbContext>(options =>
            {
                SqliteConnection connection = new SqliteConnection("Filename=:memory:");
                connection.Open();
                options.UseSqlite(connection);
            });
            this.serviceCollection.AddScoped<InfrastructureDbContext, InfrastructureDbContext>((serviceProvider) =>
            {
                DbContextOptionsBuilder<InfrastructureDbContext> options = new DbContextOptionsBuilder<InfrastructureDbContext>();
                SqliteConnection connection = new SqliteConnection("Filename=:memory:");
                connection.Open();
                options.UseSqlite(connection);
                InfrastructureDbContext infrastructureDbContext = new InfrastructureDbContext(options.Options);
                TenantDevelopmentOptions developmentOptions = new TenantDevelopmentOptions();
                Configuration.GetSection(TenantDevelopmentOptions.Section).Bind(developmentOptions);
                infrastructureDbContext.Database.EnsureCreated();
                if (!string.IsNullOrEmpty(developmentOptions.Name) &&
                    !string.IsNullOrEmpty(developmentOptions.DomainIdentifier))
                {
                    infrastructureDbContext.Tenants.Add(new Tenant()
                    {
                        Name = developmentOptions.Name,
                        Created = DateTime.UtcNow,
                        Domains = new List<DomainIdentifier>()
                    {
                        new DomainIdentifier()
                        {
                            Created = DateTime.UtcNow,
                            DomainName = developmentOptions.DomainIdentifier,
                            Environment = "localhost",
                        },
                    },
                        InvoicingStrategy = Enum.Parse<InvoicingStrategy>(developmentOptions.InvoicingStrategy),
                        PaymentStrategy = Enum.Parse<PaymentStrategy>(developmentOptions.PaymentStrategy),
                        PublicId = Guid.NewGuid(),
                    });
                }
                infrastructureDbContext.Tenants.Add(new Tenant()
                {
                    Name = "alimentaestaideia-developer.azurewebsites.net",
                    Created = DateTime.UtcNow,
                    Domains = new List<DomainIdentifier>()
                    {
                        new DomainIdentifier()
                        {
                            Created = DateTime.UtcNow,
                            DomainName = "dev.alimentestaideia.pt",
                            Environment = "localhost",
                        },
                    },
                    InvoicingStrategy = InvoicingStrategy.SingleInvoiceTable,
                    PaymentStrategy = PaymentStrategy.SharedPaymentProcessor,
                    PublicId = Guid.NewGuid(),
                });
                infrastructureDbContext.SaveChanges();
                return infrastructureDbContext;
            });
            this.serviceCollection.AddIdentityCore<WebUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
                options.User.RequireUniqueEmail = true;
            })
                .AddRoles<ApplicationRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();
            TelemetryConfiguration telemetryConfiguration = TelemetryConfiguration.CreateDefault();
            telemetryConfiguration.ConnectionString = $"InstrumentationKey={Guid.NewGuid()}";
            this.serviceCollection.AddSingleton(new TelemetryClient(telemetryConfiguration));

            this.ServiceProvider = this.serviceCollection.BuildServiceProvider();

            var context = this.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Database.EnsureCreated();
            var infrastructureContext = this.ServiceProvider.GetRequiredService<InfrastructureDbContext>();
            infrastructureContext.Database.EnsureCreated();
            this.UserManager = this.ServiceProvider.GetRequiredService<UserManager<WebUser>>();
            var roleManager = this.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

            Task.Run(() => InitDatabase.Seed(context, this.UserManager, roleManager, this.Configuration)).Wait();
            Task.Run(() => this.CreateTestDonation(context)).Wait();
            TenantDbInitializer.Initialize(infrastructureContext);
        }

        /// <summary>
        /// Gets or sets the configuration system.
        /// </summary>
        public IConfiguration Configuration { get; set; }

        /// <summary>
        /// Gets the service collection.
        /// </summary>
        public ServiceCollection ServiceCollection { get; private set; }

        /// <summary>
        /// Gets the Service provider.
        /// </summary>
        public ServiceProvider ServiceProvider { get; private set; }

        /// <summary>
        /// Gets the user manager.
        /// </summary>
        public UserManager<WebUser> UserManager { get; private set; }

        /// <summary>
        /// Gets the Donation id.
        /// </summary>
        public int DonationId { get; private set; } = 100;

        /// <summary>
        /// Gets the User id.
        /// </summary>
        public string UserId { get; private set; } = "d9b47304-18ab-4e72-ab7f-06c0663e4555";

        /// <summary>
        /// Gets the donation public id.
        /// </summary>
        public string PublicId { get; private set; } = "1c46a5b0-7a76-4b07-abe7-4bfcd252f420";

        /// <summary>
        /// Gets the User Nif.
        /// </summary>
        public string Nif { get; private set; } = "196807050";

        /// <summary>
        /// Gets or sets the easypay transaction key.
        /// </summary>
        public string TransactionKey { get; set; } = "64b17f8d-f52b-4043-883c-e4479432ab3e";

        /// <summary>
        /// This method creates a test donation and its related dependencies which is being used in several tests.
        /// </summary>
        /// <param name="context">Application Db context.</param>
        /// <returns>Returns async task.</returns>
        private async Task CreateTestDonation(ApplicationDbContext context)
        {
            var donationItemRepository = this.ServiceProvider.GetRequiredService<DonationItemRepository>();
            var item = await context.ProductCatalogues.FirstAsync();
            var foodBank = await context.FoodBanks.FirstAsync();
            var user = new WebUser
            {
                Id = this.UserId,
                Email = "test@test.com",
                FullName = "Test User",
            };

            var donation = new Donation()
            {
                Id = this.DonationId,
                PublicId = new Guid(this.PublicId),
                DonationDate = DateTime.UtcNow,
                DonationAmount = 2.5,
                FoodBank = foodBank,
                ReferralEntity = new Referral() { Code = "Testing" },
                DonationItems = donationItemRepository.GetDonationItems($"{item.Id}:1"),
                WantsReceipt = true,
                User = user,
                PaymentStatus = PaymentStatus.Payed,
                Nif = this.Nif,
            };

            var creditCardPayment = new CreditCardPayment
            {
                Created = DateTime.Now,
                TransactionKey = this.TransactionKey,
                Url = "https://cc.test.easypay.pt/",
                Status = "ok",
            };

            donation.ConfirmedPayment = creditCardPayment;

            await this.UserManager.CreateAsync(user);
            await context.Donations.AddAsync(donation);
            await context.SaveChangesAsync();
        }
    }
}
