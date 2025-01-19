namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tests.Naming
{
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant.Naming;
    using Microsoft.AspNetCore.Http;
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

            var context = new DefaultHttpContext();

            context.Request.Scheme = "https";
            context.Request.Host = new HostString(domain, 443);
            context.Request.Path = new PathString($"/{tenantIdentifier}/Donation");

            PathNamingStrategy strategy = new PathNamingStrategy();
            TenantData name = strategy.GetTenantName(context);
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

            var context = new DefaultHttpContext();

            context.Request.Scheme = "https";
            context.Request.Host = new HostString(domain, 443);
            context.Request.Path = new PathString($"/Donation");

            PathNamingStrategy strategy = new PathNamingStrategy();
            TenantData name = strategy.GetTenantName(context);
            Assert.NotNull(name);
            Assert.Equal(tenantIdentifier, name.Name);
        }
    }
}
