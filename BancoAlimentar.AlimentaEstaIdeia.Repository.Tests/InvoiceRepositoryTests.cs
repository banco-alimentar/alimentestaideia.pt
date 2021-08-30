// -----------------------------------------------------------------------
// <copyright file="InvoiceRepositoryTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Threading.Tasks;
    using Xunit;

    public class InvoiceRepositoryTests : IClassFixture<ServicesFixture>
    {
        private readonly ServicesFixture _fixture;
        private readonly InvoiceRepository _invoiceRepository;

        public InvoiceRepositoryTests(ServicesFixture servicesFixture)
        {
            this._fixture = servicesFixture;
            _invoiceRepository = this._fixture.ServiceProvider.GetRequiredService<InvoiceRepository>();
        }
        [Fact]
        public void Can_FindInvoiceByPublicId()
        {
            var invoice = _invoiceRepository.FindInvoiceByPublicId(_fixture.PublicId);

            Assert.NotNull(invoice);
            Assert.False(invoice.IsCanceled);
            Assert.Equal(2.5, invoice.Donation.DonationAmount);
            Assert.Equal("123456789", invoice.Donation.Nif);
        }

        [Fact]
        public void Can_Not_FindInvoiceByPublicId_With_Empty_PublicId()
        {
            var invoice = _invoiceRepository.FindInvoiceByPublicId(string.Empty);
            Assert.Null(invoice);
        }

        [Fact]
        public void Can_Not_FindInvoiceByPublicId_With_Wrong_PublicId()
        {
            var invoice = _invoiceRepository.FindInvoiceByPublicId(Guid.NewGuid().ToString());
            Assert.Null(invoice);
        }

        [Fact]
        public void Can_Not_FindInvoiceByPublicId_When_PublicId_IsNot_Guid()
        {
            var invoice = _invoiceRepository.FindInvoiceByPublicId("notaguidid");
            Assert.Null(invoice);
        }

        [Fact]
        public async Task Can_FindInvoiceByDonation()
        {
            var user = await _fixture.UserManager.FindByIdAsync(_fixture.UserId);
            var invoice = _invoiceRepository.FindInvoiceByDonation(_fixture.DonationId, user);
            Assert.NotNull(invoice);
            Assert.False(invoice.IsCanceled);
            Assert.Equal(2.5, invoice.Donation.DonationAmount);
            Assert.Equal("123456789", invoice.Donation.Nif);

            var invoiceName = _invoiceRepository.GetInvoiceName(invoice);
            Assert.StartsWith("RECIBO", invoiceName);
        }

        [Fact]
        public async Task Can_Not_FindInvoiceByDonation_With_Wrong_DonationId()
        {
            var user = await _fixture.UserManager.FindByIdAsync(_fixture.UserId);
            var invoice = _invoiceRepository.FindInvoiceByDonation(2000, user);
            Assert.Null(invoice);
        }

        [Fact]
        public void Can_Not_FindInvoiceByDonation_With_Wrong_UserDetails()
        {
            var user = new WebUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = "newtest@test.com",
                FullName = "New Test User"
            };
            var invoice = _invoiceRepository.FindInvoiceByDonation(_fixture.DonationId, user);
            Assert.Null(invoice);
        }

        [Fact]
        public async Task Can_Not_FindInvoiceByDonation_When_Payment_Status_IsNot_Payed()
        {
            var context = _fixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var donation = await context.Donations.FirstOrDefaultAsync(d => d.Id == _fixture.DonationId);
            donation.PaymentStatus = PaymentStatus.NotPayed;
            await context.SaveChangesAsync();

            var user = await _fixture.UserManager.FindByIdAsync(_fixture.UserId);
            var invoice = _invoiceRepository.FindInvoiceByDonation(_fixture.DonationId, user);
            Assert.Null(invoice);

            //Reset the donation status back to Payed
            donation.PaymentStatus = PaymentStatus.Payed;
            await context.SaveChangesAsync();

        }

        [Fact]
        public async Task Can_Not_FindInvoiceByDonation_When_ConfirmPayment_Status_IsNot_Ok()
        {
            var context = _fixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var donation = await context.Donations.FirstOrDefaultAsync(d => d.Id == _fixture.DonationId);
            donation.ConfirmedPayment.Status = "err";
            await context.SaveChangesAsync();

            var user = await _fixture.UserManager.FindByIdAsync(_fixture.UserId);
            var invoice = _invoiceRepository.FindInvoiceByDonation(_fixture.DonationId, user);
            Assert.Null(invoice);

            //Reset the donation status back to Payed
            donation.ConfirmedPayment.Status = "ok";
            await context.SaveChangesAsync();
        }
    }
}
