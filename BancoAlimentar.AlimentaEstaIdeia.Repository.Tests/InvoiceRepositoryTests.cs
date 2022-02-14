// -----------------------------------------------------------------------
// <copyright file="InvoiceRepositoryTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Invoice repository tests.
    /// </summary>
    public class InvoiceRepositoryTests : IClassFixture<ServicesFixture>
    {
        private readonly ServicesFixture fixture;
        private readonly InvoiceRepository invoiceRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvoiceRepositoryTests"/> class.
        /// </summary>
        /// <param name="servicesFixture">Service list.</param>
        public InvoiceRepositoryTests(ServicesFixture servicesFixture)
        {
            // Not elegant but solves the issue of persistnence of memory database across tests.
            ServicesFixture newServicesFixture = new ServicesFixture();
            this.fixture = newServicesFixture;
            this.invoiceRepository = this.fixture.ServiceProvider.GetRequiredService<InvoiceRepository>();
        }

        /// <summary>
        /// Find invoice by public ID.
        /// </summary>
        [Fact]
        public void Can_FindInvoiceByPublicId()
        {
            var invoice = this.invoiceRepository.FindInvoiceByPublicId(this.fixture.PublicId);

            Assert.NotNull(invoice);
            Assert.False(invoice.IsCanceled);
            Assert.Equal(2.5, invoice.Donation.DonationAmount);
            Assert.Equal(this.fixture.Nif, invoice.Donation.Nif);
        }

        /// <summary>
        /// Can not find invoice with empty public id.
        /// </summary>
        [Fact]
        public void Can_Not_FindInvoiceByPublicId_With_Empty_PublicId()
        {
            var invoice = this.invoiceRepository.FindInvoiceByPublicId(string.Empty);
            Assert.Null(invoice);
        }

        /// <summary>
        /// Can not find invoice with wrong public id.
        /// </summary>
        [Fact]
        public void Can_Not_FindInvoiceByPublicId_With_Wrong_PublicId()
        {
            var invoice = this.invoiceRepository.FindInvoiceByPublicId(Guid.NewGuid().ToString());
            Assert.Null(invoice);
        }

        /// <summary>
        /// Can not find invoice by public id when public id is not a valid <see cref="Guid"/>.
        /// </summary>
        [Fact]
        public void Can_Not_FindInvoiceByPublicId_When_PublicId_IsNot_Guid()
        {
            var invoice = this.invoiceRepository.FindInvoiceByPublicId("notaguidid");
            Assert.Null(invoice);
        }

        /// <summary>
        /// Find invoice by donation.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task Can_FindInvoiceByDonation()
        {
            var user = await this.fixture.UserManager.FindByIdAsync(this.fixture.UserId);
            var invoice = this.invoiceRepository.GetOrCreateInvoiceByDonation(this.fixture.DonationId, user);
            Assert.NotNull(invoice);
            Assert.False(invoice.IsCanceled);
            Assert.Equal(2.5, invoice.Donation.DonationAmount);
            Assert.Equal(this.fixture.Nif, invoice.Donation.Nif);

            var invoiceName = this.invoiceRepository.GetInvoiceName(invoice);
            Assert.StartsWith("RECIBO", invoiceName);
        }

        /// <summary>
        /// Can not find invoice by donation with wrong donation id.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task Can_Not_FindInvoiceByDonation_With_Wrong_DonationId()
        {
            var user = await this.fixture.UserManager.FindByIdAsync(this.fixture.UserId);
            var invoice = this.invoiceRepository.GetOrCreateInvoiceByDonation(2000, user);
            Assert.Null(invoice);
        }

        /// <summary>
        /// Can not find invoice by donation with wrong user details.
        /// </summary>
        [Fact]
        public void Can_Not_FindInvoiceByDonation_With_Wrong_UserDetails()
        {
            var user = new WebUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = "newtest@test.com",
                FullName = "New Test User",
            };
            var invoice = this.invoiceRepository.GetOrCreateInvoiceByDonation(this.fixture.DonationId, user);
            Assert.Null(invoice);
        }

        /// <summary>
        /// Can not find invoice by donation when payment status is not a valid payed.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task Can_Not_FindInvoiceByDonation_When_Payment_Status_IsNot_Payed()
        {
            var context = this.fixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var donation = await context.Donations.FirstOrDefaultAsync(d => d.Id == this.fixture.DonationId);
            donation.PaymentStatus = PaymentStatus.NotPayed;
            await context.SaveChangesAsync();

            var user = await this.fixture.UserManager.FindByIdAsync(this.fixture.UserId);
            var invoice = this.invoiceRepository.GetOrCreateInvoiceByDonation(this.fixture.DonationId, user);
            Assert.Null(invoice);

            // Reset the donation status back to Payed
            donation.PaymentStatus = PaymentStatus.Payed;
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Can not find invoice by donation whe payment is status is not ok.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task Can_Not_FindInvoiceByDonation_When_ConfirmPayment_Status_IsNot_Ok()
        {
            var context = this.fixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var donation = await context.Donations.FirstOrDefaultAsync(d => d.Id == this.fixture.DonationId);
            donation.ConfirmedPayment.Status = "err";
            await context.SaveChangesAsync();

            var user = await this.fixture.UserManager.FindByIdAsync(this.fixture.UserId);
            var invoice = this.invoiceRepository.GetOrCreateInvoiceByDonation(this.fixture.DonationId, user);
            Assert.Null(invoice);

            // Reset the donation status back to Payed
            donation.ConfirmedPayment.Status = "ok";
            await context.SaveChangesAsync();
        }
    }
}
