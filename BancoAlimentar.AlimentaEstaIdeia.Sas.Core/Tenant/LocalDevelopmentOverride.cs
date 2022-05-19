// -----------------------------------------------------------------------
// <copyright file="LocalDevelopmentOverride.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Options;
using Microsoft.Extensions.Configuration;

/// <summary>
/// This class handle the Tenant override for the local development in localhost.
/// </summary>
public class LocalDevelopmentOverride
{
    private const string TenantOverrideKey = "Tenant-Override";
    private readonly IConfiguration configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalDevelopmentOverride"/> class.
    /// </summary>
    /// <param name="configuration">Configuration.</param>
    public LocalDevelopmentOverride(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    internal TenantData GetOverrideTenantName()
    {
        IConfigurationSection section = this.configuration.GetSection(TenantDevelopmentOptions.Section);
        if (section != null)
        {
            TenantDevelopmentOptions options = new TenantDevelopmentOptions();
            this.configuration.GetSection(TenantDevelopmentOptions.Section).Bind(options);
            return new TenantData(options.DomainIdentifier, true);
        }

        return new TenantData("localhost", true);
    }
}
