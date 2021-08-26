using BancoAlimentar.AlimentaEstaIdeia.Model;
using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    public class InvoiceRepositoryTests : IClassFixture<ServicesFixture>
    {
        private readonly ServicesFixture _fixture;

        public InvoiceRepositoryTests(ServicesFixture servicesFixture)
        {
            this._fixture = servicesFixture;
        }
        [Fact]
        public async Task Can_FindInvoiceByPublicId()
        {
            InvoiceRepository invoiceRepository = this._fixture.ServiceProvider.GetRequiredService<InvoiceRepository>();
            DonationItemRepository donationItemRepository  = this._fixture.ServiceProvider.GetRequiredService<DonationItemRepository>();
            
            var context = _fixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var publicId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var donationId = 100;
            var item = context.ProductCatalogues.FirstOrDefault();
            var foodBank = context.FoodBanks.FirstOrDefault();
            var user = new WebUser
            {
                Id = userId.ToString(),
                Email = "test@test.com",
                FullName = "Test User"
            };
           
            var donation = new Donation()
            {
                Id=donationId,
                PublicId = publicId,
                DonationDate = DateTime.UtcNow,
                DonationAmount = 2.5,
                FoodBank = foodBank,
                Referral = "",                
                DonationItems = donationItemRepository.GetDonationItems($"{item.Id}:1"),
                WantsReceipt = true,
                User = user,
                PaymentStatus = PaymentStatus.Payed,
                Nif = "123456789",
                Payments=new List<PaymentItem>()
            };

            var creditCardPayment = new CreditCardPayment
            {
                Created = DateTime.Now,
                TransactionKey = Guid.NewGuid().ToString(),
                Url = "https://cc.test.easypay.pt/",
                Status="ok"
            };

            donation.Payments.Add(new PaymentItem() { Donation = donation, Payment = creditCardPayment });
            donation.ConfirmedPayment = creditCardPayment;
            

            await _fixture.UserManager.CreateAsync(user);

            await context.Donations.AddAsync(donation);

            await context.SaveChangesAsync();


            var invoice = invoiceRepository.FindInvoiceByPublicId(publicId.ToString());

            Assert.NotNull(invoice);
            
        }
    }
}
