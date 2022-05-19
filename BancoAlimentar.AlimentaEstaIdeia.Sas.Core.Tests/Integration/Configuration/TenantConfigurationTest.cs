// -----------------------------------------------------------------------
// <copyright file="TenantConfigurationTest.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tests.Integration.Configuration;

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

    /// <summary>
    /// Checks if an anonymous user can make a donation without a receipt.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task Check_Tenant_Configuration()
    {
        string tenantName = "alimentaestaideia-developer.azurewebsites.net";
        
        // Arrange
        HttpResponseMessage tenantIndex = await this.client.GetAsync("/Tenant/Index");
        string json = await tenantIndex.Content.ReadAsStringAsync();
        // Act
        
        JObject obj = JObject.Parse(json);


        // Verify if it was able to redirect to Payment page.
        Assert.Equal(tenantName, obj["Tenant"]["Name"].Value<string>());
    }
}
