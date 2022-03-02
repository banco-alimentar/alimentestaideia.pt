// -----------------------------------------------------------------------
// <copyright file="TenantOptions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Options
{
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Make IOptions tenant aware.
    /// </summary>
    public class TenantOptions<TOptions> : IOptions<TOptions>, IOptionsSnapshot<TOptions>
        where TOptions : class, new()
    {
        private readonly IOptionsFactory<TOptions> factory;
        private readonly IOptionsMonitorCache<TOptions> cache;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantOptions{TOptions}"/> class.
        /// </summary>
        /// <param name="factory">Option factory.</param>
        /// <param name="cache">Option monitor cache.</param>
        public TenantOptions(IOptionsFactory<TOptions> factory, IOptionsMonitorCache<TOptions> cache)
        {
            this.factory = factory;
            this.cache = cache;
        }

        /// <summary>
        /// Gets the options.
        /// </summary>
        public TOptions Value => this.Get(Options.DefaultName);

        /// <summary>
        /// Get the option.
        /// </summary>
        /// <param name="name">Name of the option.</param>
        /// <returns>A reference to the option.</returns>
        public TOptions Get(string name)
        {
            return this.cache.GetOrAdd(name, () => this.factory.Create(name));
        }
    }
}
