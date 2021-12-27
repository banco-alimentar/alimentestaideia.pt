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

            // https://cliente.alimentestaideia.pt/CashDonation
            // mytenant.bancoalimentosportugat.pt/CashDonation
            // doar.apoiar.org/CashDonation

            // PROD DNS doar.apoiar.org CNAME alimentaestaideia.azurewebsites.net IP Azure

            // DEV dev.doar.apoiar.org CNAME alimentaestaideia-developer.azurewebsites.net IP Azure
            // DEV alimentaestaideia-developer.azurewebsites.net/doar.apoiar.org/CashDonation

            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var context = new DefaultHttpContext();

            context.Request.Host = new HostString($"{tenantName}.{baseDomain}", 443);
            mockHttpContextAccessor.Setup(_ => _.HttpContext).Returns(context);

            SubdomainNamingStrategy strategy = new SubdomainNamingStrategy(baseDomain);
            TenantData name = strategy.GetTenantName(mockHttpContextAccessor.Object);
            Assert.NotNull(name);
            Assert.Equal(tenantName, name.Name);
        }

        /// <summary>
        /// Test subdomain naming strategy.
        /// </summary>
        [Fact]
        public void GetSubdomainInformationNullTest()
        {
            string baseDomain = "bancoalimentosportugat.pt";
            string tenantName = string.Empty;

            // https://cliente.alimentestaideia.pt/CashDonation
            // mytenant.bancoalimentosportugat.pt/CashDonation
            // doar.apoiar.org/CashDonation

            // PROD DNS doar.apoiar.org CNAME alimentaestaideia.azurewebsites.net IP Azure

            // DEV dev.doar.apoiar.org CNAME alimentaestaideia-developer.azurewebsites.net IP Azure
            // DEV alimentaestaideia-developer.azurewebsites.net/doar.apoiar.org/CashDonation

            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var context = new DefaultHttpContext();

            context.Request.Host = new HostString($"{baseDomain}", 443);
            mockHttpContextAccessor.Setup(_ => _.HttpContext).Returns(context);

            SubdomainNamingStrategy strategy = new SubdomainNamingStrategy(baseDomain);
            TenantData name = strategy.GetTenantName(mockHttpContextAccessor.Object);
            Assert.NotNull(name);
            Assert.Equal(tenantName, name.Name);
        }
    }
}