// -----------------------------------------------------------------------
// <copyright file="SubdomainNamingStrategyTest.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tests
{
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant.Naming;
    using Microsoft.AspNetCore.Http;
    using Moq;
    using Xunit;

    /// <summary>
    /// Subdomain naming strategy test.
    /// </summary>
    public class SubdomainNamingStrategyTest
    {
        /// <summary>
        /// Test subdomain naming strategy.
        /// </summary>
        [Fact]
        public void GetSubdomainInformationTest()
        {
            string baseDomain = "bancoalimentosportugat.pt";
            string tenantName = "mytenant";

            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var context = new DefaultHttpContext();

            context.Request.Host = new HostString($"{tenantName}.{baseDomain}", 443);
            mockHttpContextAccessor.Setup(_ => _.HttpContext).Returns(context);

            SubdomainNamingStrategy strategy = new SubdomainNamingStrategy(mockHttpContextAccessor.Object, baseDomain);
            TenantData name = strategy.GetTenantName();
            Assert.NotNull(name);
            Assert.Equal(tenantName, name.Name);
        }
    }
}