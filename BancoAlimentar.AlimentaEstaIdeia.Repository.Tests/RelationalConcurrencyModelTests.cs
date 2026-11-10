// -----------------------------------------------------------------------
// <copyright file="RelationalConcurrencyModelTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Common.EasyPay;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Data.Sqlite;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;
    using Xunit;

    /// <summary>
    /// Relational constraint tests for concurrency-sensitive entities.
    /// </summary>
    public class RelationalConcurrencyModelTests
    {
        /// <summary>
        /// Invoice sequence numbers are unique within a year and sequence scope.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task InvoiceSequenceIsUniqueWithinScope()
        {
            await using SqliteConnection connection = await CreateOpenConnectionAsync();
            await using ApplicationDbContext context = CreateContext(connection);
            await context.Database.EnsureCreatedAsync();

            context.Invoices.Add(CreateInvoice(sequence: 1));
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            context.Invoices.Add(CreateInvoice(sequence: 1));

            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }

        /// <summary>
        /// Easypay payment identifiers cannot be persisted more than once.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task EasyPayPaymentIdIsUnique()
        {
            await using SqliteConnection connection = await CreateOpenConnectionAsync();
            await using ApplicationDbContext context = CreateContext(connection);
            await context.Database.EnsureCreatedAsync();
            string paymentId = Guid.NewGuid().ToString();

            context.CreditCardPayments.Add(CreatePayment(paymentId));
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            context.CreditCardPayments.Add(CreatePayment(paymentId));

            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }

        /// <summary>
        /// Concurrent payment completions cannot overwrite the first confirmed payment.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task DonationCompletionUsesOptimisticConcurrency()
        {
            await using SqliteConnection connection = await CreateOpenConnectionAsync();
            await using (ApplicationDbContext seedContext = CreateContext(connection))
            {
                await seedContext.Database.EnsureCreatedAsync();
                var donation = new Donation
                {
                    DonationAmount = 10,
                    DonationDate = DateTime.UtcNow,
                    PaymentStatus = PaymentStatus.WaitingPayment,
                    PublicId = Guid.NewGuid(),
                };
                donation.PaymentList =
                [
                    CreatePayment(Guid.NewGuid().ToString()),
                    CreatePayment(Guid.NewGuid().ToString()),
                ];
                seedContext.Donations.Add(donation);
                await seedContext.SaveChangesAsync();
            }

            await using ApplicationDbContext firstContext = CreateContext(connection);
            await using ApplicationDbContext secondContext = CreateContext(connection);
            Donation firstDonation = await firstContext.Donations
                .Include(donation => donation.PaymentList)
                .SingleAsync();
            Donation secondDonation = await secondContext.Donations
                .Include(donation => donation.PaymentList)
                .SingleAsync();
            BasePayment firstPayment = firstDonation.PaymentList.OrderBy(payment => payment.Id).First();
            BasePayment secondPayment = secondDonation.PaymentList.OrderBy(payment => payment.Id).Last();

            firstDonation.PaymentStatus = PaymentStatus.Payed;
            firstDonation.ConfirmedPayment = firstPayment;
            secondDonation.PaymentStatus = PaymentStatus.Payed;
            secondDonation.ConfirmedPayment = secondPayment;

            await firstContext.SaveChangesAsync();
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondContext.SaveChangesAsync());

            await using ApplicationDbContext verificationContext = CreateContext(connection);
            Donation persisted = await verificationContext.Donations
                .Include(donation => donation.ConfirmedPayment)
                .SingleAsync();
            Assert.Equal(firstPayment.Id, persisted.ConfirmedPayment.Id);
            Assert.Equal(PaymentStatus.Payed, persisted.PaymentStatus);
        }

        /// <summary>
        /// Recurring captures persist both payment links without an insert cycle or duplicate replay.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task SubscriptionCapturePersistsConfirmedPaymentWithoutInsertCycle()
        {
            await using SqliteConnection connection = await CreateOpenConnectionAsync();
            await using ApplicationDbContext context = CreateContext(connection);
            EasyPaySubscriptionPaymentEvidence evidence = await SeedSubscriptionAsync(context);
            using var cache = new MemoryCache(new MemoryCacheOptions());
            using var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
            var repository = new SubscriptionRepository(context, cache, new TelemetryClient(telemetryConfiguration));

            var result = repository.CompleteSubscriptionCapture(evidence);
            Assert.True(result.DonationId > 0);
            context.ChangeTracker.Clear();

            Donation donation = await context.Donations
                .Include(value => value.ConfirmedPayment)
                .Include(value => value.PaymentList)
                .SingleAsync(value => value.Id == result.DonationId);
            var payment = Assert.IsType<CreditCardPayment>(Assert.Single(donation.PaymentList));
            Assert.Same(payment, donation.ConfirmedPayment);
            Assert.Equal(donation.Id, payment.Donation.Id);
            Assert.Equal(PaymentStatus.Payed, donation.PaymentStatus);
            Assert.Equal(evidence.PaymentDate, donation.DonationDate);
            Assert.Equal(evidence.EasypayPaymentId, payment.EasyPayPaymentId);
            Assert.Equal(evidence.PaymentDate, payment.Completed);
            Assert.Equal((float)evidence.Requested, payment.Requested);
            Assert.Equal((float)evidence.Paid, payment.Paid);

            context.ChangeTracker.Clear();
            var replay = repository.CompleteSubscriptionCapture(evidence);

            Assert.Equal(result.DonationId, replay.DonationId);
            Assert.Equal(2, await context.Donations.CountAsync());
            Assert.Equal(1, await context.Payments.CountAsync());
            Assert.Equal(2, await context.SubscriptionDonations.CountAsync());
        }

        /// <summary>
        /// A failed confirmation update rolls back the recurring donation, payment and subscription link.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task SubscriptionCaptureRollsBackWhenConfirmationFails()
        {
            await using SqliteConnection connection = await CreateOpenConnectionAsync();
            await using ApplicationDbContext context = CreateContext(connection);
            EasyPaySubscriptionPaymentEvidence evidence = await SeedSubscriptionAsync(context);
            using var cache = new MemoryCache(new MemoryCacheOptions());
            using var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
            var repository = new SubscriptionRepository(context, cache, new TelemetryClient(telemetryConfiguration));
            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE TRIGGER RejectRecurringDonationConfirmation
                BEFORE UPDATE OF ConfirmedPaymentId ON Donations
                WHEN NEW.ConfirmedPaymentId IS NOT NULL
                BEGIN
                    SELECT RAISE(ABORT, 'Confirmation rejected for test');
                END;
                """);

            DbUpdateException exception = Assert.Throws<DbUpdateException>(
                () => repository.CompleteSubscriptionCapture(evidence));

            var sqliteException = Assert.IsType<SqliteException>(exception.InnerException);
            Assert.Contains("Confirmation rejected for test", sqliteException.Message);
            Assert.Empty(context.ChangeTracker.Entries());
            await using ApplicationDbContext verificationContext = CreateContext(connection);
            Assert.Equal(1, await verificationContext.Donations.CountAsync());
            Assert.Equal(0, await verificationContext.Payments.CountAsync());
            Assert.Equal(1, await verificationContext.SubscriptionDonations.CountAsync());
        }

        private static ApplicationDbContext CreateContext(SqliteConnection connection)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;
            return new ApplicationDbContext(options);
        }

        private static Invoice CreateInvoice(int sequence)
        {
            return new Invoice
            {
                BlobName = Guid.NewGuid(),
                Created = DateTime.UtcNow,
                Number = $"2026/{sequence}",
                Sequence = sequence,
                SequenceScope = 0,
                Year = 2026,
            };
        }

        private static CreditCardPayment CreatePayment(string paymentId)
        {
            return new CreditCardPayment
            {
                Created = DateTime.UtcNow,
                Paid = 10,
                Requested = 10,
                EasyPayPaymentId = paymentId,
                TransactionKey = Guid.NewGuid().ToString(),
            };
        }

        private static async Task<EasyPaySubscriptionPaymentEvidence> SeedSubscriptionAsync(ApplicationDbContext context)
        {
            await context.Database.EnsureCreatedAsync();
            var donation = new Donation
            {
                DonationAmount = 10,
                DonationDate = DateTime.UtcNow.AddMonths(-1),
                DonationItems = [],
                PaymentStatus = PaymentStatus.Payed,
                PublicId = Guid.NewGuid(),
            };
            var subscription = new Subscription
            {
                Created = donation.DonationDate,
                EasyPaySubscriptionId = Guid.NewGuid().ToString(),
                InitialDonation = donation,
                Status = SubscriptionStatus.Active,
                TransactionKey = Guid.NewGuid().ToString(),
            };
            context.Subscriptions.Add(subscription);
            context.SubscriptionDonations.Add(new SubscriptionDonations
            {
                Donation = donation,
                Subscription = subscription,
            });
            await context.SaveChangesAsync();

            return new EasyPaySubscriptionPaymentEvidence
            {
                EasypayPaymentId = Guid.NewGuid().ToString(),
                EasypaySubscriptionId = subscription.EasyPaySubscriptionId,
                TransactionKey = subscription.TransactionKey,
                PaymentDate = DateTime.UtcNow,
                Requested = 10,
                Paid = 10,
            };
        }

        private static async Task<SqliteConnection> CreateOpenConnectionAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            return connection;
        }
    }
}