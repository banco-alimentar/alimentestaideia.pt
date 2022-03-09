// -----------------------------------------------------------------------
// <copyright file="TenantOptionsFactory.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Options
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Create a new options instance with configuration applied.
    /// </summary>
    /// <typeparam name="TOptions">Option type.</typeparam>
    public class TenantOptionsFactory<TOptions> : IOptionsFactory<TOptions>
        where TOptions : class, new()
    {
        private readonly IEnumerable<IConfigureOptions<TOptions>> setups;
        private readonly IEnumerable<IPostConfigureOptions<TOptions>> postConfigures;
        private readonly IHttpContextAccessor contextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantOptionsFactory{TOptions}"/> class.
        /// </summary>
        /// <param name="setups">Setups.</param>
        /// <param name="postConfigures">Post configures.</param>
        /// <param name="contextAccessor">Http context accessor.</param>
        public TenantOptionsFactory(
            IEnumerable<IConfigureOptions<TOptions>> setups,
            IEnumerable<IPostConfigureOptions<TOptions>> postConfigures,
            IHttpContextAccessor contextAccessor)
        {
            this.setups = setups;
            this.postConfigures = postConfigures;
            this.contextAccessor = contextAccessor;
        }

        /// <summary>
        /// Create a new options instance.
        /// </summary>
        /// <param name="name">Name of the option.</param>
        /// <returns>A reference to the newly created options.</returns>
        public TOptions Create(string name)
        {
            TOptions options = new TOptions();

            // Apply options setup configuration
            foreach (var setup in this.setups)
            {
                if (setup is IConfigureNamedOptions<TOptions> namedSetup)
                {
                    namedSetup.Configure(name, options);
                }
                else
                {
                    setup.Configure(options);
                }
            }

            // Apply post configuration
            foreach (var postConfig in this.postConfigures)
            {
                postConfig.PostConfigure(name, options);
            }

            return options;
        }
    }
}
