// -----------------------------------------------------------------------
// <copyright file="ServicesFixture.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Initializer;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.Validation;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Diagnostics;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;

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

            this.serviceCollection.AddScoped<DonationRepository>();
            this.serviceCollection.AddMemoryCache();
            this.serviceCollection.AddScoped<ProductCatalogueRepository>();
            this.serviceCollection.AddScoped<FoodBankRepository>();
            this.serviceCollection.AddScoped<DonationItemRepository>();
            this.serviceCollection.AddScoped<InvoiceRepository>();
            this.serviceCollection.AddScoped<EasyPayBuilder>();
            this.serviceCollection.AddScoped<PayPalBuilder>();
            this.serviceCollection.AddSingleton<NifApiValidator>();
            this.serviceCollection.AddSingleton(this.Configuration);
            this.serviceCollection.AddScoped<IUnitOfWork, UnitOfWork>();
            this.serviceCollection.AddSingleton<IMemoryCache, MemoryCache>();
            this.serviceCollection.AddApplicationInsightsTelemetryWorkerService(options =>
            {
                options.ConnectionString = this.Configuration["APPINSIGHTS_CONNECTIONSTRING"];
                options.EnableQuickPulseMetricStream = false;
                options.EnablePerformanceCounterCollectionModule = false;
                options.EnableEventCounterCollectionModule = true;
                options.EnableAppServicesHeartbeatTelemetryModule = false;
                options.EnableAzureInstanceMetadataTelemetryModule = false;
            });
            this.serviceCollection.AddSingleton(this.InitializeTenantData());
            this.serviceCollection.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });
            this.serviceCollection.AddDbContext<InfrastructureDbContext>(options =>
            {
                options.UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));
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
            this.UserManager = this.ServiceProvider.GetRequiredService<UserManager<WebUser>>();
            var roleManager = this.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

            Task.Run(() => InitDatabase.Seed(context, this.UserManager, roleManager, this.Configuration)).Wait();
            Task.Run(() => this.CreateTestDonation(context)).Wait();

            var infrastructureContext = this.ServiceProvider.GetRequiredService<InfrastructureDbContext>();
            infrastructureContext.Database.EnsureCreated();
            infrastructureContext.Tenants.Add(new Tenant()
            {
                Created = DateTime.Now,
                Domains = new List<DomainIdentifier>()
                {
                    new DomainIdentifier()
                    {
                        Created = DateTime.UtcNow,
                        DomainName = "localhost",
                        Environment = "Testing",
                    },
                },
                Id = 1,
                Name = "localhost",
                InvoicingStrategy = Sas.Model.Strategy.InvoicingStrategy.SingleInvoiceTable,
                PaymentStrategy = Sas.Model.Strategy.PaymentStrategy.SharedPaymentProcessor,
                PublicId = Guid.NewGuid(),
            });
            infrastructureContext.SaveChanges();
        }

        /// <summary>
        /// Gets or sets the configuration system.
        /// </summary>
        public IConfiguration Configuration { get; set; }

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
        public async Task CreateTestDonation(ApplicationDbContext context)
        {
            var donationItemRepository = this.ServiceProvider.GetRequiredService<DonationItemRepository>();
            var item = await context.ProductCatalogues.FirstOrDefaultAsync();
            var foodBank = await context.FoodBanks.FirstOrDefaultAsync();
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
                Referral = string.Empty,
                DonationItems = donationItemRepository.GetDonationItems($"{item.Id}:1"),
                WantsReceipt = true,
                User = user,
                PaymentStatus = PaymentStatus.Payed,
                Nif = this.Nif,
                PaymentList = new List<BasePayment>(),
            };

            var creditCardPayment = new CreditCardPayment
            {
                Id = 1,
                Created = DateTime.Now,
                TransactionKey = this.TransactionKey,
                Url = "https://cc.test.easypay.pt/",
                Status = "ok",
            };

            var existingCreditCardPayment = await context.Payments.FirstOrDefaultAsync(x => x.Id == 1);
            if (existingCreditCardPayment != null)
            {
                context.Entry(existingCreditCardPayment).State = EntityState.Deleted;
                await context.SaveChangesAsync();
            }

            donation.PaymentList.Add(creditCardPayment);
            donation.ConfirmedPayment = creditCardPayment;

            var existingUser = await context.WebUser.FirstOrDefaultAsync(x => x.Id == this.UserId);
            if (existingUser != null)
            {
                context.Entry(existingUser).State = EntityState.Deleted;
                await context.SaveChangesAsync();
            }

            await this.UserManager.CreateAsync(user);

            var existingDonation = await context.Donations.FirstOrDefaultAsync(x => x.Id == this.DonationId);
            if (existingDonation != null)
            {
                context.Entry(existingDonation).State = EntityState.Deleted;
                await context.SaveChangesAsync();
            }

            await context.Payments.AddAsync(creditCardPayment);
            await context.Donations.AddAsync(donation);

            foreach (var donationItem in context.DonationItems)
            {
                context.Entry(donationItem).State = EntityState.Deleted;
            }

            await context.SaveChangesAsync();
        }

        private IHttpContextAccessor InitializeTenantData()
        {
            Mock<IHttpContextAccessor> mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            DefaultHttpContext context = new DefaultHttpContext();
            context.SetTenant(new Sas.Model.Tenant()
            {
                Created = DateTime.Now,
                Domains = new List<DomainIdentifier>()
                {
                    new DomainIdentifier()
                    {
                        Created = DateTime.UtcNow,
                        DomainName = "localhost",
                        Environment = "localhost",
                    },
                },
                InvoicingStrategy = Sas.Model.Strategy.InvoicingStrategy.SingleInvoiceTable,
                Name = "test",
                PaymentStrategy = Sas.Model.Strategy.PaymentStrategy.SharedPaymentProcessor,
                PublicId = Guid.NewGuid(),
                Id = 1,
            });
            mockHttpContextAccessor.Setup(_ => _.HttpContext).Returns(context);

            return mockHttpContextAccessor.Object;
        }
    }
}
