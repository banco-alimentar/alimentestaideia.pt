// -----------------------------------------------------------------------
// <copyright file="RelationalConcurrencyModelTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Common.EasyPay;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Data.Sqlite;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Diagnostics;
    using Microsoft.Extensions.Caching.Memory;
    using Xunit;
    using NotificationGeneric = Easypay.Rest.Client.Model.NotificationGeneric;
    using SinglePaymentStatus = Easypay.Rest.Client.Model.SinglePaymentStatus;

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
        /// An Easypay webhook that races MBWay polling retries, claims the provider id,
        /// and does not persist unverified fee values.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task EasyPayWebhookRetriesWhenPollingCompletesDonationConcurrently()
        {
            await using SqliteConnection connection = await CreateOpenConnectionAsync();
            string transactionKey = Guid.NewGuid().ToString();
            var donation = new Donation
            {
                DonationAmount = 10,
                DonationDate = DateTime.UtcNow,
                PaymentStatus = PaymentStatus.WaitingPayment,
                PublicId = Guid.NewGuid(),
            };
            await using (ApplicationDbContext seedContext = CreateContext(connection))
            {
                await seedContext.Database.EnsureCreatedAsync();
                donation.PaymentList =
                [
                    new MBWayPayment
                    {
                        Created = DateTime.UtcNow,
                        TransactionKey = transactionKey,
                    },
                ];
                seedContext.Donations.Add(donation);
                await seedContext.SaveChangesAsync();
            }

            using var cache = new MemoryCache(new MemoryCacheOptions());
            using var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
            var telemetryClient = new TelemetryClient(telemetryConfiguration);
            var pollingCompletion = new BeforeFirstSaveInterceptor(() =>
            {
                using ApplicationDbContext pollingContext = CreateContext(connection);
                new DonationRepository(pollingContext, cache, telemetryClient)
                    .UpdatePaymentStatus<MBWayPayment>(donation.PublicId, Easypay.Rest.Client.Model.SinglePaymentStatus.Paid);
            });
            await using ApplicationDbContext webhookContext = CreateContext(connection, pollingCompletion);
            var repository = new DonationRepository(webhookContext, cache, telemetryClient);
            string easyPayId = Guid.NewGuid().ToString();

            (int donationId, int paymentId) = await repository.CompleteEasyPayPaymentAsync<MBWayPayment>(
                easyPayId,
                transactionKey,
                Guid.NewGuid().ToString(),
                DateTime.UtcNow,
                requested: 10,
                paid: 10,
                fixedFee: 0.1f,
                variableFee: 0.2f,
                tax: 0.05f,
                transfer: 9.65f,
                configuration: null);

            Assert.True(pollingCompletion.Executed);
            Assert.Equal(donation.Id, donationId);
            await using ApplicationDbContext verificationContext = CreateContext(connection);
            Donation persisted = await verificationContext.Donations
                .Include(value => value.ConfirmedPayment)
                .SingleAsync();
            Assert.Equal(PaymentStatus.Payed, persisted.PaymentStatus);
            Assert.Equal(paymentId, persisted.ConfirmedPayment.Id);
            var payment = Assert.IsType<MBWayPayment>(persisted.ConfirmedPayment);
            Assert.Equal(easyPayId, payment.EasyPayPaymentId);
            Assert.NotNull(payment.Completed);
            Assert.Equal(0, payment.FixedFee);
            Assert.Equal(0, payment.Transfer);
        }

        /// <summary>
        /// Generic notifications re-evaluate their status when polling completes the payment.
        /// </summary>
        /// <param name="status">Incoming notification status.</param>
        /// <returns>A <see cref="Task"/> representing the operation.</returns>
        [Theory]
        [InlineData(NotificationGeneric.StatusEnum.Success)]
        [InlineData(NotificationGeneric.StatusEnum.Failed)]
        public async Task GenericWebhookRetriesWhenPollingCompletesDonationConcurrently(NotificationGeneric.StatusEnum status)
        {
            await using SqliteConnection connection = await CreateOpenConnectionAsync();
            await using ApplicationDbContext seedContext = CreateContext(connection);
            Donation donation = await SeedGenericDonationAsync(seedContext);
            string transactionKey = donation.PaymentList.Single().TransactionKey;
            using var cache = new MemoryCache(new MemoryCacheOptions());
            using var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
            var telemetryClient = new TelemetryClient(telemetryConfiguration);
            var pollingCompletion = new BeforeFirstSaveInterceptor(() =>
            {
                using ApplicationDbContext pollingContext = CreateContext(connection);
                Assert.True(new DonationRepository(pollingContext, cache, telemetryClient)
                    .UpdatePaymentStatus<MBWayPayment>(donation.PublicId, SinglePaymentStatus.Paid));
            });
            await using ApplicationDbContext webhookContext = CreateContext(connection, pollingCompletion);
            var repository = new DonationRepository(webhookContext, cache, telemetryClient);

            (int paymentId, int donationId) = repository.UpdatePaymentTransaction(
                Guid.NewGuid().ToString(),
                transactionKey,
                status,
                string.Empty);

            Assert.True(pollingCompletion.Executed);
            Assert.Equal(donation.Id, donationId);
            await using ApplicationDbContext verificationContext = CreateContext(connection);
            Donation persisted = await verificationContext.Donations
                .Include(value => value.ConfirmedPayment)
                .SingleAsync();
            Assert.Equal(PaymentStatus.Payed, persisted.PaymentStatus);
            Assert.Equal(paymentId, persisted.ConfirmedPayment.Id);
            Assert.NotEqual(NotificationGeneric.StatusEnum.Failed.ToString(), persisted.ConfirmedPayment.Status);
            Assert.Equal(status == NotificationGeneric.StatusEnum.Success, persisted.ConfirmedPayment.Completed.HasValue);
        }

        /// <summary>
        /// A concurrent successful capture cannot replace a different confirmed payment.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the operation.</returns>
        [Fact]
        public async Task GenericWebhookRetryPreservesCompetingConfirmedPayment()
        {
            await using SqliteConnection connection = await CreateOpenConnectionAsync();
            await using ApplicationDbContext seedContext = CreateContext(connection);
            Donation donation = await SeedGenericDonationAsync(seedContext, includeSecondPayment: true);
            string transactionKey = donation.PaymentList.First().TransactionKey;
            int winningPaymentId = donation.PaymentList.Last().Id;
            var competingCompletion = new BeforeFirstSaveInterceptor(() =>
            {
                using ApplicationDbContext competingContext = CreateContext(connection);
                Donation current = competingContext.Donations.Include(value => value.PaymentList).Single();
                current.PaymentStatus = PaymentStatus.Payed;
                current.ConfirmedPayment = current.PaymentList.Single(value => value.Id == winningPaymentId);
                current.ConfirmedPayment.Status = "Success";
                competingContext.SaveChanges();
            });
            using var cache = new MemoryCache(new MemoryCacheOptions());
            using var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
            await using ApplicationDbContext webhookContext = CreateContext(connection, competingCompletion);
            var repository = new DonationRepository(webhookContext, cache, new TelemetryClient(telemetryConfiguration));

            (int paymentId, int donationId) = repository.UpdatePaymentTransaction(
                Guid.NewGuid().ToString(),
                transactionKey,
                NotificationGeneric.StatusEnum.Success,
                string.Empty);

            Assert.True(competingCompletion.Executed);
            Assert.Equal(donation.Id, donationId);
            await using ApplicationDbContext verificationContext = CreateContext(connection);
            Donation persisted = await verificationContext.Donations
                .Include(value => value.ConfirmedPayment)
                .SingleAsync();
            Assert.Equal(PaymentStatus.Payed, persisted.PaymentStatus);
            Assert.Equal(winningPaymentId, persisted.ConfirmedPayment.Id);
            var losingPayment = await verificationContext.Payments.SingleAsync(value => value.Id == paymentId);
            Assert.NotEqual("Success", losingPayment.Status);
            Assert.Null(losingPayment.Completed);
        }

        /// <summary>
        /// A competing capture cannot replace a donation's already confirmed payment.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the operation.</returns>
        [Fact]
        public async Task EasyPayWebhookRejectsSecondSuccessfulCapture()
        {
            await using SqliteConnection connection = await CreateOpenConnectionAsync();
            await using var seedContext = CreateContext(connection);
            await seedContext.Database.EnsureCreatedAsync();
            string confirmedKey = Guid.NewGuid().ToString();
            string competingKey = Guid.NewGuid().ToString();
            var donation = new Donation
            {
                DonationAmount = 10,
                DonationDate = DateTime.UtcNow,
                PaymentStatus = PaymentStatus.Payed,
                PublicId = Guid.NewGuid(),
            };
            donation.PaymentList =
            [
                new CreditCardPayment
                {
                    Created = DateTime.UtcNow,
                    Completed = DateTime.UtcNow,
                    Paid = 10,
                    Requested = 10,
                    Status = "Success",
                    TransactionKey = confirmedKey,
                },
                new CreditCardPayment
                {
                    Created = DateTime.UtcNow,
                    Paid = 10,
                    Requested = 10,
                    TransactionKey = competingKey,
                },
            ];
            seedContext.Donations.Add(donation);
            await seedContext.SaveChangesAsync();
            donation.ConfirmedPayment = donation.PaymentList.First();
            await seedContext.SaveChangesAsync();

            using var cache = new MemoryCache(new MemoryCacheOptions());
            using var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
            await using ApplicationDbContext webhookContext = CreateContext(connection);
            var repository = new DonationRepository(
                webhookContext,
                cache,
                new TelemetryClient(telemetryConfiguration));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                repository.CompleteEasyPayPaymentAsync<CreditCardPayment>(
                    Guid.NewGuid().ToString(),
                    competingKey,
                    Guid.NewGuid().ToString(),
                    DateTime.UtcNow,
                    requested: 10,
                    paid: 10,
                    fixedFee: 0.1f,
                    variableFee: 0.2f,
                    tax: 0.05f,
                    transfer: 9.65f,
                    configuration: null));

            await using ApplicationDbContext verificationContext = CreateContext(connection);
            var payments = await verificationContext.CreditCardPayments
                .OrderBy(payment => payment.Id)
                .ToListAsync();
            Assert.Equal(2, payments.Count);
            Assert.Null(payments[1].EasyPayPaymentId);
        }

        /// <summary>
        /// The first concurrent Easypay id claim wins and the retry rejects the other id.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the operation.</returns>
        [Fact]
        public async Task EasyPayWebhookRetriesConcurrentProviderIdClaim()
        {
            await using SqliteConnection connection = await CreateOpenConnectionAsync();
            await using var seedContext = CreateContext(connection);
            await seedContext.Database.EnsureCreatedAsync();
            string transactionKey = Guid.NewGuid().ToString();
            var donation = new Donation
            {
                DonationAmount = 10,
                DonationDate = DateTime.UtcNow,
                PaymentStatus = PaymentStatus.Payed,
                PublicId = Guid.NewGuid(),
            };
            donation.PaymentList =
            [
                new MBWayPayment
                {
                    Created = DateTime.UtcNow,
                    Completed = DateTime.UtcNow,
                    Paid = 10,
                    Requested = 10,
                    Status = "Success",
                    TransactionKey = transactionKey,
                },
            ];
            seedContext.Donations.Add(donation);
            await seedContext.SaveChangesAsync();
            donation.ConfirmedPayment = donation.PaymentList.First();
            await seedContext.SaveChangesAsync();

            using var cache = new MemoryCache(new MemoryCacheOptions());
            using var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
            var telemetryClient = new TelemetryClient(telemetryConfiguration);
            string winningId = Guid.NewGuid().ToString();
            string losingId = Guid.NewGuid().ToString();
            var concurrentClaim = new BeforeFirstSaveInterceptor(() =>
            {
                using ApplicationDbContext concurrentContext = CreateContext(connection);
                var concurrentRepository = new DonationRepository(concurrentContext, cache, telemetryClient);
                concurrentRepository.CompleteEasyPayPaymentAsync<MBWayPayment>(
                    winningId,
                    transactionKey,
                    Guid.NewGuid().ToString(),
                    DateTime.UtcNow,
                    requested: 10,
                    paid: 10,
                    fixedFee: 0.1f,
                    variableFee: 0.2f,
                    tax: 0.05f,
                    transfer: 9.65f,
                    configuration: null).GetAwaiter().GetResult();
            });
            await using ApplicationDbContext webhookContext = CreateContext(connection, concurrentClaim);
            var repository = new DonationRepository(webhookContext, cache, telemetryClient);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                repository.CompleteEasyPayPaymentAsync<MBWayPayment>(
                    losingId,
                    transactionKey,
                    Guid.NewGuid().ToString(),
                    DateTime.UtcNow,
                    requested: 10,
                    paid: 10,
                    fixedFee: 0.3f,
                    variableFee: 0.4f,
                    tax: 0.05f,
                    transfer: 9.25f,
                    configuration: null));

            await using ApplicationDbContext verificationContext = CreateContext(connection);
            var persisted = await verificationContext.MBWayPayments.SingleAsync();
            Assert.Equal(winningId, persisted.EasyPayPaymentId);
            Assert.Equal(0, persisted.FixedFee);
            Assert.Equal(0, persisted.Transfer);
        }

        /// <summary>
        /// Subscription capture rebuilds its state and retries after a concurrency exception.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the operation.</returns>
        [Fact]
        public async Task SubscriptionCaptureRetriesAfterConcurrencyException()
        {
            await using SqliteConnection connection = await CreateOpenConnectionAsync();
            await using ApplicationDbContext seedContext = CreateContext(connection);
            EasyPaySubscriptionPaymentEvidence evidence = await SeedSubscriptionAsync(seedContext);
            var interceptor = new ThrowOnceConcurrencyExceptionInterceptor();
            await using ApplicationDbContext context = CreateContext(connection, interceptor);
            using var cache = new MemoryCache(new MemoryCacheOptions());
            using var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
            var repository = new SubscriptionRepository(context, cache, new TelemetryClient(telemetryConfiguration));

            var result = repository.CompleteSubscriptionCapture(evidence);

            Assert.True(result.DonationId > 0);
            Assert.Equal(3, interceptor.Attempts);
            Assert.Equal(2, await context.Donations.CountAsync());
            Assert.Single(await context.Payments.ToListAsync());
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

        private static ApplicationDbContext CreateContext(
            SqliteConnection connection,
            params IInterceptor[] interceptors)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .AddInterceptors(interceptors)
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

        private static async Task<Donation> SeedGenericDonationAsync(
            ApplicationDbContext context,
            bool includeSecondPayment = false)
        {
            await context.Database.EnsureCreatedAsync();
            var donation = new Donation
            {
                DonationAmount = 10,
                DonationDate = DateTime.UtcNow,
                PaymentStatus = PaymentStatus.WaitingPayment,
                PublicId = Guid.NewGuid(),
                PaymentList =
                [
                    new MBWayPayment { Created = DateTime.UtcNow, TransactionKey = Guid.NewGuid().ToString() },
                ],
            };
            if (includeSecondPayment)
            {
                donation.PaymentList.Add(
                    new MBWayPayment { Created = DateTime.UtcNow, TransactionKey = Guid.NewGuid().ToString() });
            }

            context.Donations.Add(donation);
            await context.SaveChangesAsync();
            return donation;
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

        private sealed class BeforeFirstSaveInterceptor(Action action) : SaveChangesInterceptor
        {
            public bool Executed { get; private set; }

            public override InterceptionResult<int> SavingChanges(
                DbContextEventData eventData,
                InterceptionResult<int> result)
            {
                if (!this.Executed)
                {
                    this.Executed = true;
                    action();
                }

                return base.SavingChanges(eventData, result);
            }

            public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
                DbContextEventData eventData,
                InterceptionResult<int> result,
                CancellationToken cancellationToken = default)
            {
                if (!this.Executed)
                {
                    this.Executed = true;
                    action();
                }

                return base.SavingChangesAsync(eventData, result, cancellationToken);
            }
        }

        private sealed class ThrowOnceConcurrencyExceptionInterceptor : SaveChangesInterceptor
        {
            public int Attempts { get; private set; }

            public override InterceptionResult<int> SavingChanges(
                DbContextEventData eventData,
                InterceptionResult<int> result)
            {
                this.Attempts++;
                if (this.Attempts == 1)
                {
                    throw new DbUpdateConcurrencyException("Simulated optimistic concurrency conflict.");
                }

                return base.SavingChanges(eventData, result);
            }
        }
    }
}