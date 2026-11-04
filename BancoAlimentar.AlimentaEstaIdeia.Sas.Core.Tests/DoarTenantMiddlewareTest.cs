// -----------------------------------------------------------------------
// <copyright file="DoarTenantMiddlewareTest.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------


namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tests
{
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Options;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Middleware;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant.Naming;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Testing.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging.Abstractions;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Doar middleware test.
    /// </summary>
    public class DoarTenantMiddlewareTest : IClassFixture<ServicesFixture>
    {
        private readonly ServicesFixture fixture;

        /// <summary>
        /// Initializes a new instance of the <see cref="DoarTenantMiddlewareTest"/> class.
        /// </summary>
        /// <param name="servicesFixture">Service list.</param>
        public DoarTenantMiddlewareTest(ServicesFixture servicesFixture)
        {
            this.fixture = servicesFixture;
        }

        /// <summary>
        /// Test subdomain naming strategy.
        /// </summary>
        [Fact]
        public async Task GetDoarTenantMiddlewareTest()
        {
            string baseDomain = "alimentaestaideia-developer.azurewebsites.net";
            //          "Tenant-Override": {
            //              "Name": "alimentestaideia.pt",
            //  "DomainIdentifier": "dev.alimentestaideia.pt",
            //  "InvoicingStrategy": "SingleInvoiceTable",
            //  "PaymentStrategy": "SharedPaymentProcessor",
            //  "UseSecrets": true
            //},
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .AddUserSecrets<DoarTenantMiddlewareTest>()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "Tenant-Override:Name", "alimentestaideia.pt" },
                    { "Tenant-Override:DomainIdentifier", "dev.alimentestaideia.pt" },
                    { "Tenant-Override:InvoicingStrategy", "SingleInvoiceTable" },
                    { "Tenant-Override:PaymentStrategy", "SharedPaymentProcessor" },
                    { "Tenant-Override:UseSecrets", "true" },
                })
                .AddConfiguration(this.fixture.Configuration)
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    // appsettings.json contains deployment token placeholders that Azure SDK cannot parse in CI.
                    { "AzureStorage:ConnectionString", "UseDevelopmentStorage=true" },
                    { "AzureStorage:FoodBankSourceBlobName", string.Empty },
                    { "AzureStorage:FoodBankSourceContainerName", string.Empty },
                });

            IConfigurationRoot testConfiguration = builder.Build();

            IReadOnlyCollection<INamingStrategy> providers =
                new List<INamingStrategy>() {
                    new DomainNamingStrategy(),
                    new SubdomainNamingStrategy(testConfiguration),
                    new PathNamingStrategy() };

            var context = new DefaultHttpContext();
            context.Request.Scheme = "https";
            context.Request.Host = new HostString($"localhost", 44301);
            context.Request.Path = new PathString($"/Donation");
            context.RequestServices = this.fixture.ServiceProvider;
            TenantProvider tenantProvider = new TenantProvider(providers, new LocalDevelopmentOverride(testConfiguration));

            InfrastructureDbContext infrastructureDbContext = this.fixture.ServiceProvider.GetRequiredService<InfrastructureDbContext>();

            DoarTenantMiddleware doarTenantMiddleware = new DoarTenantMiddleware(new RequestDelegate(context => { return Task.CompletedTask; }));
            await doarTenantMiddleware.Invoke(
                context,
                tenantProvider,
                this.fixture.ServiceProvider.GetRequiredService<IInfrastructureUnitOfWork>(),
                new TestKeyVaultConfigurationManager(
                    this.fixture.ServiceProvider.GetRequiredService<IConfiguration>()),
                this.fixture.ServiceProvider.GetRequiredService<IWebHostEnvironment>(),
                testConfiguration);

            Model.Tenant tenant = context.GetTenant();
            Assert.NotNull(tenant);
            Assert.Equal(baseDomain, tenant?.Name);
        }

        /// <summary>
        /// A bootstrap failure returns diagnostics without invoking the next delegate.
        /// </summary>
        [Fact]
        public async Task BootstrapFailureReturnsServiceUnavailable()
        {
            var manager = new Mock<IKeyVaultConfigurationManager>(MockBehavior.Strict);
            manager.Setup(item => item.LoadTenantConfiguration()).Returns(false);
            manager.Setup(item => item.GetLastLoadDiagnostics()).Returns(new KeyVaultConfigurationLoadDiagnostics
            {
                Stage = "SasCoreKeyVaultLoadFailed",
                Message = "Synthetic bootstrap failure.",
            });
            var next = new Mock<RequestDelegate>();

            HttpContext context = await this.InvokeMiddlewareAsync(manager, next);

            Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
            Assert.Equal("text/plain; charset=utf-8", context.Response.ContentType);
            string response = await ReadResponseAsync(context);
            Assert.Contains("Stage: SasCoreKeyVaultLoadFailed", response);
            Assert.Contains("Summary: Synthetic bootstrap failure.", response);
            Assert.Null(context.Features.Get<Model.Tenant>());
            next.Verify(item => item(It.IsAny<HttpContext>()), Times.Never);
        }

        /// <summary>
        /// An unknown tenant keeps the existing response status and message.
        /// </summary>
        [Fact]
        public async Task UnknownTenantPreservesResponse()
        {
            var manager = new Mock<IKeyVaultConfigurationManager>(MockBehavior.Strict);
            manager.Setup(item => item.LoadTenantConfiguration()).Returns(true);
            var next = new Mock<RequestDelegate>();

            HttpContext context = await this.InvokeMiddlewareAsync(manager, next, "missing.example");

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            Assert.Equal(
                "Can't find a valid tenant Name: missing.example, IsLocalHost: True for environment localhost",
                await ReadResponseAsync(context));
            Assert.Null(context.Features.Get<Model.Tenant>());
            next.Verify(item => item(It.IsAny<HttpContext>()), Times.Never);
        }

        /// <summary>
        /// Missing configuration returns recorded diagnostics or the tenant-specific fallback.
        /// </summary>
        /// <param name="hasDiagnostics">Whether load diagnostics are available.</param>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task MissingTenantConfigurationReturnsServiceUnavailable(bool hasDiagnostics)
        {
            var manager = new Mock<IKeyVaultConfigurationManager>(MockBehavior.Strict);
            manager.Setup(item => item.LoadTenantConfiguration()).Returns(true);
            manager.Setup(item => item.EnsureTenantConfigurationLoaded(It.IsAny<int>(), TenantDevelopmentOptions.ProductionOptions))
                .ReturnsAsync(false);
            manager.Setup(item => item.GetTenantConfiguration(It.IsAny<int>())).Returns((Dictionary<string, string>)null);
            manager.Setup(item => item.GetLastLoadDiagnostics()).Returns(hasDiagnostics
                ? new KeyVaultConfigurationLoadDiagnostics
                {
                    Stage = "TenantKeyVaultUnavailable",
                    Message = "Synthetic tenant configuration failure.",
                }
                : null);
            var next = new Mock<RequestDelegate>();

            HttpContext context = await this.InvokeMiddlewareAsync(manager, next);

            Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
            Assert.Equal("text/plain; charset=utf-8", context.Response.ContentType);
            string response = await ReadResponseAsync(context);
            Model.Tenant tenant = context.GetTenant();
            Assert.NotNull(tenant);
            Assert.Same(manager.Object, context.Items[typeof(IKeyVaultConfigurationManager).Name]);
            Assert.Contains(hasDiagnostics ? "Stage: TenantKeyVaultUnavailable" : "Stage: TenantSecretsNotLoaded", response);
            Assert.Contains(hasDiagnostics
                ? "Summary: Synthetic tenant configuration failure."
                : $"Summary: Tenant secrets were not loaded for {tenant.Name} (Id {tenant.Id}).", response);
            next.Verify(item => item(It.IsAny<HttpContext>()), Times.Never);
        }

        /// <summary>
        /// Existing configuration proceeds without resolving the initialization service.
        /// </summary>
        [Fact]
        public async Task ExistingConfigurationProceedsWhenNoInitializationIsRequested()
        {
            var manager = new Mock<IKeyVaultConfigurationManager>(MockBehavior.Strict);
            manager.Setup(item => item.LoadTenantConfiguration()).Returns(true);
            manager.Setup(item => item.EnsureTenantConfigurationLoaded(It.IsAny<int>(), TenantDevelopmentOptions.ProductionOptions))
                .ReturnsAsync(false);
            manager.Setup(item => item.GetTenantConfiguration(It.IsAny<int>())).Returns(new Dictionary<string, string>());
            var next = new Mock<RequestDelegate>();
            next.Setup(item => item(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

            HttpContext context = await this.InvokeMiddlewareAsync(manager, next);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            Assert.NotNull(context.GetTenant());
            Assert.Same(manager.Object, context.Items[typeof(IKeyVaultConfigurationManager).Name]);
            next.Verify(item => item(context), Times.Once);
        }

        /// <summary>
        /// Concurrent requests share initialization even when seeding resumes asynchronously.
        /// </summary>
        [Fact]
        public async Task ConcurrentTenantInitializationSeedsOnce()
        {
            using ApplicationDbContext database = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(nameof(this.ConcurrentTenantInitializationSeedsOnce))
                .Options);
            var roleManager = new Mock<RoleManager<ApplicationRole>>(
                Mock.Of<IRoleStore<ApplicationRole>>(),
                Array.Empty<IRoleValidator<ApplicationRole>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                NullLogger<RoleManager<ApplicationRole>>.Instance);
            var seedEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseSeed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            int seedCalls = 0;
            roleManager.Setup(item => item.RoleExistsAsync(It.IsAny<string>())).Returns<string>(async role =>
            {
                if (role == UserRoles.SuperAdmin.ToString())
                {
                    Interlocked.Increment(ref seedCalls);
                    seedEntered.TrySetResult();
                    await releaseSeed.Task;
                }

                return true;
            });
            using ServiceProvider services = new ServiceCollection()
                .AddSingleton(database)
                .AddSingleton(this.fixture.UserManager)
                .AddSingleton(roleManager.Object)
                .BuildServiceProvider();
            IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                { "LocalEmailCapture:Enabled", "true" },
                { "AzureStorage:FoodBankSourceBlobName", string.Empty },
                { "AzureStorage:FoodBankSourceContainerName", string.Empty },
            }).Build();
            var initializer = new TenantInitializationService();
            var tenant = new Model.Tenant { Id = 42 };
            var firstContext = new DefaultHttpContext { RequestServices = services };
            var secondContext = new DefaultHttpContext { RequestServices = services };

            Task first = initializer.InitializeTenant(firstContext, tenant, null, configuration);
            await seedEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Task second = initializer.InitializeTenant(secondContext, tenant, null, configuration);
            Assert.False(second.IsCompleted);
            releaseSeed.SetResult();

            await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5));
            await initializer.InitializeTenant(secondContext, tenant, null, configuration);
            Assert.Equal(1, seedCalls);
        }

        private static async Task<string> ReadResponseAsync(HttpContext context)
        {
            context.Response.Body.Position = 0;
            using StreamReader reader = new StreamReader(context.Response.Body);
            return await reader.ReadToEndAsync();
        }

        private async Task<HttpContext> InvokeMiddlewareAsync(
            Mock<IKeyVaultConfigurationManager> manager,
            Mock<RequestDelegate> next,
            string tenantName = "dev.alimentestaideia.pt")
        {
            using ServiceProvider requestServices = new ServiceCollection().BuildServiceProvider();
            var context = new DefaultHttpContext
            {
                RequestServices = requestServices,
            };
            context.Response.Body = new MemoryStream();
            var tenantProvider = new Mock<ITenantProvider>(MockBehavior.Strict);
            tenantProvider.Setup(item => item.GetTenantData(context)).Returns(new TenantData(tenantName, true));
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "AzureStorage:ConnectionString", "UseDevelopmentStorage=true" },
                })
                .Build();

            var middleware = new DoarTenantMiddleware(next.Object);
            await middleware.Invoke(
                context,
                tenantProvider.Object,
                this.fixture.ServiceProvider.GetRequiredService<IInfrastructureUnitOfWork>(),
                manager.Object,
                this.fixture.ServiceProvider.GetRequiredService<IWebHostEnvironment>(),
                configuration);
            return context;
        }
    }
}
