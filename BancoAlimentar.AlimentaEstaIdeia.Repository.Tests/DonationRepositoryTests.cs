// -----------------------------------------------------------------------
// <copyright file="DonationRepositoryTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Services;
    using Easypay.Rest.Client.Api;
    using Easypay.Rest.Client.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// This class defines unit tests for donation repository.
    /// </summary>
    public class DonationRepositoryTests : IClassFixture<ServicesFixture>
    {
        private readonly ServicesFixture fixture;
        private readonly DonationRepository donationRepository;
        private readonly ApplicationDbContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="DonationRepositoryTests"/> class.
        /// </summary>
        /// <param name="servicesFixture">Service list.</param>
        public DonationRepositoryTests(ServicesFixture servicesFixture)
        {
            this.fixture = servicesFixture;
            this.donationRepository = this.fixture.ServiceProvider.GetRequiredService<DonationRepository>();
            this.context = this.fixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        }

        /// <summary>
        /// Get total donation test.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanGetTotalDonations()
        {
            await this.fixture.CreateTestDonation(this.context);
            var items = await this.context.ProductCatalogues.ToListAsync();
            var result = this.donationRepository.GetTotalDonations(items);
            Assert.Equal(1, result.First().Total);
        }

        /// <summary>
        /// Claim an existing donation to a user.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanClaimDonationToUser()
        {
            var user = new WebUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = "newtest@newtest.com",
                FullName = "New Test User",
            };
            await this.fixture.UserManager.CreateAsync(user);

            var result = this.donationRepository.ClaimDonationToUser(this.fixture.PublicId, user);
            Assert.True(result);
        }

        /// <summary>
        /// Claiming an existing donation when the PublicID is not a Guid.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanNotClaimDonationToUserWhenPublicIdIsNotGuid()
        {
            var user = new WebUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = "newtest@newtest.com",
                FullName = "New Test User",
            };
            await this.fixture.UserManager.CreateAsync(user);

            var result = this.donationRepository.ClaimDonationToUser("notguidvalue", user);
            Assert.False(result);
        }

        /// <summary>
        /// Claiming an existing donation when the PublicID is empty.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanNotClaimDonationToUserWithEmptyPublicId()
        {
            var user = new WebUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = "newtest@newtest.com",
                FullName = "New Test User",
            };
            await this.fixture.UserManager.CreateAsync(user);

            var result = this.donationRepository.ClaimDonationToUser(string.Empty, user);
            Assert.False(result);
        }

        /// <summary>
        /// Get donation from public Id.
        /// </summary>
        [Fact]
        public void CanGetDonationFromPublicId()
        {
            var result = this.donationRepository.GetDonationIdFromPublicId(new Guid(this.fixture.PublicId));
            Assert.Equal(this.fixture.DonationId, result);
        }

        /// <summary>
        /// Returns zero when no donation matches the public id.
        /// </summary>
        [Fact]
        public void GetDonationIdFromPublicId_ReturnsZeroWhenNotFound()
        {
            var result = this.donationRepository.GetDonationIdFromPublicId(Guid.NewGuid());
            Assert.Equal(0, result);
        }

        /// <summary>
        /// Get donation from the Easypay transaction key.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanGetDonationFromTransactionKey()
        {
            await this.fixture.CreateTestDonation(this.context);

            var result = this.donationRepository.GetDonationIdFromPaymentTransactionId(this.fixture.TransactionKey);
            Assert.Equal(this.fixture.DonationId, result);
        }

        /// <summary>
        /// Update credit card payment flow.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanUpdateCreditCardPayment()
        {
            var result = this.donationRepository.UpdatePaymentStatus<CreditCardPayment>(new Guid(this.fixture.PublicId), SinglePaymentStatus.Pending);
            Assert.True(result);

            var donation = await this.context.Donations.FirstOrDefaultAsync(d => d.Id == this.fixture.DonationId);
            Assert.Equal(PaymentStatus.WaitingPayment, donation.PaymentStatus);

            result = this.donationRepository.UpdatePaymentStatus<CreditCardPayment>(new Guid(this.fixture.PublicId), SinglePaymentStatus.Failed);
            Assert.True(result);

            donation = await this.context.Donations.FirstOrDefaultAsync(d => d.Id == this.fixture.DonationId);
            Assert.Equal(PaymentStatus.ErrorPayment, donation.PaymentStatus);

            result = this.donationRepository.UpdatePaymentStatus<CreditCardPayment>(new Guid(this.fixture.PublicId), SinglePaymentStatus.Paid);
            Assert.True(result);

            donation = await this.context.Donations.FirstOrDefaultAsync(d => d.Id == this.fixture.DonationId);
            Assert.Equal(PaymentStatus.Payed, donation.PaymentStatus);
        }

        /// <summary>
        /// Update payment status.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanUpdateDonationPaymentId()
        {
            var donation = await this.context.Donations.FirstOrDefaultAsync(d => d.Id == this.fixture.DonationId);
            var payments = donation.PaymentList;
            var result = this.donationRepository.UpdateDonationPaymentId(donation, "COMPLETED", "somerandomtoken", "12345");
            Assert.True(result);

            // Add payments back
            donation.PaymentList = payments;
            await this.context.SaveChangesAsync();
        }

        /// <summary>
        /// Update payment when the donation has no payments.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanUpdateDonationPaymentIdWhenDonationHasNoPayments()
        {
            await this.fixture.CreateTestDonation(this.context);

            var donation = await this.context.Donations.FirstOrDefaultAsync(d => d.Id == this.fixture.DonationId);
            var payments = donation.PaymentList;
            donation.PaymentList = null;
            var result = this.donationRepository.UpdateDonationPaymentId(donation, "COMPLETED", "somerandomtoken", "12345");
            Assert.True(result);

            // Add payments back
            donation.PaymentList = payments;
            await this.context.SaveChangesAsync();
        }

        /// <summary>
        /// Donation is not updated when donation is null.
        /// </summary>
        [Fact]
        public void CanNotUpdateDonationPaymentIdWhenDonationIsNull()
        {
            var result = this.donationRepository.UpdateDonationPaymentId(null, "COMPLETED", "somerandomtoken", "12345");
            Assert.False(result);
        }

        /// <summary>
        /// PayPal update fails when the token is missing.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task UpdateDonationPaymentId_ReturnsFalse_WhenTokenIsEmpty()
        {
            var donation = await this.context.Donations.FirstOrDefaultAsync(d => d.Id == this.fixture.DonationId);
            var result = this.donationRepository.UpdateDonationPaymentId(donation, "COMPLETED", string.Empty, "12345");
            Assert.False(result);
        }

        /// <summary>
        /// PayPal update reuses an existing payment row for the same token.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task UpdateDonationPaymentId_UpdatesExistingPayPalPayment_ByToken()
        {
            await this.fixture.CreateTestDonation(this.context);
            var donation = await this.context.Donations.FirstOrDefaultAsync(d => d.Id == this.fixture.DonationId);
            const string token = "paypal-token-123";
            Assert.True(this.donationRepository.UpdateDonationPaymentId(donation, "CREATED", token, null));

            var existingPaymentId = donation.ConfirmedPayment.Id;
            Assert.True(this.donationRepository.UpdateDonationPaymentId(donation, "COMPLETED", token, "payer-99"));

            var payment = await this.context.PayPalPayments.FirstAsync(p => p.PayPalPaymentId == token);
            Assert.Equal(existingPaymentId, payment.Id);
            Assert.Equal("COMPLETED", payment.Status);
            Assert.Equal("payer-99", payment.PayerId);
            Assert.NotNull(payment.Completed);
        }

        /// <summary>
        /// Updated multibanco payment.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanUpdateMultiBankPayment()
        {
            await this.fixture.CreateTestDonation(this.context);

            var donation = await this.context.Donations.FirstOrDefaultAsync(d => d.Id == this.fixture.DonationId);
            var payments = donation.PaymentList;
            donation.PaymentList = null;
            var result = this.donationRepository.UpdateMultiBankPayment(donation, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "entity", "refrence");
            Assert.True(result);

            // Add payments back
            donation.PaymentList = payments;
            await this.context.SaveChangesAsync();
        }

        /// <summary>
        /// Update multibanco payment.
        /// </summary>
        [Fact]
        public void CanNotCanUpdateMultiBankPaymentWhenDonationIsNull()
        {
            var result = this.donationRepository.UpdateMultiBankPayment(null, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "entity", "refrence");
            Assert.False(result);
        }

        /// <summary>
        /// Update multibanco payment when donation has no payments.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanUpdateMultiBankPaymentWhenDonationHasNoPayments()
        {
            await this.fixture.CreateTestDonation(this.context);

            var donation = await this.context.Donations.FirstOrDefaultAsync(d => d.Id == this.fixture.DonationId);
            var payments = donation.PaymentList;
            donation.PaymentList = null;
            var result = this.donationRepository.UpdateMultiBankPayment(donation, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "entity", "refrence");
            Assert.True(result);

            // Add payments back
            donation.PaymentList = payments;
            await this.context.SaveChangesAsync();
        }

        /// <summary>
        /// Update payment transaction to payed.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CanUpdatePaymentTransactionToPayed()
        {
            await this.fixture.CreateTestDonation(this.context);
            (int basePaymentId, int donationId) = this.donationRepository.UpdatePaymentTransaction(string.Empty, this.fixture.TransactionKey, NotificationGeneric.StatusEnum.Success, string.Empty);
            Assert.True(basePaymentId > 0);

            var donation = this.context.Payments
                    .Where(p => p.TransactionKey == this.fixture.TransactionKey)
                    .Select(p => p.Donation)
                    .FirstOrDefault();

            Assert.NotNull(donation);
            Assert.Equal(PaymentStatus.Payed, donation.PaymentStatus);
        }

        /// <summary>
        /// Update payment transaction to error payment.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanUpdatePaymentTransactionToErrorPayment()
        {
            await this.fixture.CreateTestDonation(this.context);

            // reset the payment status to waiting.
            Donation donation = this.context.Payments
                    .Where(p => p.TransactionKey == this.fixture.TransactionKey)
                    .Select(p => p.Donation)
                    .FirstOrDefault();
            donation.PaymentStatus = PaymentStatus.WaitingPayment;
            this.context.SaveChanges();

            // update the payment
            (int basePaymentId, int donationId) = this.donationRepository.UpdatePaymentTransaction(string.Empty, this.fixture.TransactionKey, NotificationGeneric.StatusEnum.Failed, string.Empty);
            Assert.True(basePaymentId > 0);

            donation = this.context.Payments
                    .Where(p => p.TransactionKey == this.fixture.TransactionKey)
                    .Select(p => p.Donation)
                    .FirstOrDefault();

            Assert.NotNull(donation);
            Assert.Equal(PaymentStatus.ErrorPayment, donation.PaymentStatus);

            // Update the status back to payed.
            donation.PaymentStatus = PaymentStatus.Payed;
            await this.context.SaveChangesAsync();
        }

        /// <summary>
        /// Update transaction when transaction key is null is empy.
        /// </summary>
        [Fact]
        public void CanNotUpdatePaymentTransactionWhenTransactionKeyIsInvalid()
        {
            (int basePaymentId, int donationId) = this.donationRepository.UpdatePaymentTransaction(string.Empty, "wrong-transaction-key", NotificationGeneric.StatusEnum.Failed, string.Empty);
            Assert.True(basePaymentId == 0);
        }

        /// <summary>
        /// Multibanco payment is completed.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanCompleteMultiBankPayment()
        {
            var testTransactionKey = Guid.NewGuid().ToString();
            var donation = await this.context.Donations.FirstOrDefaultAsync(d => d.Id == this.fixture.DonationId);
            var payments = donation.PaymentList;
            donation.PaymentList = null;
            this.donationRepository.UpdateMultiBankPayment(donation, Guid.NewGuid().ToString(), testTransactionKey, "entity", "refrence");

            float amount = (float)donation.DonationAmount;
            var result = await this.donationRepository.CompleteEasyPayPaymentAsync<MultiBankPayment>(
                "easypayid",
                testTransactionKey,
                "easypayment-transactionid",
                DateTime.Now,
                amount,
                amount,
                0,
                0,
                0,
                0,
                this.fixture.Configuration);
            Assert.True(result.DonationId > 0);

            // Add payments back
            donation.PaymentList = payments;
            await this.context.SaveChangesAsync();
        }

        /// <summary>
        /// Can't update multibanco payment when trasaction key is invalid.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanNotCompleteMultiBankPaymentWithInValidTransactionKey()
        {
            var testTransactionKey = Guid.NewGuid().ToString();
            var donation = await this.context.Donations.FirstOrDefaultAsync(d => d.Id == this.fixture.DonationId);
            var payments = donation.PaymentList;
            donation.PaymentList = null;
            this.donationRepository.UpdateMultiBankPayment(donation, Guid.NewGuid().ToString(), this.fixture.TransactionKey, "entity", "refrence");

            var result = await this.donationRepository.CompleteEasyPayPaymentAsync<MultiBankPayment>(
                Guid.NewGuid().ToString(),
                testTransactionKey,
                "easypayment-transactionid",
                DateTime.Now,
                10,
                10,
                0,
                0,
                0,
                0,
                this.fixture.Configuration);
            Assert.True(result.DonationId == 0);

            // Add payments back
            donation.PaymentList = payments;
            await this.context.SaveChangesAsync();
        }

        /// <summary>
        /// Create a new MBWay payment.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanCreateMBWayPayment()
        {
            var donation = await this.context.Donations.FirstOrDefaultAsync(d => d.Id == this.fixture.DonationId);
            var payments = donation.PaymentList;
            donation.PaymentList = null;

            var result = this.donationRepository.CreateMBWayPayment(donation, "easypayid", this.fixture.TransactionKey, "alias");
            Assert.True(result);

            // Add payments back
            donation.PaymentList = payments;
            await this.context.SaveChangesAsync();
        }

        /// <summary>
        /// Can not create MBWay payment when the donation is null.
        /// </summary>
        [Fact]
        public void CanNotCreateMBWayPaymentWhenDonationIsNull()
        {
            var result = this.donationRepository.CreateMBWayPayment(null, "easypayid", this.fixture.TransactionKey, "alias");
            Assert.False(result);
        }

        /// <summary>
        /// Create credit card payment.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanCreateCreditCardPayment()
        {
            var donation = await this.context.Donations.FirstOrDefaultAsync(d => d.Id == this.fixture.DonationId);
            var payments = donation.PaymentList;
            donation.PaymentList = null;

            var result = this.donationRepository.CreateCreditCardPaymnet(donation, "easypayid", this.fixture.TransactionKey, "url", DateTime.Now);
            Assert.True(result);

            // Add payments back
            donation.PaymentList = payments;
            await this.context.SaveChangesAsync();
        }

        /// <summary>
        /// Can not create credit card payment when the donation id is null.
        /// </summary>
        [Fact]
        public void CanNotCreateCreditCardPaymentWhenDonationIsNull()
        {
            var result = this.donationRepository.CreateCreditCardPaymnet(null, "easypayid", this.fixture.TransactionKey, "url", DateTime.Now);
            Assert.False(result);
        }

        /// <summary>
        /// Complete credit card payment.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanCompleteCreditCardPayment()
        {
            var donation = await this.context.Donations.FirstOrDefaultAsync(d => d.Id == this.fixture.DonationId);
            var payments = donation.PaymentList;
            donation.PaymentList = null;
            var testTransactionKey = Guid.NewGuid().ToString();
            this.donationRepository.CreateCreditCardPaymnet(donation, "easypayid", testTransactionKey, "url", DateTime.Now);

            float amount = (float)donation.DonationAmount;
            var result = await this.donationRepository.CompleteEasyPayPaymentAsync<CreditCardPayment>(
                "easypayid",
                testTransactionKey,
                "easypayment-transactionid",
                DateTime.Now,
                amount,
                amount,
                0,
                0,
                0,
                0,
                this.fixture.Configuration);

            Assert.True(result.DonationId > 0);

            // Add payments back
            donation.PaymentList = payments;
            await this.context.SaveChangesAsync();
        }

        /// <summary>
        /// Can not complete credit card payment with wrong transaction key.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanNotCompleteCreditCardPaymentWithWrongTransactionKey()
        {
            var donation = await this.context.Donations.FirstOrDefaultAsync(d => d.Id == this.fixture.DonationId);
            var payments = donation.PaymentList;
            donation.PaymentList = null;
            var testTransactionKey = Guid.NewGuid().ToString();
            this.donationRepository.CreateCreditCardPaymnet(donation, "easypayid", testTransactionKey, "url", DateTime.Now);

            var result = await this.donationRepository.CompleteEasyPayPaymentAsync<CreditCardPayment>(
                "easypayid",
                "wrong-transaction-key",
                "easypayment-transactionid",
                DateTime.Now,
                10,
                10,
                0,
                0,
                0,
                0,
                this.fixture.Configuration);

            Assert.True(result.DonationId == 0);

            // Add payments back
            donation.PaymentList = payments;
            await this.context.SaveChangesAsync();
        }

        /// <summary>
        /// Can not complete credit card payment with wrong transaction key.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanNotCompleteCreditCardPaymentWithMisMatchedTransactionKey()
        {
            var donation = await this.context.Donations.FirstOrDefaultAsync(d => d.Id == this.fixture.DonationId);
            var payments = donation.PaymentList;
            donation.PaymentList = null;
            var testTransactionKey = Guid.NewGuid().ToString();
            this.donationRepository.CreateCreditCardPaymnet(donation, "easypayid", testTransactionKey, "url", DateTime.Now);

            var result = await this.donationRepository.CompleteEasyPayPaymentAsync<CreditCardPayment>(
                "easypayid",
                Guid.NewGuid().ToString(),
                "easypayment-transactionid",
                DateTime.Now,
                10,
                10,
                0,
                0,
                0,
                0,
                this.fixture.Configuration);

            Assert.True(result.DonationId == 0);

            // Add payments back
            donation.PaymentList = payments;
            await this.context.SaveChangesAsync();
        }

        /// <summary>
        /// Complete MBway payment.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanCompleteMBWayPayment()
        {
            var testTransactionKey = Guid.NewGuid().ToString();
            var donation = await this.context.Donations.FirstOrDefaultAsync(d => d.Id == this.fixture.DonationId);
            var payments = donation.PaymentList;
            donation.PaymentList = null;

            this.donationRepository.CreateMBWayPayment(donation, "easypayid", testTransactionKey, "alias");

            float amount = (float)donation.DonationAmount;
            var result = await this.donationRepository.CompleteEasyPayPaymentAsync<MBWayPayment>(
                "easypayid",
                testTransactionKey,
                "easypayment-transactionid",
                DateTime.Now,
                amount,
                amount,
                0,
                0,
                0,
                0,
                this.fixture.Configuration);

            Assert.True(result.DonationId > 0);

            // Add payments back
            donation.PaymentList = payments;
            await this.context.SaveChangesAsync();
        }

        /// <summary>
        /// Can not complete MBWay payment with wrong transaction key.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanNotCompleteMBWayPaymentWithWrongTransactionKey()
        {
            var testTransactionKey = Guid.NewGuid().ToString();
            var donation = await this.context.Donations.FirstOrDefaultAsync(d => d.Id == this.fixture.DonationId);
            var payments = donation.PaymentList;
            donation.PaymentList = null;

            this.donationRepository.CreateMBWayPayment(donation, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "alias");

            var result = await this.donationRepository.CompleteEasyPayPaymentAsync<MBWayPayment>(
                "easypayid",
                testTransactionKey,
                "easypayment-transactionid",
                DateTime.Now,
                10,
                10,
                0,
                0,
                0,
                0,
                this.fixture.Configuration);

            Assert.True(result.DonationId == 0);

            // Add payments back
            donation.PaymentList = payments;
            await this.context.SaveChangesAsync();
        }

        /// <summary>
        /// Can not complete MBWay payment with mismatched transaction key.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanNotCompleteMBWayPaymentWithMisMatchedTransactionKey()
        {
            var testTransactionKey = Guid.NewGuid().ToString();
            var donation = await this.context.Donations.FirstOrDefaultAsync(d => d.Id == this.fixture.DonationId);
            var payments = donation.PaymentList;
            donation.PaymentList = null;

            this.donationRepository.CreateMBWayPayment(donation, "easypayid", Guid.NewGuid().ToString(), "alias");

            var result = await this.donationRepository.CompleteEasyPayPaymentAsync<MBWayPayment>(
                "easypayid",
                testTransactionKey,
                "easypayment-transactionid",
                DateTime.Now,
                10,
                10,
                0,
                0,
                0,
                0,
                this.fixture.Configuration);

            Assert.True(result.DonationId == 0);

            // Add payments back
            donation.PaymentList = payments;
            await this.context.SaveChangesAsync();
        }

        /// <summary>
        /// Gets multibanco payments.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanGetCurrentMultiBankPayment()
        {
            var testTransactionKey = Guid.NewGuid().ToString();
            var donation = await this.context.Donations.FirstOrDefaultAsync(d => d.Id == this.fixture.DonationId);
            var payments = donation.PaymentList;
            donation.PaymentList = null;

            this.donationRepository.UpdateMultiBankPayment(donation, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "entity", "refrence");

            var result = this.donationRepository.GetCurrentMultiBankPayment(donation.Id);
            Assert.NotNull(result);

            // Add payments back
            donation.PaymentList = payments;
            await this.context.SaveChangesAsync();
        }

        /// <summary>
        /// Can not get current multibanco payment with wrong donation id.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanNotGetCurrentMultiBankPaymentForWrongDonationId()
        {
            var testTransactionKey = Guid.NewGuid().ToString();
            var donation = await this.context.Donations.FirstOrDefaultAsync(d => d.Id == this.fixture.DonationId);
            var payments = donation.PaymentList;
            donation.PaymentList = null;

            this.donationRepository.UpdateMultiBankPayment(donation, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "entity", "refrence");

            var result = this.donationRepository.GetCurrentMultiBankPayment(4000);
            Assert.Null(result);

            // Add payments back
            donation.PaymentList = payments;
            await this.context.SaveChangesAsync();
        }

        /// <summary>
        /// Get full donation by id.
        /// </summary>
        [Fact]
        public void CanGetFullDonationById()
        {
            var result = this.donationRepository.GetFullDonationById(this.fixture.DonationId);

            Assert.NotNull(result);
            Assert.NotNull(result.User);
            Assert.NotNull(result.DonationItems);
            Assert.NotNull(result.FoodBank);
            Assert.NotNull(result.ConfirmedPayment);
        }

        /// <summary>
        /// Get payments for a particular donation.
        /// </summary>
        [Fact]
        public void CanGetPaymentsForDonation()
        {
            var result = this.donationRepository.GetPaymentsForDonation(this.fixture.DonationId);

            Assert.NotNull(result);
        }

        /// <summary>
        /// Get user donations.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CanGetUserDonation()
        {
            await this.fixture.CreateTestDonation(this.context);
            var result = this.donationRepository.GetUserDonation(this.fixture.UserId);

            Assert.NotNull(result);

            var donation = result.FirstOrDefault();
            Assert.NotNull(donation.DonationItems);
            Assert.NotNull(donation.FoodBank);
            Assert.NotNull(donation.PaymentList);
        }

        /// <summary>
        /// Check payment types.
        /// </summary>
        [Fact]
        public void CanGetPaymentType()
        {
            var result = this.donationRepository.GetPaymentType(new PayPalPayment());
            Assert.True(result == PaymentType.Paypal);
            result = this.donationRepository.GetPaymentType(new CreditCardPayment());
            Assert.True(result == PaymentType.CreditCard);
            result = this.donationRepository.GetPaymentType(new MBWayPayment());
            Assert.True(result == PaymentType.MBWay);
            result = this.donationRepository.GetPaymentType(new MultiBankPayment());
            Assert.True(result == PaymentType.MultiBanco);
            result = this.donationRepository.GetPaymentType(null);
            Assert.True(result == PaymentType.None);
        }

        /// <summary>
        /// Returns human-readable payment method names.
        /// </summary>
        [Fact]
        public void CanGetPaymentHumanName()
        {
            Assert.Equal("Paypal", this.donationRepository.GetPaymentHumanName(new PayPalPayment()));
            Assert.Equal("Cartão de Crédito", this.donationRepository.GetPaymentHumanName(new CreditCardPayment()));
            Assert.Equal("MBWay", this.donationRepository.GetPaymentHumanName(new MBWayPayment()));
            Assert.Equal("Multibanco", this.donationRepository.GetPaymentHumanName(new MultiBankPayment()));
            Assert.Equal("desconhecido", this.donationRepository.GetPaymentHumanName(null));
        }

        /// <summary>
        /// Detects when a transaction key belongs to a subscription.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CanDetectSubscriptionTransactionKey()
        {
            Assert.False(this.donationRepository.IsTransactionKeySubcriptionBased(this.fixture.TransactionKey));

            var user = await this.context.WebUser.FirstAsync(u => u.Id == this.fixture.UserId);
            this.context.Subscriptions.Add(new BancoAlimentar.AlimentaEstaIdeia.Model.Subscription
            {
                TransactionKey = "subscription-tx-key",
                User = user,
                PublicId = Guid.NewGuid(),
                Created = DateTime.UtcNow,
                StartTime = DateTime.UtcNow,
                ExpirationTime = DateTime.UtcNow.AddYears(1),
                Frequency = "1M",
                Status = SubscriptionStatus.Active,
            });
            await this.context.SaveChangesAsync();

            Assert.True(this.donationRepository.IsTransactionKeySubcriptionBased("subscription-tx-key"));
            Assert.False(this.donationRepository.IsTransactionKeySubcriptionBased(string.Empty));
        }

        /// <summary>
        /// Resolves donation id from easypay transaction key guid.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanGetDonationByTransactionKey()
        {
            await this.fixture.CreateTestDonation(this.context);
            var transactionGuid = new Guid(this.fixture.TransactionKey);
            int donationId = this.donationRepository.GetDonationByTransactionKey(transactionGuid);

            Assert.Equal(this.fixture.DonationId, donationId);
        }

        /// <summary>
        /// Finds a payment of the requested type for a donation.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanFindPaymentByType()
        {
            await this.fixture.CreateTestDonation(this.context);

            var creditCard = this.donationRepository.FindPaymentByType<CreditCardPayment>(this.fixture.DonationId);
            var mbWay = this.donationRepository.FindPaymentByType<MBWayPayment>(this.fixture.DonationId);

            Assert.NotNull(creditCard);
            Assert.Null(mbWay);
        }

        /// <summary>
        /// Aggregates paid donation totals for a food bank.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CanGetTotalDonationsOfFoodBank()
        {
            await this.fixture.CreateTestDonation(this.context);
            var items = await this.context.ProductCatalogues.ToListAsync();
            var donation = await this.context.Donations
                .Include(d => d.FoodBank)
                .FirstAsync(d => d.Id == this.fixture.DonationId);

            var result = this.donationRepository.GetTotalDonationsOfFoodBank(items, donation.FoodBank.Id);

            Assert.NotEmpty(result);
            Assert.Contains(result, r => r.Total > 0);
        }

        /// <summary>
        /// Sums paid cash donation line amounts.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CanGetTotalCashDonation()
        {
            await this.fixture.CreateTestDonation(this.context);
            var cashProduct = await this.context.ProductCatalogues
                .FirstAsync(p => p.Name == ProductCatalogue.CashProductCatalogName);
            var donation = await this.context.Donations.FirstAsync(d => d.Id == this.fixture.DonationId);
            donation.DonationItems.Add(new DonationItem
            {
                Donation = donation,
                ProductCatalogue = cashProduct,
                Price = 15.0,
                Quantity = 1,
            });
            await this.context.SaveChangesAsync();

            var result = this.donationRepository.GetTotalCashDonation(cashProduct);

            Assert.Equal(15.0, result.Total);
        }

        /// <summary>
        /// Clones a donation with a new public id and waiting payment status.
        /// </summary>
        [Fact]
        public void CanCloneDonation()
        {
            var source = this.donationRepository.GetFullDonationById(this.fixture.DonationId);
            var clone = this.donationRepository.CloneDonation(source);

            Assert.NotEqual(source.PublicId, clone.PublicId);
            Assert.Equal(PaymentStatus.WaitingPayment, clone.PaymentStatus);
            Assert.Equal(source.DonationAmount, clone.DonationAmount);
            Assert.Equal(source.DonationItems.Count, clone.DonationItems.Count);
            Assert.Equal(0, clone.Id);
        }

        /// <summary>
        /// Deletes donation items and confirmed payment for a donation.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CanDeleteDonation()
        {
            var user = await this.context.WebUser.FirstAsync(u => u.Id == this.fixture.UserId);
            var foodBank = await this.context.FoodBanks.FirstAsync();
            var product = await this.context.ProductCatalogues.FirstAsync();
            var donation = new Donation
            {
                PublicId = Guid.NewGuid(),
                DonationAmount = 10,
                DonationDate = DateTime.UtcNow,
                FoodBank = foodBank,
                User = user,
                PaymentStatus = PaymentStatus.Payed,
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
            this.context.Donations.Add(donation);
            await this.context.SaveChangesAsync();

            int disposableDonationId = donation.Id;
            this.donationRepository.DeleteDonation(disposableDonationId);

            Assert.Empty(await this.context.DonationItems.Where(i => i.Donation.Id == disposableDonationId).ToListAsync());
        }

        /// <summary>
        /// Deletes a multibanco payment by easypay id.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CanDeletePayment()
        {
            var donation = await this.context.Donations.FirstAsync(d => d.Id == this.fixture.DonationId);
            string easypayId = Guid.NewGuid().ToString();
            var payment = new MultiBankPayment
            {
                Created = DateTime.UtcNow,
                TransactionKey = Guid.NewGuid().ToString(),
                EasyPayPaymentId = easypayId,
                Donation = donation,
            };
            this.context.Payments.Add(payment);
            await this.context.SaveChangesAsync();

            this.donationRepository.DeletePayment(easypayId);

            Assert.Null(await this.context.Payments.FindAsync(payment.Id));
        }

        /// <summary>
        /// Tests the easypay API.
        /// </summary>
        /// <returns>A task.</returns>
        [Fact]
        public async Task EasyPayTest()
        {
            IUnitOfWork context = this.fixture.ServiceProvider.GetRequiredService<IUnitOfWork>();
            EasyPayBuilder easypayBuilder = this.fixture.ServiceProvider.GetRequiredService<EasyPayBuilder>();
            Donation temporalDonation = this.CreateTemporalDonation(context);

            InlineObject5 targetPayment = null;

            try
            {
                SinglePostRequest paymentRequest = new SinglePostRequest()
                {
                    Key = temporalDonation.Id.ToString(),
                    Type = OperationType.Sale,
                    Currency = Currency.EUR,
                    Customer = new Customer()
                    {
                        Name = temporalDonation.User.UserName,
                        Email = temporalDonation.User.Email,
                        Phone = temporalDonation.User.PhoneNumber,
                        FiscalNumber = temporalDonation.User.Nif,
                        Language = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName,
                        Key = temporalDonation.User.Id,
                    },
                    Value = (float)temporalDonation.DonationAmount,
                    Method = SinglePaymentMethods.Mb,
                    Capture = new CreateCapture(
                        transactionKey: Guid.NewGuid().ToString(),
                        descriptive: "AlimentaEstaideapayment"),
                };

                ISinglePaymentApi easyPayApiClient = easypayBuilder.GetSinglePaymentApi();
                targetPayment = await easyPayApiClient.SinglePostAsync(paymentRequest);

                temporalDonation.ServiceEntity = targetPayment.Method.Entity.ToString();
                temporalDonation.ServiceReference = targetPayment.Method.Reference;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                context.DonationItem.RemoveRange(temporalDonation.DonationItems);
                context.Donation.Remove(temporalDonation);
                int affectedRows = context.Complete();
                Assert.True(affectedRows > 0);
            }

            Assert.True(targetPayment.Status == ResponseStatus.Ok, "Payment was not successfull");
            Assert.False(string.IsNullOrEmpty(targetPayment.Id), "No payment Id returned");
            Assert.True(targetPayment.Message.Count > 0, "No payment status message returned");
            Assert.True(targetPayment.Message[0] == "Your request was successfully created", $"Not success message: {targetPayment.Message[0]}");
            Assert.True(targetPayment.Method != null, "No return method for created single payment");

            Assert.True(targetPayment.Method.Type == "mb", "Type of new single payment Method is not mb");
            Assert.True(targetPayment.Method.Status == "pending", "New single payment Method Status not pending");
            Assert.False(string.IsNullOrEmpty(targetPayment.Method.Entity), "New single payment Method (Mb) Entity not valid");
            Assert.True(targetPayment.Method.Reference != null, "New single payment Method (Mb) Reference not valid");
        }

        /// <summary>
        /// Sums paid donations for a user.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanGetTotalUserDonations()
        {
            string userId = Guid.NewGuid().ToString();
            var user = new WebUser
            {
                Id = userId,
                Email = $"totals-{userId}@example.com",
                UserName = $"totals-{userId}@example.com",
                NormalizedEmail = $"TOTALS-{userId}@EXAMPLE.COM",
            };
            this.context.WebUser.Add(user);
            var foodBank = await this.context.FoodBanks.FirstAsync();
            var product = await this.context.ProductCatalogues.FirstAsync();
            var olderDate = DateTime.UtcNow.AddDays(-5);
            var olderDonation = new Donation
            {
                PublicId = Guid.NewGuid(),
                DonationAmount = 4,
                DonationDate = olderDate,
                FoodBank = foodBank,
                User = user,
                PaymentStatus = PaymentStatus.Payed,
                DonationItems = new List<DonationItem>(),
            };
            olderDonation.DonationItems.Add(new DonationItem
            {
                Donation = olderDonation,
                ProductCatalogue = product,
                Quantity = 1,
                Price = product.Cost,
            });
            var recentDonation = new Donation
            {
                PublicId = Guid.NewGuid(),
                DonationAmount = 6,
                DonationDate = DateTime.UtcNow,
                FoodBank = foodBank,
                User = user,
                PaymentStatus = PaymentStatus.Payed,
                DonationItems = new List<DonationItem>(),
            };
            recentDonation.DonationItems.Add(new DonationItem
            {
                Donation = recentDonation,
                ProductCatalogue = product,
                Quantity = 1,
                Price = product.Cost,
            });
            this.context.Donations.Add(olderDonation);
            this.context.Donations.Add(recentDonation);
            this.context.Donations.Add(new Donation
            {
                PublicId = Guid.NewGuid(),
                DonationAmount = 99,
                DonationDate = DateTime.UtcNow,
                FoodBank = foodBank,
                User = user,
                PaymentStatus = PaymentStatus.WaitingPayment,
            });
            await this.context.SaveChangesAsync();

            (double total, int count, DateTime firstDate) = this.donationRepository.GetTotalUserDonations(userId);

            Assert.Equal(10, total);
            Assert.Equal(2, count);
            Assert.Equal(olderDate, firstDate);
        }

        /// <summary>
        /// Returns zero totals for a user with no paid donations.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task GetTotalUserDonationsReturnsZeroForUserWithoutPaidDonations()
        {
            string userId = Guid.NewGuid().ToString();
            this.context.WebUser.Add(new WebUser
            {
                Id = userId,
                Email = $"empty-{userId}@example.com",
                UserName = $"empty-{userId}@example.com",
                NormalizedEmail = $"EMPTY-{userId}@EXAMPLE.COM",
            });
            await this.context.SaveChangesAsync();

            (double total, int count, DateTime firstDate) = this.donationRepository.GetTotalUserDonations(userId);

            Assert.Equal(0, total);
            Assert.Equal(0, count);
        }

        /// <summary>
        /// Clears cached total donation aggregates.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanInvalidateTotalCache()
        {
            var memoryCache = this.fixture.ServiceProvider.GetRequiredService<IMemoryCache>();
            var items = await this.context.ProductCatalogues.ToListAsync();
            var product = items.First();

            this.donationRepository.GetTotalDonations(items);
            string cacheKey = $"{nameof(TotalDonationsResult)}-{product.Id}";
            Assert.True(memoryCache.TryGetValue(cacheKey, out _));

            this.donationRepository.InvalidateTotalCache();

            Assert.False(memoryCache.TryGetValue(cacheKey, out _));
        }

        /// <summary>
        /// Clears only product-scoped cache keys and leaves food-bank scoped keys intact.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task InvalidateTotalCache_LeavesFoodBankScopedKeys()
        {
            var memoryCache = this.fixture.ServiceProvider.GetRequiredService<IMemoryCache>();
            var items = await this.context.ProductCatalogues.ToListAsync();
            var product = items.First();
            const int foodBankId = 1;

            this.donationRepository.GetTotalDonations(items);
            this.donationRepository.GetTotalDonationsOfFoodBank(items, foodBankId);

            string productCacheKey = $"{nameof(TotalDonationsResult)}-{product.Id}";
            string foodBankCacheKey = $"{foodBankId}-{nameof(TotalDonationsResult)}-{product.Id}";
            Assert.True(memoryCache.TryGetValue(productCacheKey, out _));
            Assert.True(memoryCache.TryGetValue(foodBankCacheKey, out _));

            this.donationRepository.InvalidateTotalCache();

            Assert.False(memoryCache.TryGetValue(productCacheKey, out _));
            Assert.True(memoryCache.TryGetValue(foodBankCacheKey, out _));
        }

        private Donation CreateTemporalDonation(IUnitOfWork context)
        {
            string email = "username@domain.com";
            WebUser user = new WebUser()
            {
                Id = Guid.NewGuid().ToString(),
                Email = email,
                UserName = "Bartholomew Chungus Gingersnap III",
                NormalizedEmail = email.ToUpperInvariant(),
                PhoneNumber = "+34123456789",
                Nif = this.fixture.Nif,
            };

            Donation result = new Donation()
            {
                DonationDate = DateTime.UtcNow,
                DonationItems = new List<DonationItem>(),
                FoodBank = context.FoodBank.GetById(2),
                ReferralEntity = new Referral() { Code = "Testing" },
                DonationAmount = 23,
                User = user,
                WantsReceipt = false,
                PaymentStatus = PaymentStatus.WaitingPayment,
            };

            int count = 0;
            foreach (var item in context.ProductCatalogue.GetAll().ToList())
            {
                result.DonationItems.Add(new DonationItem()
                {
                    Donation = result,
                    ProductCatalogue = item,
                    Price = item.Cost,
                    Quantity = ++count,
                });
            }

            context.Donation.Add(result);
            int affectedRows = context.Complete();
            Assert.True(affectedRows > 0);

            return result;
        }
    }
}
