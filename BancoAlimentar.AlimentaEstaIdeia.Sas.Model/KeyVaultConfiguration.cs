// -----------------------------------------------------------------------
// <copyright file="KeyVaultConfiguration.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Model
{
    using System;

    /// <summary>
    /// Define the Key Vault configuration.
    /// </summary>
    public class KeyVaultConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVaultConfiguration"/> class.
        /// </summary>
        public KeyVaultConfiguration()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVaultConfiguration"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="vault">The vault.</param>
        /// <param name="created">The created date.</param>
        /// <param name="environment">The environment.</param>
        /// <param name="tenantId">The tenant Id.</param>
        public KeyVaultConfiguration(int id, Uri vault, DateTime created, string environment, int tenantId)
        {
            (this.Id, this.Vault, this.Created, this.Environment, this.TenantId) = (id, vault, created, environment, tenantId);
        }

        /// <summary>
        /// Gets or sets the unique id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets Azure Key Vault url.
        /// </summary>
        public Uri Vault { get; set; }

        /// <summary>
        /// Gets or sets the environment.
        /// </summary>
        public string Environment { get; set; }

        /// <summary>
        /// Gets or sets when the Configuration was created.
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Gets or sets when the tenant Id.
        /// </summary>
        private int TenantId { get; set; }
    }
}
