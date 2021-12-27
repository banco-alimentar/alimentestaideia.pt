namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tests.Naming
{
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant.Naming;
    using Microsoft.AspNetCore.Http;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Domain naming strategy test.
    /// </summary>
    public class DomainNamingStrategyTest
    {
        /// <summary>
        /// Test Domain naming strategy.
        /// </summary>
        [Fact]
        public void GetDomainInformationTest()
        {
            string domain = "bancoalimentosportugat.pt";

            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var context = new DefaultHttpContext();

            context.Request.Host = new HostString(domain, 443);
            mockHttpContextAccessor.Setup(_ => _.HttpContext).Returns(context);

            DomainNamingStrategy strategy = new DomainNamingStrategy();
            TenantData name = strategy.GetTenantName(mockHttpContextAccessor.Object);
            Assert.NotNull(name);
            Assert.Equal(domain, name.Name);
        }
    }
}
