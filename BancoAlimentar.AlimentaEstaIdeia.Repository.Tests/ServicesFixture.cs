// -----------------------------------------------------------------------
// <copyright file="ServicesFixture.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Initializer;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Diagnostics;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// This class defines shared services class fixture for unit tests.
    /// </summary>
    public class ServicesFixture
    {       
        private readonly ServiceCollection _serviceCollection;

        /// <summary>
        /// Constructor method for initiliazing required services.
        /// </summary>
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
            _serviceCollection.AddSingleton(new TelemetryClient(new TelemetryConfiguration(Guid.NewGuid().ToString())));

            ServiceProvider = _serviceCollection.BuildServiceProvider();

            var context = ServiceProvider.GetRequiredService<ApplicationDbContext>();

            context.Database.EnsureCreated();
            UserManager = ServiceProvider.GetRequiredService<UserManager<WebUser>>();
            var roleManager = ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

            Task.Run(() => InitDatabase.Seed(context, UserManager, roleManager)).Wait();
            Task.Run(() => CreateTestDonation(context)).Wait();

        }

        public ServiceProvider ServiceProvider { get; private set; }

        public UserManager<WebUser> UserManager { get; private set; }

        public int DonationId { get; private set; } = 100;
        public string UserId { get; private set; } = "d9b47304-18ab-4e72-ab7f-06c0663e4555";
        public string PublicId { get; private set; } = "1c46a5b0-7a76-4b07-abe7-4bfcd252f420";

        public string TransactionKey { get; set; } = "64b17f8d-f52b-4043-883c-e4479432ab3e";



        /// <summary>
        /// This method creates a test donation and its related dependencies which is being used in several tests.
        /// </summary>
        /// <param name="context">Application Db context.</param>
        /// <returns>Returns async task.</returns>
        private async Task CreateTestDonation(ApplicationDbContext context)
        {
            var donationItemRepository = ServiceProvider.GetRequiredService<DonationItemRepository>();
            var item = await context.ProductCatalogues.FirstOrDefaultAsync();
            var foodBank = await context.FoodBanks.FirstOrDefaultAsync();
            var user = new WebUser
            {
                Id = UserId,
                Email = "test@test.com",
                FullName = "Test User"
            };

            var donation = new Donation()
            {
                Id = DonationId,
                PublicId = new Guid(PublicId),
                DonationDate = DateTime.UtcNow,
                DonationAmount = 2.5,
                FoodBank = foodBank,
                Referral = "",
                DonationItems = donationItemRepository.GetDonationItems($"{item.Id}:1"),
                WantsReceipt = true,
                User = user,
                PaymentStatus = PaymentStatus.Payed,
                Nif = "123456789",
                Payments = new List<PaymentItem>()
            };

            var creditCardPayment = new CreditCardPayment
            {
                Created = DateTime.Now,
                TransactionKey = TransactionKey,
                Url = "https://cc.test.easypay.pt/",
                Status = "ok"
            };

            donation.Payments.Add(new PaymentItem() { Donation = donation, Payment = creditCardPayment });
            donation.ConfirmedPayment = creditCardPayment;

            await UserManager.CreateAsync(user);
            await context.Donations.AddAsync(donation);
            await context.SaveChangesAsync();
        }
    }
}
