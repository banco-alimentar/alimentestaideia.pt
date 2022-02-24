// -----------------------------------------------------------------------
// <copyright file="TestKeyVaultConfigurationManager.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Testing.Common
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Test implementation.
    /// </summary>
    public class TestKeyVaultConfigurationManager : IKeyVaultConfigurationManager
    {
        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestKeyVaultConfigurationManager"/> class.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        public TestKeyVaultConfigurationManager(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        /// <inheritdoc/>
        public Task<bool> EnsureTenantConfigurationLoaded(int tenantId)
        {
            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        public Dictionary<string, string> GetTenantConfiguration(int tenantId)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            IEnumerable<KeyValuePair<string, string>> keyValuePairs = this.configuration.AsEnumerable();
            foreach (KeyValuePair<string, string> pair in keyValuePairs)
            {
                result.Add(pair.Key, pair.Value);
            }

            return result;
        }

        /// <inheritdoc/>
        public void LoadTenantConfiguration()
        {
        }
    }
}
