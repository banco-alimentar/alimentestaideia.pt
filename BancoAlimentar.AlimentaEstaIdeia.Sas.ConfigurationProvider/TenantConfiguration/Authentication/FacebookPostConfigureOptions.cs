// -----------------------------------------------------------------------
// <copyright file="FacebookPostConfigureOptions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Authentication;

using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

/// <summary>
/// Facebook configuration options.
/// </summary>
public class FacebookPostConfigureOptions : IPostConfigureOptions<FacebookOptions>
{
    private readonly IConfiguration configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="FacebookPostConfigureOptions"/> class.
    /// </summary>
    /// <param name="configuration">Configuration.</param>
    public FacebookPostConfigureOptions(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    /// <summary>
    /// Configure Facebook.
    /// </summary>
    /// <param name="name">Name.</param>
    /// <param name="facebookOptions">Options.</param>
    public void PostConfigure(string name, FacebookOptions facebookOptions)
    {
        facebookOptions.AppId = this.configuration["Authentication:Facebook:AppId"];
        facebookOptions.AppSecret = this.configuration["Authentication:Facebook:AppSecret"];
    }
}
