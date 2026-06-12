// -----------------------------------------------------------------------
// <copyright file="DonationPaymentCompletionTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Easypay.Rest.Client.Model;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Tests for keeping donation payment status and confirmed payment in sync.
    /// </summary>
    public class DonationPaymentCompletionTests : IClassFixture<ServicesFixture>
    {
        private readonly ServicesFixture fixture;
        private readonly DonationRepository donationRepository;
        private readonly ApplicationDbContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="DonationPaymentCompletionTests"/> class.
        /// </summary>
        /// <param name="servicesFixture">Service list.</param>
        public DonationPaymentCompletionTests(ServicesFixture servicesFixture)
        {
            this.fixture = servicesFixture;
            this.context = servicesFixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            this.donationRepository = servicesFixture.ServiceProvider.GetRequiredService<DonationRepository>();
        }

        /// <summary>
        /// Generic EasyPay success notifications set both payment status and confirmed payment.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task UpdatePaymentTransaction_SetsConfirmedPaymentOnSuccess()
        {
            await this.fixture.CreateTestDonation(this.context);

            (int basePaymentId, int donationId) = this.donationRepository.UpdatePaymentTransaction(
                string.Empty,
                this.fixture.TransactionKey,
                NotificationGeneric.StatusEnum.Success,
                string.Empty);

            Assert.True(basePaymentId > 0);

            var donation = await this.context.Donations
                .Include(d => d.ConfirmedPayment)
                .FirstAsync(d => d.Id == donationId);

            Assert.Equal(PaymentStatus.Payed, donation.PaymentStatus);
            Assert.NotNull(donation.ConfirmedPayment);
            Assert.Equal(basePaymentId, donation.ConfirmedPayment.Id);
        }

        /// <summary>
        /// Generic EasyPay success notifications mark waiting multibanco donations as paid.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task UpdatePaymentTransaction_MarksWaitingMultiBankDonationPaid()
        {
            var donation = await this.context.Donations.FirstAsync(d => d.Id == this.fixture.DonationId);
            var transactionKey = Guid.NewGuid().ToString();
            var multiBankPayment = new MultiBankPayment
            {
                Created = DateTime.UtcNow,
                TransactionKey = transactionKey,
                EasyPayPaymentId = Guid.NewGuid().ToString(),
                Donation = donation,
            };
            donation.PaymentStatus = PaymentStatus.WaitingPayment;
            donation.ConfirmedPayment = null;
            donation.PaymentList.Add(multiBankPayment);
            this.context.MultiBankPayments.Add(multiBankPayment);
            await this.context.SaveChangesAsync();

            (int basePaymentId, int donationId) = this.donationRepository.UpdatePaymentTransaction(
                string.Empty,
                transactionKey,
                NotificationGeneric.StatusEnum.Success,
                string.Empty);

            Assert.True(basePaymentId > 0);

            donation = await this.context.Donations
                .Include(d => d.ConfirmedPayment)
                .FirstAsync(d => d.Id == donationId);

            Assert.Equal(PaymentStatus.Payed, donation.PaymentStatus);
            Assert.Equal(basePaymentId, donation.ConfirmedPayment.Id);
        }

        /// <summary>
        /// MBWay poll completion sets both payment status and confirmed payment.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task UpdatePaymentStatus_SetsConfirmedPaymentWhenPaid()
        {
            await this.fixture.CreateTestDonation(this.context);

            var donation = await this.context.Donations.FirstAsync(d => d.Id == this.fixture.DonationId);
            var payment = donation.PaymentList.OfType<CreditCardPayment>().First();
            payment.Requested = (float)donation.DonationAmount;
            payment.Paid = (float)donation.DonationAmount;
            donation.PaymentStatus = PaymentStatus.WaitingPayment;
            await this.context.SaveChangesAsync();

            var result = this.donationRepository.UpdatePaymentStatus<CreditCardPayment>(
                new Guid(this.fixture.PublicId),
                SinglePaymentStatus.Paid);

            Assert.True(result);

            donation = await this.context.Donations
                .Include(d => d.ConfirmedPayment)
                .FirstAsync(d => d.Id == this.fixture.DonationId);

            Assert.Equal(PaymentStatus.Payed, donation.PaymentStatus);
            Assert.Equal(payment.Id, donation.ConfirmedPayment.Id);
        }

        /// <summary>
        /// Amount mismatches do not mark the payment row as completed.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CompleteEasyPayPayment_DoesNotSetCompletedWhenAmountMismatch()
        {
            var donation = await this.context.Donations.FirstAsync(d => d.Id == this.fixture.DonationId);
            donation.PaymentStatus = PaymentStatus.WaitingPayment;
            await this.context.SaveChangesAsync();

            var transactionKey = Guid.NewGuid().ToString();
            this.donationRepository.CreateCreditCardPaymnet(donation, "easypay-id", transactionKey, "url", DateTime.UtcNow);

            var result = await this.donationRepository.CompleteEasyPayPaymentAsync<CreditCardPayment>(
                donation.PublicId.ToString(),
                transactionKey,
                "txn-mismatch",
                DateTime.UtcNow,
                0.01f,
                0.01f,
                0,
                0,
                0,
                0,
                this.fixture.Configuration);

            Assert.Equal(0, result.DonationId);

            var payment = await this.context.CreditCardPayments.FirstAsync(p => p.TransactionKey == transactionKey);
            Assert.Null(payment.Completed);
        }

        /// <summary>
        /// PayPal completion sets both payment status and confirmed payment.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task UpdateDonationPaymentId_SetsPaymentStatusAndConfirmedPayment()
        {
            await this.fixture.CreateTestDonation(this.context);
            var donation = await this.context.Donations.FirstAsync(d => d.Id == this.fixture.DonationId);
            donation.PaymentStatus = PaymentStatus.WaitingPayment;
            donation.ConfirmedPayment = null;
            await this.context.SaveChangesAsync();

            var result = this.donationRepository.UpdateDonationPaymentId(
                donation,
                "COMPLETED",
                "paypal-token",
                "payer-1");

            Assert.True(result);

            donation = await this.context.Donations
                .Include(d => d.ConfirmedPayment)
                .FirstAsync(d => d.Id == this.fixture.DonationId);

            Assert.Equal(PaymentStatus.Payed, donation.PaymentStatus);
            Assert.NotNull(donation.ConfirmedPayment);
            Assert.Equal("paypal-token", ((PayPalPayment)donation.ConfirmedPayment).PayPalPaymentId);
        }
    }
}
