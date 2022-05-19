// -----------------------------------------------------------------------
// <copyright file="DoarConfigurationSource.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;

using Microsoft.Extensions.Configuration;

/// <summary>
/// This is the Doar SaS configuration source implementation.
/// </summary>
public class DoarConfigurationSource : IConfigurationSource
{
    private readonly IConfiguration configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="DoarConfigurationSource"/> class.
    /// </summary>
    /// <param name="configuration">Current configuration.</param>
    public DoarConfigurationSource(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    /// <summary>
    /// Builds the configuration source.
    /// </summary>
    /// <param name="builder">A reference to the <see cref="IConfigurationBuilder"/>.</param>
    /// <returns>The <see cref="DoarConfigurationProvider"/> instance.</returns>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        // InfrastructureDbContext context = new InfrastructureDbContext();
        // List<KeyVaultConfiguration> keyVaultConfigurations = context.KeyVaultConfigurations.ToList();

        // list all
        return DoarConfigurationProvider.Instance;
    }
}
