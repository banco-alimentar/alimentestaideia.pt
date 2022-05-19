// -----------------------------------------------------------------------
// <copyright file="SubdomainNamingStrategyTest.cs" company="Federa��o Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federa��o Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tests.Naming;

using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant;
using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant.Naming;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
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

        IConfigurationBuilder builder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>() { { "SAS-BaseDomain", baseDomain } });

        var context = new DefaultHttpContext();

        context.Request.Host = new HostString($"{tenantName}.{baseDomain}", 443);

        SubdomainNamingStrategy strategy = new SubdomainNamingStrategy(builder.Build());
        TenantData name = strategy.GetTenantName(context);
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

        IConfigurationBuilder builder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>() { { "SAS-BaseDomain", baseDomain } });
        var context = new DefaultHttpContext();

        context.Request.Host = new HostString($"{baseDomain}", 443);

        SubdomainNamingStrategy strategy = new SubdomainNamingStrategy(builder.Build());
        TenantData name = strategy.GetTenantName(context);
        Assert.NotNull(name);
        Assert.Equal(tenantName, name.Name);
    }
}
