// -----------------------------------------------------------------------
// <copyright file="GooglePostConfigureOptions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Authentication;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

/// <summary>
/// Google configuration options.
/// </summary>
public class GooglePostConfigureOptions : IPostConfigureOptions<GoogleOptions>
{
    private readonly IConfiguration configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="GooglePostConfigureOptions"/> class.
    /// </summary>
    /// <param name="configuration">Configuration.</param>
    public GooglePostConfigureOptions(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    /// <summary>
    /// Configure Google.
    /// </summary>
    /// <param name="name">Name.</param>
    /// <param name="options">Options.</param>
    public void PostConfigure(string name, GoogleOptions options)
    {
        options.ClientId = this.configuration["Authentication:Google:ClientId"];
        options.ClientSecret = this.configuration["Authentication:Google:ClientSecret"];
        options.ClientId = this.configuration["Authentication:Google:ClientId"];
        options.ClientId = this.configuration["Authentication:Google:ClientSecret"];
        options.ClaimActions.MapJsonKey("urn:google:picture", "picture", "url");
        options.ClaimActions.MapJsonKey("urn:google:locale", "locale", "string");
        options.SaveTokens = true;
        options.Events.OnCreatingTicket = ctx =>
        {
            List<AuthenticationToken> tokens = ctx.Properties.GetTokens().ToList();
            tokens.Add(new AuthenticationToken()
            {
                Name = "TicketCreated",
                Value = DateTime.UtcNow.ToString(),
            });
            ctx.Properties.StoreTokens(tokens);
            return Task.CompletedTask;
        };
    }
}
