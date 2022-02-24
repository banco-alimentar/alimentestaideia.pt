// -----------------------------------------------------------------------
// <copyright file="TenantProviderTest.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------


namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tests
{
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant.Naming;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using System.Collections.Generic;
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
                    new DomainNamingStrategy(),
                    new SubdomainNamingStrategy(builder.Build()),
                    new PathNamingStrategy() };

            var context = new DefaultHttpContext();
            context.Request.Scheme = "https";
            context.Request.Host = new HostString($"localhost", 44301);
            context.Request.Path = new PathString($"/Donation");

            TenantProvider tenantProvider = new TenantProvider(providers);
            TenantData tenantData = tenantProvider.GetTenantData(context);
            Assert.NotNull(tenantData);
            Assert.Equal("localhost", tenantData.Name);
        }
    }
}
