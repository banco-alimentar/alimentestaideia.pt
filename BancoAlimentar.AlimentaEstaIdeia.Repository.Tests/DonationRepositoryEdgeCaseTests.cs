// -----------------------------------------------------------------------
// <copyright file="DonationRepositoryEdgeCaseTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Easypay.Rest.Client.Model;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Edge-case unit tests for payment completion and notification idempotency.
    /// </summary>
    public class DonationRepositoryEdgeCaseTests : IClassFixture<ServicesFixture>
    {
        private readonly ServicesFixture fixture;
        private readonly DonationRepository donationRepository;
        private readonly ApplicationDbContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="DonationRepositoryEdgeCaseTests"/> class.
        /// </summary>
        /// <param name="servicesFixture">Shared services fixture.</param>
        public DonationRepositoryEdgeCaseTests(ServicesFixture servicesFixture)
        {
            this.fixture = servicesFixture;
            this.donationRepository = servicesFixture.ServiceProvider.GetRequiredService<DonationRepository>();
            this.context = servicesFixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        }

        /// <summary>
        /// Completing the same Easypay payment twice keeps the donation paid.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CompleteEasyPayPayment_IsIdempotentForCreditCard()
        {
            var donation = await this.context.Donations.FirstAsync(d => d.Id == this.fixture.DonationId);
            var payments = donation.PaymentList;
            donation.PaymentList = null;
            var transactionKey = Guid.NewGuid().ToString();
            this.donationRepository.CreateCreditCardPaymnet(donation, "easypay-id", transactionKey, "url", DateTime.UtcNow);

            float amount = (float)donation.DonationAmount;
            var first = await this.donationRepository.CompleteEasyPayPaymentAsync<CreditCardPayment>(
                donation.PublicId.ToString(),
                transactionKey,
                "txn-1",
                DateTime.UtcNow,
                amount,
                amount,
                0,
                0,
                0,
                0,
                this.fixture.Configuration);

            var second = await this.donationRepository.CompleteEasyPayPaymentAsync<CreditCardPayment>(
                donation.PublicId.ToString(),
                transactionKey,
                "txn-2",
                DateTime.UtcNow,
                amount,
                amount,
                0,
                0,
                0,
                0,
                this.fixture.Configuration);

            Assert.Equal(first.DonationId, second.DonationId);
            Assert.True(first.DonationId > 0);

            var updated = await this.context.Donations.AsNoTracking().FirstAsync(d => d.Id == donation.Id);
            Assert.Equal(PaymentStatus.Payed, updated.PaymentStatus);

            donation.PaymentList = payments;
            await this.context.SaveChangesAsync();
        }

        /// <summary>
        /// Failed generic notifications do not downgrade an already paid donation.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task UpdatePaymentTransaction_DoesNotDowngradePaidDonationOnFailure()
        {
            await this.fixture.CreateTestDonation(this.context);

            var donation = await this.context.Donations.FirstAsync(d => d.Id == this.fixture.DonationId);
            donation.PaymentStatus = PaymentStatus.Payed;
            await this.context.SaveChangesAsync();

            (int paymentId, int donationId) = this.donationRepository.UpdatePaymentTransaction(
                string.Empty,
                this.fixture.TransactionKey,
                NotificationGeneric.StatusEnum.Failed,
                "late failure");

            Assert.True(paymentId > 0);
            var updated = await this.context.Donations.AsNoTracking().FirstAsync(d => d.Id == donationId);
            Assert.Equal(PaymentStatus.Payed, updated.PaymentStatus);
        }

        /// <summary>
        /// Mismatched paid amounts do not mark the donation as paid.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CompleteEasyPayPayment_RejectsAmountMismatch()
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

            var updated = await this.context.Donations.AsNoTracking().FirstAsync(d => d.Id == donation.Id);
            Assert.Equal(PaymentStatus.WaitingPayment, updated.PaymentStatus);
        }

        /// <summary>
        /// Unknown transaction keys return zero identifiers without throwing.
        /// </summary>
        [Fact]
        public void UpdatePaymentTransaction_ReturnsZeroForUnknownTransactionKey()
        {
            (int paymentId, int donationId) = this.donationRepository.UpdatePaymentTransaction(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                NotificationGeneric.StatusEnum.Success,
                string.Empty);

            Assert.Equal(0, paymentId);
            Assert.Equal(0, donationId);
        }
    }
}
