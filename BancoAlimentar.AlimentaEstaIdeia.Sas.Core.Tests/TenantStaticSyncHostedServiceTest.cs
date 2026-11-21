namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Options;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.HostedServices;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.StaticFileProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using Moq;
    using Xunit;

    /// <summary>
    /// Exercises background refresh scheduling without remote services or wall-clock delays.
    /// </summary>
    public class TenantStaticSyncHostedServiceTest
    {
        /// <summary>
        /// Successful refreshes repeat after 24 hours using a new service scope.
        /// </summary>
        [Fact]
        public async Task SuccessfulRefreshRepeatsAfterOneDay()
        {
            await using Harness harness = await Harness.CreateAsync();
            await harness.Service.StartAsync(CancellationToken.None);
            DateTimeOffset startedAt = harness.Clock.GetUtcNow();
            Assert.Equal(startedAt.AddDays(1), await harness.NextScheduleAsync(harness.Tenants[0].PublicId));
            Assert.Equal(startedAt.AddDays(1), await harness.Clock.NextTimerAsync());

            harness.Clock.Advance(TimeSpan.FromDays(1));
            Assert.Equal(startedAt.AddDays(2), await harness.NextScheduleAsync(harness.Tenants[0].PublicId));

            harness.Cache.Verify(cache => cache.ResyncFromBlobAsync(harness.Tenants[0].PublicId, "tenant1", "test-only", true, It.IsAny<CancellationToken>()), Times.Exactly(2));
            Assert.Equal(2, harness.ScopesCreated);
            harness.Manager.Verify(manager => manager.LoadTenantConfiguration(), Times.Never);
        }

        /// <summary>
        /// One broken tenant retries with bounded backoff without refreshing successful tenants.
        /// </summary>
        [Fact]
        public async Task FailedTenantUsesBoundedIndependentBackoff()
        {
            await using Harness harness = await Harness.CreateAsync(tenantCount: 2);
            Guid failingTenant = harness.Tenants[1].PublicId;
            harness.Cache.Setup(cache => cache.ResyncFromBlobAsync(failingTenant, "tenant2", "test-only", true, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Synthetic storage failure."));
            await harness.Service.StartAsync(CancellationToken.None);
            DateTimeOffset nextAttempt = harness.Clock.GetUtcNow().AddSeconds(5);
            Assert.Equal(harness.Clock.GetUtcNow().AddDays(1), await harness.NextScheduleAsync(harness.Tenants[0].PublicId));
            Assert.Equal(nextAttempt, await harness.NextScheduleAsync(failingTenant));

            foreach (int retryDelay in new[] { 10, 20, 40, 60, 60 })
            {
                Assert.Equal(nextAttempt, await harness.Clock.NextTimerAsync());
                harness.Clock.Advance(nextAttempt - harness.Clock.GetUtcNow());
                nextAttempt = harness.Clock.GetUtcNow().AddSeconds(retryDelay);
                Assert.Equal(nextAttempt, await harness.NextScheduleAsync(failingTenant));
            }

            harness.Cache.Verify(cache => cache.ResyncFromBlobAsync(harness.Tenants[0].PublicId, "tenant1", "test-only", true, It.IsAny<CancellationToken>()), Times.Once);
            harness.Cache.Verify(cache => cache.ResyncFromBlobAsync(failingTenant, "tenant2", "test-only", true, It.IsAny<CancellationToken>()), Times.Exactly(6));
            harness.Cache.Verify(cache => cache.RecordFailure(failingTenant, It.IsAny<Exception>()), Times.Exactly(6));
        }

        /// <summary>
        /// Failed metadata bootstrap is retried instead of ending the background service.
        /// </summary>
        [Fact]
        public async Task BootstrapFailureRemainsRetryable()
        {
            await using Harness harness = await Harness.CreateAsync();
            var bootstrapAttempted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            int attempts = 0;
            harness.Manager.Setup(manager => manager.LoadTenantConfigurationAsync(It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    if (Interlocked.Increment(ref attempts) == 1)
                    {
                        bootstrapAttempted.TrySetResult(true);
                        return Task.FromResult(false);
                    }

                    return Task.FromResult(true);
                });
            await harness.Service.StartAsync(CancellationToken.None);
            await bootstrapAttempted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(harness.Clock.GetUtcNow().AddSeconds(5), await harness.Clock.NextTimerAsync());

            harness.Clock.Advance(TimeSpan.FromSeconds(5));
            Assert.Equal(harness.Clock.GetUtcNow().AddDays(1), await harness.NextScheduleAsync(harness.Tenants[0].PublicId));
            Assert.Equal(2, attempts);
            Assert.Equal(2, harness.ScopesCreated);
        }

        /// <summary>
        /// Missing tenant storage configuration is recorded and retried, not skipped.
        /// </summary>
        [Fact]
        public async Task MissingConfigurationIsRetried()
        {
            await using Harness harness = await Harness.CreateAsync();
            harness.Manager.Setup(manager => manager.GetTenantConfiguration(It.IsAny<int>())).Returns((Dictionary<string, string>)null);
            await harness.Service.StartAsync(CancellationToken.None);
            Assert.Equal(harness.Clock.GetUtcNow().AddSeconds(5), await harness.NextScheduleAsync(harness.Tenants[0].PublicId));
            Assert.Equal(harness.Clock.GetUtcNow().AddSeconds(5), await harness.Clock.NextTimerAsync());
            harness.Cache.Verify(cache => cache.RecordFailure(harness.Tenants[0].PublicId, It.IsAny<InvalidOperationException>()), Times.Once);
            harness.Manager.Setup(manager => manager.GetTenantConfiguration(It.IsAny<int>())).Returns(new Dictionary<string, string> { ["AzureStorage:ConnectionString"] = "test-only" });

            harness.Clock.Advance(TimeSpan.FromSeconds(5));
            Assert.Equal(harness.Clock.GetUtcNow().AddDays(1), await harness.NextScheduleAsync(harness.Tenants[0].PublicId));

            harness.Cache.Verify(cache => cache.ResyncFromBlobAsync(harness.Tenants[0].PublicId, "tenant1", "test-only", true, It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Invalidation immediately refreshes successful tenants rather than waiting one day.
        /// </summary>
        [Fact]
        public async Task ReloadSchedulesImmediateRefresh()
        {
            await using Harness harness = await Harness.CreateAsync();
            await harness.Service.StartAsync(CancellationToken.None);
            DateTimeOffset firstSchedule = await harness.NextScheduleAsync(harness.Tenants[0].PublicId);
            Assert.Equal(firstSchedule, await harness.Clock.NextTimerAsync());

            harness.ConfigurationCache.Clear();

            // Resetting the schedule emits "now" before the completed refresh emits its deadline.
            Assert.Equal(harness.Clock.GetUtcNow(), await harness.NextScheduleAsync(harness.Tenants[0].PublicId));
            Assert.Equal(firstSchedule, await harness.NextScheduleAsync(harness.Tenants[0].PublicId));
            harness.Cache.Verify(cache => cache.ResyncFromBlobAsync(harness.Tenants[0].PublicId, "tenant1", "test-only", true, It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        /// <summary>
        /// Shutdown cancels an in-progress refresh and clears scheduled runtime status.
        /// </summary>
        [Fact]
        public async Task ShutdownCancelsInFlightRefresh()
        {
            await using Harness harness = await Harness.CreateAsync();
            var entered = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
            harness.Cache.Setup(cache => cache.ResyncFromBlobAsync(harness.Tenants[0].PublicId, "tenant1", "test-only", true, It.IsAny<CancellationToken>()))
                .Returns<Guid, string, string, bool, CancellationToken>(async (id, name, connection, changed, token) =>
                {
                    entered.TrySetResult(token);
                    await Task.Delay(Timeout.InfiniteTimeSpan, token);
                    return new TenantStaticLocalCacheResult();
                });
            await harness.Service.StartAsync(CancellationToken.None);
            CancellationToken refreshToken = await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));

            await harness.Service.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));

            Assert.True(refreshToken.IsCancellationRequested);
            harness.Cache.Verify(cache => cache.SetNextAttempt(harness.Tenants[0].PublicId, null), Times.AtLeastOnce);
            harness.Cache.Verify(cache => cache.ResyncFromBlobAsync(harness.Tenants[0].PublicId, "tenant1", "test-only", true, It.IsAny<CancellationToken>()), Times.Once);
        }

        private sealed class Harness : IAsyncDisposable
        {
            private readonly ServiceProvider services;
            private readonly Channel<(Guid TenantId, DateTimeOffset NextAttempt)> schedules = Channel.CreateUnbounded<(Guid, DateTimeOffset)>();
            private readonly Dictionary<Guid, Queue<DateTimeOffset>> pendingSchedules = new Dictionary<Guid, Queue<DateTimeOffset>>();
            private int scopesCreated;

            private Harness()
            {
                this.Manager.Setup(manager => manager.LoadTenantConfigurationAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
                this.Manager.Setup(manager => manager.EnsureTenantConfigurationLoaded(It.IsAny<int>(), It.IsAny<TenantDevelopmentOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
                this.Manager.Setup(manager => manager.GetTenantConfiguration(It.IsAny<int>())).Returns(new Dictionary<string, string> { ["AzureStorage:ConnectionString"] = "test-only" });
                this.Cache.Setup(cache => cache.ResyncFromBlobAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new TenantStaticLocalCacheResult());
                this.Cache.Setup(cache => cache.GetStatus(It.IsAny<Guid>())).Returns(new TenantStaticSyncStatus { LastFailureType = "RefreshFailed" });
                this.Cache.Setup(cache => cache.SetNextAttempt(It.IsAny<Guid>(), It.IsAny<DateTimeOffset?>()))
                    .Callback<Guid, DateTimeOffset?>((id, at) =>
                    {
                        if (at.HasValue)
                        {
                            this.schedules.Writer.TryWrite((id, at.Value));
                        }
                    });
                var environment = new Mock<IWebHostEnvironment>();
                environment.SetupGet(item => item.EnvironmentName).Returns("Production");
                var services = new ServiceCollection();
                services.AddSingleton(this.ConfigurationCache);
                services.AddSingleton(this.Cache.Object);
                services.AddSingleton(environment.Object);
                services.AddScoped<IKeyVaultConfigurationManager>(_ =>
                {
                    Interlocked.Increment(ref this.scopesCreated);
                    return this.Manager.Object;
                });
                string databaseName = Guid.NewGuid().ToString();
                services.AddDbContext<InfrastructureDbContext>(options => options.UseInMemoryDatabase(databaseName));
                this.services = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
                this.Service = new TenantStaticSyncHostedService(NullLogger<TenantStaticSyncHostedService>.Instance, this.services, Options.Create(new TenantRuntimeOptions()), this.Clock);
            }

            public Mock<IKeyVaultConfigurationManager> Manager { get; } = new Mock<IKeyVaultConfigurationManager>();

            public Mock<ITenantStaticLocalCacheService> Cache { get; } = new Mock<ITenantStaticLocalCacheService>();

            public KeyVaultConfigurationCache ConfigurationCache { get; } = new KeyVaultConfigurationCache();

            public TestClock Clock { get; } = new TestClock();

            public TenantStaticSyncHostedService Service { get; }

            public List<Model.Tenant> Tenants { get; } = new List<Model.Tenant>();

            public int ScopesCreated => Volatile.Read(ref this.scopesCreated);

            public static async Task<Harness> CreateAsync(int tenantCount = 1)
            {
                var harness = new Harness();
                using IServiceScope scope = harness.services.CreateScope();
                InfrastructureDbContext context = scope.ServiceProvider.GetRequiredService<InfrastructureDbContext>();
                for (int id = 1; id <= tenantCount; id++)
                {
                    harness.Tenants.Add(new Model.Tenant
                    {
                        Id = id,
                        PublicId = Guid.NewGuid(),
                        Name = $"tenant{id}",
                        Domains = new List<DomainIdentifier> { new DomainIdentifier { DomainName = $"tenant{id}.example", Environment = "Production" } },
                    });
                }

                context.Tenants.AddRange(harness.Tenants);
                await context.SaveChangesAsync();
                return harness;
            }

            public async Task<DateTimeOffset> NextScheduleAsync(Guid tenantId)
            {
                if (this.pendingSchedules.TryGetValue(tenantId, out Queue<DateTimeOffset> pending) && pending.Count > 0)
                {
                    return pending.Dequeue();
                }

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                while (true)
                {
                    var schedule = await this.schedules.Reader.ReadAsync(timeout.Token);
                    if (schedule.TenantId == tenantId)
                    {
                        return schedule.NextAttempt;
                    }

                    if (!this.pendingSchedules.TryGetValue(schedule.TenantId, out Queue<DateTimeOffset> otherPending))
                    {
                        otherPending = new Queue<DateTimeOffset>();
                        this.pendingSchedules.Add(schedule.TenantId, otherPending);
                    }

                    otherPending.Enqueue(schedule.NextAttempt);
                }
            }

            public async ValueTask DisposeAsync()
            {
                await this.Service.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
                this.Service.Dispose();
                await this.services.DisposeAsync();
            }
        }

        private sealed class TestClock : TimeProvider
        {
            private readonly List<TestTimer> timers = new List<TestTimer>();
            private readonly Channel<DateTimeOffset> timerRequests = Channel.CreateUnbounded<DateTimeOffset>();
            private DateTimeOffset now = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);

            public override DateTimeOffset GetUtcNow()
            {
                lock (this.timers)
                {
                    return this.now;
                }
            }

            public override ITimer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
            {
                var timer = new TestTimer(this, callback, state);
                timer.Change(dueTime, period);
                return timer;
            }

            public void Advance(TimeSpan duration)
            {
                TestTimer[] due;
                lock (this.timers)
                {
                    this.now += duration;
                    due = this.timers.Where(timer => timer.DueAt <= this.now).ToArray();
                    foreach (TestTimer timer in due)
                    {
                        this.timers.Remove(timer);
                    }
                }

                foreach (TestTimer timer in due)
                {
                    timer.Invoke();
                }
            }

            public async Task<DateTimeOffset> NextTimerAsync()
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                return await this.timerRequests.Reader.ReadAsync(timeout.Token);
            }

            private sealed class TestTimer : ITimer
            {
                private readonly TestClock clock;
                private readonly TimerCallback callback;
                private readonly object state;

                public TestTimer(TestClock clock, TimerCallback callback, object state)
                {
                    this.clock = clock;
                    this.callback = callback;
                    this.state = state;
                }

                public DateTimeOffset DueAt { get; private set; }

                public bool Change(TimeSpan dueTime, TimeSpan period)
                {
                    lock (this.clock.timers)
                    {
                        this.clock.timers.Remove(this);
                        if (dueTime != Timeout.InfiniteTimeSpan)
                        {
                            this.DueAt = this.clock.now + dueTime;
                            this.clock.timers.Add(this);
                            this.clock.timerRequests.Writer.TryWrite(this.DueAt);
                        }
                    }

                    return true;
                }

                public void Invoke()
                {
                    this.callback(this.state);
                }

                public void Dispose()
                {
                    lock (this.clock.timers)
                    {
                        this.clock.timers.Remove(this);
                    }
                }

                public ValueTask DisposeAsync()
                {
                    this.Dispose();
                    return ValueTask.CompletedTask;
                }
            }
        }
    }
}
