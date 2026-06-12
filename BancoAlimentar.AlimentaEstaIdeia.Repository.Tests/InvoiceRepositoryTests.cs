// -----------------------------------------------------------------------
// <copyright file="InvoiceRepositoryTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.Tests.DataGenerator;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
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
        /// <param name="tenant">Tenant.</param>
        [Theory]
        [ClassData(typeof(TenantDataGenerator))]
        public void CanFindInvoiceByPublicId(Tenant tenant)
        {
            var invoice = this.invoiceRepository.FindInvoiceByPublicId(this.fixture.PublicId, tenant, out InvoiceStatusResult invoiceStatusResult);

            Assert.NotNull(invoice);
            Assert.False(invoice.IsCanceled);
            Assert.Equal(2.5, invoice.Donation.DonationAmount);
            Assert.Equal(this.fixture.Nif, invoice.Donation.Nif);
        }

        /// <summary>
        /// Can not find invoice with empty public id.
        /// </summary>
        /// <param name="tenant">Tenant.</param>
        [Theory]
        [ClassData(typeof(TenantDataGenerator))]
        public void CanNotFindInvoiceByPublicIdWithEmptyPublicId(Tenant tenant)
        {
            var invoice = this.invoiceRepository.FindInvoiceByPublicId(string.Empty, tenant, out InvoiceStatusResult invoiceStatusResult);
            Assert.Null(invoice);
        }

        /// <summary>
        /// Can not find invoice with wrong public id.
        /// </summary>
        /// <param name="tenant">Tenant.</param>
        [Theory]
        [ClassData(typeof(TenantDataGenerator))]
        public void CanNotFindInvoiceByPublicIdWithWrongPublicId(Tenant tenant)
        {
            var invoice = this.invoiceRepository.FindInvoiceByPublicId(Guid.NewGuid().ToString(), tenant, out InvoiceStatusResult invoiceStatusResult);
            Assert.Null(invoice);
        }

        /// <summary>
        /// Can not find invoice by public id when public id is not a valid <see cref="Guid"/>.
        /// </summary>
        /// <param name="tenant">Tenant.</param>
        [Theory]
        [ClassData(typeof(TenantDataGenerator))]
        public void CanNotFindInvoiceByPublicIdWhenPublicIdIsNotGuid(Tenant tenant)
        {
            var invoice = this.invoiceRepository.FindInvoiceByPublicId("notaguidid", tenant, out InvoiceStatusResult invoiceStatusResult);
            Assert.Null(invoice);
        }

        /// <summary>
        /// Find invoice by donation.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        /// <param name="tenant">Tenant.</param>
        [Theory]
        [ClassData(typeof(TenantDataGenerator))]
        public async Task CanFindInvoiceByDonation(Tenant tenant)
        {
            var user = await this.fixture.UserManager.FindByIdAsync(this.fixture.UserId);
            var invoice = this.invoiceRepository.GetOrCreateInvoiceByDonation(this.fixture.DonationId, user, tenant, out InvoiceStatusResult result);
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
        /// <param name="tenant">Tenant.</param>
        [Theory]
        [ClassData(typeof(TenantDataGenerator))]
        public async Task CanNotFindInvoiceByDonationWithWrongDonationId(Tenant tenant)
        {
            var user = await this.fixture.UserManager.FindByIdAsync(this.fixture.UserId);
            var invoice = this.invoiceRepository.GetOrCreateInvoiceByDonation(2000, user, tenant, out InvoiceStatusResult result);
            Assert.Null(invoice);
        }

        /// <summary>
        /// Can not find invoice by donation with wrong user details.
        /// </summary>
        /// <param name="tenant">Tenant.</param>
        [Theory]
        [ClassData(typeof(TenantDataGenerator))]
        public void CanNotFindInvoiceByDonationWithWrongUserDetails(Tenant tenant)
        {
            var user = new WebUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = "newtest@test.com",
                FullName = "New Test User",
            };
            var invoice = this.invoiceRepository.GetOrCreateInvoiceByDonation(this.fixture.DonationId, user, tenant, out InvoiceStatusResult result);
            Assert.Null(invoice);
            Assert.Equal(InvoiceStatusResult.DonationUserNotFound, result);
        }

        /// <summary>
        /// Can not find invoice by donation when payment status is not a valid payed.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        /// <param name="tenant">Tenant.</param>
        [Theory]
        [ClassData(typeof(TenantDataGenerator))]
        public async Task CanNotFindInvoiceByDonationWhenPaymentStatusIsNotPayed(Tenant tenant)
        {
            var context = this.fixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var donation = await context.Donations.FirstOrDefaultAsync(d => d.Id == this.fixture.DonationId);
            donation.PaymentStatus = PaymentStatus.NotPayed;
            await context.SaveChangesAsync();

            var user = await this.fixture.UserManager.FindByIdAsync(this.fixture.UserId);
            var invoice = this.invoiceRepository.GetOrCreateInvoiceByDonation(this.fixture.DonationId, user, tenant, out InvoiceStatusResult result);
            Assert.Null(invoice);
            Assert.Equal(InvoiceStatusResult.NotPayed, result);

            // Reset the donation status back to Payed
            donation.PaymentStatus = PaymentStatus.Payed;
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Can not find invoice by donation whe payment is status is not ok.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        /// <param name="tenant">Tenant.</param>
        [Theory]
        [ClassData(typeof(TenantDataGenerator))]
        public async Task CanNotFindInvoiceByDonationWhenConfirmPaymentStatusIsNotOk(Tenant tenant)
        {
            var context = this.fixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var donation = await context.Donations.FirstOrDefaultAsync(d => d.Id == this.fixture.DonationId);
            donation.ConfirmedPayment.Status = "err";
            await context.SaveChangesAsync();

            var user = await this.fixture.UserManager.FindByIdAsync(this.fixture.UserId);
            var invoice = this.invoiceRepository.GetOrCreateInvoiceByDonation(this.fixture.DonationId, user, tenant, out InvoiceStatusResult result);
            Assert.Null(invoice);
            Assert.Equal(InvoiceStatusResult.ConfirmedFailedPaymentStatus, result);

            // Reset the donation status back to Payed
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Returns all invoices for a user.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        /// <param name="tenant">Tenant.</param>
        [Theory]
        [ClassData(typeof(TenantDataGenerator))]
        public async Task CanGetAllInvoicesFromUserId(Tenant tenant)
        {
            var user = await this.fixture.UserManager.FindByIdAsync(this.fixture.UserId);
            var invoice = this.invoiceRepository.GetOrCreateInvoiceByDonation(
                this.fixture.DonationId,
                user,
                tenant,
                out InvoiceStatusResult _);

            var result = this.invoiceRepository.GetAllInvoicesFromUserId(this.fixture.UserId);

            Assert.Contains(result, i => i.Id == invoice.Id);
        }

        /// <summary>
        /// Empty user id returns an empty list.
        /// </summary>
        [Fact]
        public void GetAllInvoicesFromUserIdReturnsEmptyForEmptyUserId()
        {
            Assert.Empty(this.invoiceRepository.GetAllInvoicesFromUserId(string.Empty));
        }

        /// <summary>
        /// Invoice name uses the receipt number regardless of canceled state.
        /// </summary>
        [Fact]
        public void CanGetInvoiceNameForCanceledInvoice()
        {
            var invoice = new Invoice
            {
                Number = "2026/42",
                IsCanceled = true,
            };

            var name = this.invoiceRepository.GetInvoiceName(invoice);

            Assert.Equal("RECIBO Nº 2026/42", name);
        }

        /// <summary>
        /// Null invoice returns null name.
        /// </summary>
        [Fact]
        public void GetInvoiceNameReturnsNullForNullInvoice()
        {
            Assert.Null(this.invoiceRepository.GetInvoiceName(null));
        }

        /// <summary>
        /// Canceled invoices are not returned and report canceled status.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task FindInvoiceByPublicIdReturnsCanceledStatus()
        {
            var context = this.fixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await this.fixture.UserManager.FindByIdAsync(this.fixture.UserId);
            await this.EnsureUserHasAddressAsync(context, user);
            var tenant = GetDefaultTenant();
            var publicId = Guid.NewGuid();
            var donation = await this.SeedPaidDonationAsync(context, user, publicId, this.fixture.Nif);
            var invoice = this.invoiceRepository.GetOrCreateInvoiceByDonation(
                donation.Id,
                user,
                tenant,
                out InvoiceStatusResult _);
            Assert.NotNull(invoice);

            invoice.IsCanceled = true;
            await context.SaveChangesAsync();

            var found = this.invoiceRepository.FindInvoiceByPublicId(
                publicId.ToString(),
                tenant,
                out InvoiceStatusResult status);

            Assert.Null(found);
            Assert.Equal(InvoiceStatusResult.InvoiceCanceled, status);
        }

        /// <summary>
        /// Invalid donation and user NIF returns NifNotValid.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task GetOrCreateInvoiceByDonationReturnsNifNotValid()
        {
            var context = this.fixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await this.fixture.UserManager.FindByIdAsync(this.fixture.UserId);
            user.Nif = "123";
            await context.SaveChangesAsync();

            var tenant = GetDefaultTenant();
            var donation = await this.SeedPaidDonationAsync(
                context,
                user,
                Guid.NewGuid(),
                "456");

            var invoice = this.invoiceRepository.GetOrCreateInvoiceByDonation(
                donation.Id,
                user,
                tenant,
                out InvoiceStatusResult status);

            Assert.Null(invoice);
            Assert.Equal(InvoiceStatusResult.NifNotValid, status);

            user.Nif = this.fixture.Nif;
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// When generateInvoice is false, no new invoice row is created.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task DoesNotCreateInvoiceWhenGenerateInvoiceIsFalse()
        {
            var context = this.fixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await this.fixture.UserManager.FindByIdAsync(this.fixture.UserId);
            await this.EnsureUserHasAddressAsync(context, user);
            var tenant = GetDefaultTenant();
            var donation = await this.SeedPaidDonationAsync(
                context,
                user,
                Guid.NewGuid(),
                this.fixture.Nif);

            int countBefore = await context.Invoices.CountAsync(i => i.Donation.Id == donation.Id);

            var invoice = this.invoiceRepository.GetOrCreateInvoiceByDonation(
                donation.Id,
                user,
                tenant,
                out InvoiceStatusResult _,
                generateInvoice: false);

            int countAfter = await context.Invoices.CountAsync(i => i.Donation.Id == donation.Id);

            Assert.Null(invoice);
            Assert.Equal(countBefore, countAfter);
        }

        /// <summary>
        /// Second call returns the same invoice without creating a duplicate.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task GetOrCreateInvoiceByDonationIsIdempotent()
        {
            var context = this.fixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await this.fixture.UserManager.FindByIdAsync(this.fixture.UserId);
            await this.EnsureUserHasAddressAsync(context, user);
            var tenant = GetDefaultTenant();
            var donation = await this.SeedPaidDonationAsync(
                context,
                user,
                Guid.NewGuid(),
                this.fixture.Nif);

            var first = this.invoiceRepository.GetOrCreateInvoiceByDonation(
                donation.Id,
                user,
                tenant,
                out InvoiceStatusResult _);
            var second = this.invoiceRepository.GetOrCreateInvoiceByDonation(
                donation.Id,
                user,
                tenant,
                out InvoiceStatusResult _);

            Assert.NotNull(first);
            Assert.Equal(first.Id, second.Id);
            Assert.Equal(1, await context.Invoices.CountAsync(i => i.Donation.Id == donation.Id));
        }

        /// <summary>
        /// Repairs payment status when confirmed payment is set but donation status is not payed.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task GetOrCreateInvoiceByDonation_FixPaymentStatusFromConfirmedPayment()
        {
            var context = this.fixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await this.fixture.UserManager.FindByIdAsync(this.fixture.UserId);
            await this.EnsureUserHasAddressAsync(context, user);
            var tenant = GetDefaultTenant();
            var donation = await this.SeedPaidDonationAsync(context, user, Guid.NewGuid(), this.fixture.Nif);
            var payment = donation.ConfirmedPayment as CreditCardPayment;
            payment.Requested = (float)donation.DonationAmount;
            payment.Paid = (float)donation.DonationAmount;
            donation.PaymentStatus = PaymentStatus.NotPayed;
            context.Update(donation);
            await context.SaveChangesAsync();

            var invoice = this.invoiceRepository.GetOrCreateInvoiceByDonation(
                donation.Id,
                user,
                tenant,
                out InvoiceStatusResult result);

            Assert.NotNull(invoice);
            Assert.Equal(InvoiceStatusResult.GeneratedOk, result);

            var updated = await context.Donations.AsNoTracking().FirstAsync(d => d.Id == donation.Id);
            Assert.Equal(PaymentStatus.Payed, updated.PaymentStatus);
        }

        /// <summary>
        /// Repairs confirmed payment from the payment list when missing on a paid donation.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task GetOrCreateInvoiceByDonation_FixConfirmedPaymentFromPaymentList()
        {
            var context = this.fixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await this.fixture.UserManager.FindByIdAsync(this.fixture.UserId);
            await this.EnsureUserHasAddressAsync(context, user);
            var tenant = GetDefaultTenant();
            var donation = await this.SeedPaidDonationAsync(context, user, Guid.NewGuid(), this.fixture.Nif);
            donation.ConfirmedPayment = null;
            context.Update(donation);
            await context.SaveChangesAsync();

            var invoice = this.invoiceRepository.GetOrCreateInvoiceByDonation(
                donation.Id,
                user,
                tenant,
                out InvoiceStatusResult result);

            Assert.NotNull(invoice);
            Assert.Equal(InvoiceStatusResult.GeneratedOk, result);
        }

        /// <summary>
        /// Returns ConfirmedPaymentIsNull when no payment can be inferred for a paid donation.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task GetOrCreateInvoiceByDonation_ReturnsConfirmedPaymentIsNull()
        {
            var context = this.fixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await this.fixture.UserManager.FindByIdAsync(this.fixture.UserId);
            var tenant = GetDefaultTenant();
            var donation = await this.SeedPaidDonationAsync(context, user, Guid.NewGuid(), this.fixture.Nif);
            var payments = await context.Payments.Where(p => p.Donation.Id == donation.Id).ToListAsync();
            context.Payments.RemoveRange(payments);
            donation.ConfirmedPayment = null;
            donation.PaymentList = null;
            context.Update(donation);
            await context.SaveChangesAsync();

            var invoice = this.invoiceRepository.GetOrCreateInvoiceByDonation(
                donation.Id,
                user,
                tenant,
                out InvoiceStatusResult result);

            Assert.Null(invoice);
            Assert.Equal(InvoiceStatusResult.ConfirmedPaymentIsNull, result);
        }

        /// <summary>
        /// Uses the user NIF when the donation NIF is invalid.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task GetOrCreateInvoiceByDonation_UsesUserNif_WhenDonationNifInvalid()
        {
            var context = this.fixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await this.fixture.UserManager.FindByIdAsync(this.fixture.UserId);
            user.Nif = this.fixture.Nif;
            await this.EnsureUserHasAddressAsync(context, user);
            var tenant = GetDefaultTenant();
            var donation = await this.SeedPaidDonationAsync(context, user, Guid.NewGuid(), "000000000");

            var invoice = this.invoiceRepository.GetOrCreateInvoiceByDonation(
                donation.Id,
                user,
                tenant,
                out InvoiceStatusResult result);

            Assert.NotNull(invoice);
            Assert.Equal(InvoiceStatusResult.GeneratedOk, result);
        }

        /// <summary>
        /// Donations paid in a previous calendar year still generate invoices with current year-boundary logic.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task GetOrCreateInvoiceByDonation_AllowsInvoiceForDonationPaidInPreviousYear()
        {
            var context = this.fixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await this.fixture.UserManager.FindByIdAsync(this.fixture.UserId);
            await this.EnsureUserHasAddressAsync(context, user);
            var tenant = GetDefaultTenant();
            var donation = await this.SeedPaidDonationAsync(context, user, Guid.NewGuid(), this.fixture.Nif);
            var payment = donation.ConfirmedPayment;
            payment.Created = new DateTime(DateTime.UtcNow.Year - 1, 12, 31, 12, 0, 0, DateTimeKind.Utc);
            context.Update(payment);
            await context.SaveChangesAsync();

            var invoice = this.invoiceRepository.GetOrCreateInvoiceByDonation(
                donation.Id,
                user,
                tenant,
                out InvoiceStatusResult result);

            Assert.NotNull(invoice);
            Assert.Equal(InvoiceStatusResult.GeneratedOk, result);
            Assert.NotEqual(InvoiceStatusResult.DonationIsOneYearOld, result);
        }

        private static Tenant GetDefaultTenant()
        {
            return new Tenant
            {
                Created = DateTime.Now,
                Domains = new List<DomainIdentifier>
                {
                    new DomainIdentifier
                    {
                        Created = DateTime.UtcNow,
                        DomainName = "localhost",
                        Environment = "Testing",
                    },
                },
                Id = 1,
                Name = "localhost",
                InvoicingStrategy = InvoicingStrategy.SingleInvoiceTable,
                PaymentStrategy = PaymentStrategy.SharedPaymentProcessor,
                PublicId = Guid.NewGuid(),
            };
        }

        private async Task EnsureUserHasAddressAsync(ApplicationDbContext context, WebUser user)
        {
            if (user.Address == null || string.IsNullOrEmpty(user.Address.Address1))
            {
                user.Address ??= new DonorAddress();
                user.Address.Address1 = "Rua Teste";
                user.Address.City = "Lisboa";
                user.Address.Country = "PT";
                context.Update(user);
                await context.SaveChangesAsync();
            }
        }

        private async Task<Donation> SeedPaidDonationAsync(
            ApplicationDbContext context,
            WebUser user,
            Guid publicId,
            string nif)
        {
            await this.EnsureUserHasAddressAsync(context, user);
            var foodBank = await context.FoodBanks.FirstAsync();
            var product = await context.ProductCatalogues.FirstAsync();
            var confirmedPayment = new CreditCardPayment
            {
                Created = DateTime.UtcNow,
                TransactionKey = Guid.NewGuid().ToString(),
                Url = "https://example.com",
                Status = "ok",
            };
            var donation = new Donation
            {
                PublicId = publicId,
                DonationAmount = 5,
                DonationDate = DateTime.UtcNow,
                FoodBank = foodBank,
                User = user,
                Nif = nif,
                PaymentStatus = PaymentStatus.Payed,
                ConfirmedPayment = confirmedPayment,
                PaymentList = new List<BasePayment> { confirmedPayment },
                DonationItems = new List<DonationItem>
                {
                    new DonationItem
                    {
                        ProductCatalogue = product,
                        Quantity = 1,
                        Price = product.Cost,
                    },
                },
            };
            donation.DonationItems.First().Donation = donation;
            confirmedPayment.Donation = donation;
            context.Donations.Add(donation);
            await context.SaveChangesAsync();
            return donation;
        }
    }
}
