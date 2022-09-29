// -----------------------------------------------------------------------
// <copyright file="DoarTenantMiddlewareTest.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------


namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tests
{
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Middleware;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant.Naming;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Repository;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using System.Collections.Generic;
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
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "Tenant-Override::Name", "alimentestaideia.pt" },
                    { "Tenant-Override::DomainIdentifier", "dev.alimentestaideia.pt" },
                    { "Tenant-Override::InvoicingStrategy", "SingleInvoiceTable" },
                    { "Tenant-Override::PaymentStrategy", "SharedPaymentProcessor" },
                    { "Tenant-Override::UseSecrets", "true" },
                });

            IReadOnlyCollection<INamingStrategy> providers =
                new List<INamingStrategy>() {
                    new DomainNamingStrategy(),
                    new SubdomainNamingStrategy(builder.Build()),
                    new PathNamingStrategy() };

            var context = new DefaultHttpContext();
            context.Request.Scheme = "https";
            context.Request.Host = new HostString($"localhost", 44301);
            context.Request.Path = new PathString($"/Donation");
            context.RequestServices = this.fixture.ServiceProvider;
            TenantProvider tenantProvider = new TenantProvider(providers, new LocalDevelopmentOverride(this.fixture.Configuration));

            InfrastructureDbContext infrastructureDbContext = this.fixture.ServiceProvider.GetRequiredService<InfrastructureDbContext>();

            DoarTenantMiddleware doarTenantMiddleware = new DoarTenantMiddleware(new RequestDelegate(context => { return Task.CompletedTask; }));
            await doarTenantMiddleware.Invoke(
                context,
                tenantProvider,
                this.fixture.ServiceProvider.GetRequiredService<IInfrastructureUnitOfWork>(),
                new KeyVaultConfigurationManager(
                    this.fixture.ServiceProvider.GetRequiredService<InfrastructureDbContext>(),
                    this.fixture.ServiceProvider.GetRequiredService<IWebHostEnvironment>(),
                    this.fixture.ServiceProvider.GetRequiredService<TelemetryClient>(),
                    this.fixture.ServiceProvider.GetRequiredService<IMemoryCache>(),
                    this.fixture.ServiceProvider.GetRequiredService<IConfiguration>()),
                this.fixture.ServiceProvider.GetRequiredService<IWebHostEnvironment>(),
                this.fixture.Configuration);
            Model.Tenant tenant = context.GetTenant();
            Assert.NotNull(tenant);
            Assert.Equal(baseDomain, tenant?.CurrentDomain?.DomainName);
        }
    }
}
