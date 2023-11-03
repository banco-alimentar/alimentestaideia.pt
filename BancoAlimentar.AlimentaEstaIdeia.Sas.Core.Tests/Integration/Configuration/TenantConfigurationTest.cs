// -----------------------------------------------------------------------
// <copyright file="TenantConfigurationTest.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tests.Integration.Configuration
{
    using BancoAlimentar.AlimentaEstaIdeia.Testing.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Tenant default configuration integration tests.
    /// </summary>
    public class TenantConfigurationTest : IClassFixture<CustomWebApplicationFactory<Startup>>
    {
        private readonly HttpClient client;
        private readonly CustomWebApplicationFactory<Startup> factory;
        private readonly ITestOutputHelper outputHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantConfigurationTest"/> class.
        /// </summary>
        /// <param name="factory">Factory class.</param>
        /// <param name="outputHelper">Test output helper.</param>
        public TenantConfigurationTest(CustomWebApplicationFactory<Startup> factory, ITestOutputHelper outputHelper)
        {
            this.factory = factory;
            this.outputHelper = outputHelper;
            this.client = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var serviceProvider = services.BuildServiceProvider();
                });
            })
            .CreateClient();
        }
    }
}
