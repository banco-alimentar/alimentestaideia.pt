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
    /// Path naming strategy test.
    /// </summary>
    public class PathNamingStrategyTest
    {
        /// <summary>
        /// Test Path naming strategy.
        /// </summary>
        [Fact]
        public void GetPathInformationTest()
        {
            string domain = "bancoalimentosportugat.pt";
            string tenantIdentifier = "doar.apoiar.org";

            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var context = new DefaultHttpContext();

            context.Request.Scheme = "https";
            context.Request.Host = new HostString(domain, 443);
            context.Request.Path = new PathString($"/{tenantIdentifier}/Donation");
            mockHttpContextAccessor.Setup(_ => _.HttpContext).Returns(context);

            PathNamingStrategy strategy = new PathNamingStrategy();
            TenantData name = strategy.GetTenantName(mockHttpContextAccessor.Object);
            Assert.NotNull(name);
            Assert.Equal(tenantIdentifier, name.Name);
        }

        /// <summary>
        /// Test Path naming strategy.
        /// </summary>
        [Fact]
        public void GetPathInformationEmptyTest()
        {
            string domain = "bancoalimentosportugat.pt";
            string tenantIdentifier = string.Empty;

            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var context = new DefaultHttpContext();

            context.Request.Scheme = "https";
            context.Request.Host = new HostString(domain, 443);
            context.Request.Path = new PathString($"/Donation");
            mockHttpContextAccessor.Setup(_ => _.HttpContext).Returns(context);

            PathNamingStrategy strategy = new PathNamingStrategy();
            TenantData name = strategy.GetTenantName(mockHttpContextAccessor.Object);
            Assert.NotNull(name);
            Assert.Equal(tenantIdentifier, name.Name);
        }
    }
}
