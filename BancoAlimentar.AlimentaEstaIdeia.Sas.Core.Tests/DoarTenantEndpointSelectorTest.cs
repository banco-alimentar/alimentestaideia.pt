namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tests
{
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Middleware;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.AspNetCore.Routing.Matching;
    using Moq;
    using Xunit;

    /// <summary>
    /// Tests tenant endpoint selection after the framework validates candidates.
    /// </summary>
    public class DoarTenantEndpointSelectorTest
    {
        /// <summary>
        /// An invalid first area candidate cannot replace the valid selected endpoint.
        /// </summary>
        [Fact]
        public async Task InvalidAreaCandidateDoesNotReplaceSelectedEndpoint()
        {
            var context = new DefaultHttpContext();
            Endpoint invalid = CreateEndpoint("invalid");
            Endpoint selected = CreateEndpoint("selected");
            var values = new RouteValueDictionary { { "area", "Admin" } };
            var candidates = new CandidateSet(new[] { invalid, selected }, new[] { values, values }, new[] { 0, 1 });
            candidates.SetValidity(0, false);
            var defaultSelector = new Mock<EndpointSelector>();
            defaultSelector.Setup(item => item.SelectAsync(context, candidates)).Callback(() =>
            {
                context.SetEndpoint(selected);
                context.Request.RouteValues = values;
            }).Returns(Task.CompletedTask);
            var selector = new DoarTenantEndpointSelector(defaultSelector.Object, new DefaultEndpointDataSource(selected));

            await selector.SelectAsync(context, candidates);

            Assert.Same(selected, context.GetEndpoint());
        }

        /// <summary>
        /// Invalid candidates cannot turn an unmatched request into a matched request.
        /// </summary>
        [Fact]
        public async Task InvalidCandidateDoesNotSelectEndpoint()
        {
            var context = new DefaultHttpContext();
            Endpoint invalid = CreateEndpoint("invalid");
            var candidates = new CandidateSet(new[] { invalid }, new[] { new RouteValueDictionary { { "area", "Admin" } } }, new[] { 0 });
            candidates.SetValidity(0, false);
            var defaultSelector = new Mock<EndpointSelector>();
            defaultSelector.Setup(item => item.SelectAsync(context, candidates)).Returns(Task.CompletedTask);
            var selector = new DoarTenantEndpointSelector(defaultSelector.Object, new DefaultEndpointDataSource(invalid));

            await selector.SelectAsync(context, candidates);

            Assert.Null(context.GetEndpoint());
        }

        /// <summary>
        /// Tenant-specific endpoints are matched without case-sensitive allocations.
        /// </summary>
        [Fact]
        public async Task TenantEndpointIsSelectedCaseInsensitively()
        {
            var context = new DefaultHttpContext();
            context.SetTenant(new Model.Tenant { Name = "demo.example" });
            Endpoint selected = CreateEndpoint("/Index");
            Endpoint tenantEndpoint = CreateEndpoint("/Tenants/Demo.Example/Pages/Index");
            var candidates = new CandidateSet(new[] { selected }, new[] { new RouteValueDictionary() }, new[] { 0 });
            var defaultSelector = new Mock<EndpointSelector>();
            defaultSelector.Setup(item => item.SelectAsync(context, candidates)).Callback(() => context.SetEndpoint(selected))
                .Returns(Task.CompletedTask);
            var selector = new DoarTenantEndpointSelector(defaultSelector.Object, new DefaultEndpointDataSource(selected, tenantEndpoint));

            await selector.SelectAsync(context, candidates);

            Assert.Same(tenantEndpoint, context.GetEndpoint());
        }

        private static Endpoint CreateEndpoint(string displayName)
        {
            return new Endpoint(_ => Task.CompletedTask, EndpointMetadataCollection.Empty, displayName);
        }
    }
}