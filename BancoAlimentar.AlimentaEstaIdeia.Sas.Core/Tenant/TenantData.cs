// -----------------------------------------------------------------------
// <copyright file="TenantData.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Options;

    /// <summary>
    /// Data about the tenant.
    /// </summary>
    public sealed class TenantData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TenantData"/> class.
        /// </summary>
        /// <param name="name">Name of the tenant.</param>
        /// <param name="isLocalhost">Set if localhost.</param>
        public TenantData(string name, bool isLocalhost)
        {
            this.Name = name;
            this.Created = DateTime.UtcNow;
            this.IsLocalhost = isLocalhost;
            this.TenantDevelopmentOptions = TenantDevelopmentOptions.ProductionOptions;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantData"/> class.
        /// </summary>
        /// <param name="name">Name of the tenant.</param>
        /// <param name="isLocalhost">Set if localhost.</param>
        /// <param name="tenantDevelopmentOptions">Tenan development options.</param>
        public TenantData(string name, bool isLocalhost, TenantDevelopmentOptions tenantDevelopmentOptions)
        {
            this.Name = name;
            this.Created = DateTime.UtcNow;
            this.IsLocalhost = isLocalhost;
            this.TenantDevelopmentOptions = tenantDevelopmentOptions;
        }

        /// <summary>
        /// Gets the tenant name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the Tenant data is for the localhost tenant.
        /// </summary>
        public bool IsLocalhost { get; private set; }

        /// <summary>
        /// Gets TenantDevelopmentOptions.
        /// </summary>
        public TenantDevelopmentOptions TenantDevelopmentOptions { get; private set; }

        /// <summary>
        /// Gets when the tenant was created.
        /// </summary>
        public DateTime Created { get; private set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Name: {this.Name}, IsLocalHost: {this.IsLocalhost}";
        }
    }
}
