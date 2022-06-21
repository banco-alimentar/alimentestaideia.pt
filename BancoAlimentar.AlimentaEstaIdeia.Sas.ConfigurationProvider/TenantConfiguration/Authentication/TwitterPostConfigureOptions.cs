// -----------------------------------------------------------------------
// <copyright file="TwitterPostConfigureOptions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Authentication
{
    using Microsoft.AspNetCore.Authentication.Twitter;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Twitter configuration options.
    /// </summary>
    public class TwitterPostConfigureOptions : IPostConfigureOptions<TwitterOptions>
    {
        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="TwitterPostConfigureOptions"/> class.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        public TwitterPostConfigureOptions(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        /// <summary>
        /// Configure Twitter.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="twitterOptions">Options.</param>
        public void PostConfigure(string name, TwitterOptions twitterOptions)
        {
            twitterOptions.ConsumerKey = this.configuration["Authentication:Twitter:ConsumerAPIKey"];
            twitterOptions.ConsumerSecret = this.configuration["Authentication:Twitter:ConsumerSecret"];
            twitterOptions.RetrieveUserDetails = true;
        }
    }
}
