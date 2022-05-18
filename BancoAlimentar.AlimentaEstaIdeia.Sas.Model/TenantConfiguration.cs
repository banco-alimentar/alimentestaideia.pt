// -----------------------------------------------------------------------
// <copyright file="TenantConfiguration.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Model
{
    using System;

    /// <summary>
    /// Define the tenant configuration.
    /// </summary>
    public class TenantConfiguration
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the resource group identifier.
        /// </summary>
        /// <value>
        /// The resource group identifier.
        /// </value>
        public Guid ResourceGroupId { get; set; }

        /// <summary>
        /// Gets or sets the key vault secret identifier.
        /// </summary>
        /// <value>
        /// The key vault secret identifier.
        /// </value>
        public string KeyVaultSecretId { get; set; }

        /// <summary>
        /// Gets or sets the deployment identifier.
        /// </summary>
        /// <value>
        /// The deployment identifier.
        /// </value>
        public Guid DeploymentId { get; set; }

        /// <summary>
        /// Gets or sets the tenant.
        /// </summary>
        /// <value>
        /// The tenant.
        /// </value>
        public Tenant Tenant { get; set; }

        /// <summary>
        /// Gets or sets the test.
        /// </summary>
        /// <value>
        /// The test.
        /// </value>
        public string Test { get; set; }
    }
}
