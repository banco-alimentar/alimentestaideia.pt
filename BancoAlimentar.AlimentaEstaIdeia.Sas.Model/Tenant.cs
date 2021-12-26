// -----------------------------------------------------------------------
// <copyright file="Tenant.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Model
{
    /// <summary>
    /// Define a tenant.
    /// </summary>
    public class Tenant
    {
        /// <summary>
        /// Gets or sets the unique id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the tenant.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the public id of the tenant.
        /// </summary>
        public Guid PublicId { get; set; }

        /// <summary>
        /// Gets or sets when the tenant was created.
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Gets or sets the Azure Key Configurations.
        /// </summary>
        public ICollection<KeyVaultConfiguration> KeyVaultConfigurations { get; set; }
    }
}