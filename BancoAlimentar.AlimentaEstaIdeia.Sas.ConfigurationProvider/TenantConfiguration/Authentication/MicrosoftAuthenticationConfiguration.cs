// -----------------------------------------------------------------------
// <copyright file="MicrosoftAuthenticationConfiguration.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Authentication
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Configure the Microsoft Authentication for the tenant.
    /// </summary>
    public class MicrosoftAuthenticationConfiguration : TentantConfigurationInitializer
    {
        /// <inheritdoc/>
        public override void InitializeTenantConfiguration(Dictionary<string, string> config, IServiceCollection services)
        {
            IConfigureOptions<MicrosoftAccountOptions>? msa = services
                    .Single(p => p.ServiceType == typeof(IConfigureOptions<MicrosoftAccountOptions>))
                    .ImplementationInstance as IConfigureOptions<MicrosoftAccountOptions>;
            if (msa != null)
            {
                services.PostConfigureAll<MicrosoftAccountOptions>(options =>
                {
                    options.ClientId = config["Authentication:Microsoft:ClientId"];
                    options.ClientSecret = config["Authentication:Microsoft:ClientSecret"];
                });
            }
        }
    }
}
