// -----------------------------------------------------------------------
// <copyright file="TenantProviderTest.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------


namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tests
{
    using System.Collections.Generic;
    using System.Net;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant.Naming;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Xunit;

    /// <summary>
    /// Tenant provider test.
    /// </summary>
    public class TenantProviderTest
    {
        /// <summary>
        /// Test subdomain naming strategy.
        /// </summary>
        [Fact]
        public void GetTenantProviderTest()
        {
            string baseDomain = "bancoalimentosportugat.pt";

            IConfigurationBuilder builder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>() { { "SAS-BaseDomain", baseDomain } });

            IReadOnlyCollection<INamingStrategy> providers =
                new List<INamingStrategy>() {
                    new SubdomainNamingStrategy(builder.Build()),
                    new DomainNamingStrategy(),
                    new PathNamingStrategy() };

            var context = new DefaultHttpContext();
            context.Request.Scheme = "https";
            context.Request.Host = new HostString(baseDomain, 44301);
            context.Request.Path = new PathString($"/Donation");

            TenantProvider tenantProvider = new TenantProvider(providers, new LocalDevelopmentOverride(builder.Build()));
            TenantData tenantData = tenantProvider.GetTenantData(context);
            Assert.NotNull(tenantData);
            Assert.Equal(baseDomain, tenantData.Name);
        }

        /// <summary>
        /// Loopback IP requests from a loopback client use the configured local tenant override.
        /// </summary>
        [Fact]
        public void GetTenantProvider_UsesLocalOverrideForLoopbackAddress()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "SAS-BaseDomain", "example.test" },
                    { "Tenant-Override:DomainIdentifier", "localhost" },
                })
                .Build();
            var providers = new List<INamingStrategy>()
            {
                new SubdomainNamingStrategy(configuration),
                new DomainNamingStrategy(),
                new PathNamingStrategy(),
            };
            var context = new DefaultHttpContext();
            context.Request.Host = new HostString(IPAddress.Loopback.ToString(), 5000);
            context.Connection.RemoteIpAddress = IPAddress.Loopback;
            var tenantProvider = new TenantProvider(providers, new LocalDevelopmentOverride(configuration));

            TenantData tenantData = tenantProvider.GetTenantData(context);

            Assert.Equal("localhost", tenantData.Name);
        }

        /// <summary>
        /// Loopback IP hosts from non-loopback clients do not use the local tenant override.
        /// </summary>
        [Fact]
        public void GetTenantProvider_RequiresLoopbackClientForLoopbackAddress()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "SAS-BaseDomain", "example.test" },
                    { "Tenant-Override:DomainIdentifier", "localhost" },
                })
                .Build();
            var providers = new List<INamingStrategy>()
            {
                new SubdomainNamingStrategy(configuration),
                new DomainNamingStrategy(),
                new PathNamingStrategy(),
            };
            var context = new DefaultHttpContext();
            context.Request.Host = new HostString(IPAddress.Loopback.ToString(), 5000);
            context.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.1");
            var tenantProvider = new TenantProvider(providers, new LocalDevelopmentOverride(configuration));

            TenantData tenantData = tenantProvider.GetTenantData(context);

            Assert.Equal(IPAddress.Loopback.ToString(), tenantData.Name);
        }
    }
}
