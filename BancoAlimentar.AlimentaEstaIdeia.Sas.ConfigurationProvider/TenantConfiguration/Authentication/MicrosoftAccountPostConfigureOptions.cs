// -----------------------------------------------------------------------
// <copyright file="MicrosoftAccountPostConfigureOptions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Authentication
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Microsoft account configuration options.
    /// </summary>
    public class MicrosoftAccountPostConfigureOptions : IPostConfigureOptions<MicrosoftAccountOptions>
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<MicrosoftAccountPostConfigureOptions> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MicrosoftAccountPostConfigureOptions"/> class.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="logger">Logger.</param>
        public MicrosoftAccountPostConfigureOptions(
            IConfiguration configuration,
            ILogger<MicrosoftAccountPostConfigureOptions> logger)
        {
            this.configuration = configuration;
            this.logger = logger;
        }

        /// <summary>
        /// Configure Microsoft account authentication.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="microsoftOptions">Microsoft options.</param>
        public void PostConfigure(string name, MicrosoftAccountOptions microsoftOptions)
        {
            microsoftOptions.ClientId = this.configuration["Authentication:Microsoft:ClientId"];
            microsoftOptions.ClientSecret = this.configuration["Authentication:Microsoft:ClientSecret"];
            if (KeyVaultServicePrincipalSettings.LooksLikeSecretIdentifier(microsoftOptions.ClientSecret))
            {
                this.logger.LogError(
                    "Authentication:Microsoft:ClientSecret looks like a secret id (GUID), not a secret value. " +
                    "Update tenant Key Vault secret Authentication--Microsoft--ClientSecret for app {ClientId}.",
                    microsoftOptions.ClientId);
            }

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
            microsoftOptions.Events.OnRemoteFailure = context =>
            {
                string failureMessage = context.Failure?.Message ?? "Unknown Microsoft sign-in failure.";
                bool invalidClientSecret = KeyVaultAuthenticationFailureHelper.IsAuthenticationFailure(context.Failure);
                this.logger.LogError(
                    context.Failure,
                    "Microsoft account remote authentication failed. ClientId={ClientId}. InvalidClientSecret={InvalidClientSecret}",
                    microsoftOptions.ClientId,
                    invalidClientSecret);

                string userMessage = invalidClientSecret
                    ? "Microsoft sign-in is not available right now. Please sign in with email and password, or try again later."
                    : "Microsoft sign-in failed. Please try again or use email and password.";

                string loginPath = "/Identity/Account/Login?error=" + Uri.EscapeDataString(userMessage);
                context.Response.Redirect(loginPath);
                context.HandleResponse();
                return Task.CompletedTask;
            };
        }
    }
}
