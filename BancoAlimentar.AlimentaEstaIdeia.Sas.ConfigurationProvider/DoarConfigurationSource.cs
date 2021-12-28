// -----------------------------------------------------------------------
// <copyright file="DoarConfigurationSource.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider
{
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// This is the Doar SaS configuration source implementation.
    /// </summary>
    public class DoarConfigurationSource : IConfigurationSource
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DoarConfigurationSource"/> class.
        /// </summary>
        public DoarConfigurationSource()
        {
        }

        /// <summary>
        /// Builds the configuration source.
        /// </summary>
        /// <param name="builder">A reference to the <see cref="IConfigurationBuilder"/>.</param>
        /// <returns>The <see cref="DoarConfigurationProvider"/> instance.</returns>
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return DoarConfigurationProvider.Instance;
        }
    }
}
