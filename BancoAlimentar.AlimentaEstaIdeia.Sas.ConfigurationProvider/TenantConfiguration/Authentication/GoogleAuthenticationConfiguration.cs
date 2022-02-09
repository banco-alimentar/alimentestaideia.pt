// -----------------------------------------------------------------------
// <copyright file="GoogleAuthenticationConfiguration.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Authentication
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.AspNetCore.Authentication.Google;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Configure the Google Authentication for the tenant.
    /// </summary>
    public class GoogleAuthenticationConfiguration : TentantConfigurationInitializer
    {
        /// <inheritdoc/>
        public override void InitializeTenantConfiguration(Dictionary<string, string> config, IServiceCollection services)
        {
            IConfigureOptions<GoogleOptions>? msa = services
                    .Single(p => p.ServiceType == typeof(IConfigureOptions<GoogleOptions>))
                    .ImplementationInstance as IConfigureOptions<GoogleOptions>;
            if (msa != null)
            {
                services.PostConfigureAll<GoogleOptions>(options =>
                {
                    options.ClientId = config["Authentication:Google:ClientId"];
                    options.ClientSecret = config["Authentication:Google:ClientSecret"];
                });
            }
        }
    }
}
