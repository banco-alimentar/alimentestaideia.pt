// -----------------------------------------------------------------------
// <copyright file="MicrosoftAccountPostConfigureOptions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Authentication;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

/// <summary>
/// Microsoft account configuration options.
/// </summary>
public class MicrosoftAccountPostConfigureOptions : IPostConfigureOptions<MicrosoftAccountOptions>
{
    private readonly IConfiguration configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="MicrosoftAccountPostConfigureOptions"/> class.
    /// </summary>
    /// <param name="configuration">Configuration.</param>
    public MicrosoftAccountPostConfigureOptions(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    /// <summary>
    /// Configure Google.
    /// </summary>
    /// <param name="name">Name.</param>
    /// <param name="microsoftOptions">Google options.</param>
    public void PostConfigure(string name, MicrosoftAccountOptions microsoftOptions)
    {
        microsoftOptions.ClientId = this.configuration["Authentication:Microsoft:ClientId"];
        microsoftOptions.ClientSecret = this.configuration["Authentication:Microsoft:ClientSecret"];
        microsoftOptions.SaveTokens = true;
        microsoftOptions.Scope.Add("email");
        microsoftOptions.Scope.Add("openid");
        microsoftOptions.Scope.Add("profile");
        microsoftOptions.Scope.Add("User.ReadBasic.All");
        microsoftOptions.Events.OnCreatingTicket = ctx =>
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
