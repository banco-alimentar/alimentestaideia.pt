// -----------------------------------------------------------------------
// <copyright file="SubscriptionPaymentConsistencyTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Easypay.Rest.Client.Model;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;
    using LocalSubscription = BancoAlimentar.AlimentaEstaIdeia.Model.Subscription;

    /// <summary>
    /// Regression tests for subscription payment consistency safeguards.
    /// </summary>
    public class SubscriptionPaymentConsistencyTests : IClassFixture<ServicesFixture>
    {
        private readonly ServicesFixture fixture;
        private readonly DonationRepository donationRepository;
        private readonly ApplicationDbContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionPaymentConsistencyTests"/> class.
        /// </summary>
        /// <param name="servicesFixture">Shared services fixture.</param>
        public SubscriptionPaymentConsistencyTests(ServicesFixture servicesFixture)
        {
            this.fixture = servicesFixture;
            this.donationRepository = servicesFixture.ServiceProvider.GetRequiredService<DonationRepository>();
            this.context = servicesFixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        }

        /// <summary>
        /// A zero-value subscription callback cannot create a local recurring donation.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CompleteEasyPayPayment_DoesNotCreateRecurringDonationForZeroAmount()
        {
            string transactionKey = Guid.NewGuid().ToString();
            var seed = await SubscriptionRepositoryTestHelpers.SeedSubscriptionAsync(
                this.context,
                transactionKey,
                DateTime.UtcNow.AddDays(-2));
            int donationsBefore = await this.context.Donations.CountAsync();

            var result = await this.donationRepository.CompleteEasyPayPaymentAsync<CreditCardPayment>(
                seed.InitialDonation.PublicId.ToString(),
                transactionKey,
                Guid.NewGuid().ToString(),
                DateTime.UtcNow,
                0,
                0,
                0,
                0,
                0,
                0,
                this.fixture.Configuration);

            Assert.Equal(0, result.DonationId);
            Assert.Equal(donationsBefore, await this.context.Donations.CountAsync());
            Assert.False(await this.context.Payments.AnyAsync(payment => payment.TransactionKey == transactionKey));
        }

        /// <summary>
        /// Reuses the existing recurring payment and stores the EasyPay single-payment id.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CompleteEasyPayPayment_IsIdempotentAndStoresSinglePaymentId()
        {
            string transactionKey = Guid.NewGuid().ToString();
            string easyPayPaymentId = Guid.NewGuid().ToString();
            string easyPayTransactionId = Guid.NewGuid().ToString();
            DateTime captureDate = DateTime.UtcNow;
            var seed = await SubscriptionRepositoryTestHelpers.SeedSubscriptionAsync(
                this.context,
                transactionKey,
                captureDate.AddDays(-2));

            var firstResult = await this.donationRepository.CompleteEasyPayPaymentAsync<CreditCardPayment>(
                easyPayPaymentId,
                transactionKey,
                easyPayTransactionId,
                captureDate,
                2.5f,
                2.5f,
                0,
                0,
                0,
                2.5f,
                this.fixture.Configuration);
            var secondResult = await this.donationRepository.CompleteEasyPayPaymentAsync<CreditCardPayment>(
                easyPayPaymentId,
                transactionKey,
                easyPayTransactionId,
                captureDate,
                2.5f,
                2.5f,
                0,
                0,
                0,
                2.5f,
                this.fixture.Configuration);

            Assert.True(firstResult.DonationId > 0);
            Assert.Equal(firstResult.DonationId, secondResult.DonationId);
            int subscriptionDonationCount = await this.context.SubscriptionDonations.CountAsync(
                link => link.Subscription.TransactionKey == transactionKey);
            int paymentCount = await this.context.Payments.CountAsync(
                payment => payment.TransactionKey == transactionKey);
            Assert.Equal(2, subscriptionDonationCount);
            Assert.Equal(1, paymentCount);
            var payment = await this.context.CreditCardPayments
                .FirstAsync(payment => payment.TransactionKey == transactionKey);
            Assert.Equal(easyPayPaymentId, payment.EasyPayPaymentId);
            Assert.NotEqual(easyPayTransactionId, payment.EasyPayPaymentId);
            Assert.Equal(PaymentStatus.Payed, seed.InitialDonation.PaymentStatus);
        }

        /// <summary>
        /// A local EasyPay payment with zero amounts cannot become a confirmed payment.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task TryCompleteDonationPayment_RejectsZeroValueEasyPayPayment()
        {
            var donation = await this.context.Donations.FirstAsync(donation => donation.Id == this.fixture.DonationId);
            donation.PaymentStatus = PaymentStatus.WaitingPayment;
            donation.ConfirmedPayment = null;
            var payment = new CreditCardPayment
            {
                Donation = donation,
                Requested = 0,
                Paid = 0,
                Status = "Success",
                TransactionKey = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
            };
            this.context.CreditCardPayments.Add(payment);
            await this.context.SaveChangesAsync();

            bool completed = this.donationRepository.TryCompleteDonationPayment(
                donation,
                payment,
                requested: 0,
                paid: 0,
                trustProviderPaidStatus: true);

            Assert.False(completed);
            var updated = await this.context.Donations
                .AsNoTracking()
                .FirstAsync(value => value.Id == donation.Id);
            Assert.Equal(PaymentStatus.WaitingPayment, updated.PaymentStatus);
            Assert.Null(updated.ConfirmedPayment);
        }

        private static class SubscriptionRepositoryTestHelpers
        {
            public static async Task<(LocalSubscription Subscription, Donation InitialDonation)> SeedSubscriptionAsync(
                ApplicationDbContext context,
                string transactionKey,
                DateTime initialDonationDate)
            {
                var initialDonation = new Donation
                {
                    PublicId = Guid.NewGuid(),
                    DonationDate = initialDonationDate,
                    DonationAmount = 2.5,
                    FoodBank = await context.FoodBanks.FirstAsync(),
                    PaymentStatus = PaymentStatus.Payed,
                    DonationItems = new List<DonationItem>(),
                    PaymentList = new List<BasePayment>(),
                };
                var subscription = new LocalSubscription
                {
                    TransactionKey = transactionKey,
                    InitialDonation = initialDonation,
                    Status = SubscriptionStatus.Active,
                    Created = DateTime.UtcNow,
                    EasyPaySubscriptionId = Guid.NewGuid().ToString(),
                };
                context.Donations.Add(initialDonation);
                context.Subscriptions.Add(subscription);
                context.SubscriptionDonations.Add(new SubscriptionDonations
                {
                    Donation = initialDonation,
                    Subscription = subscription,
                });
                await context.SaveChangesAsync();
                return (subscription, initialDonation);
            }
        }
    }
}
