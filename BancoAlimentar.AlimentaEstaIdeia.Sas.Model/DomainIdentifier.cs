// -----------------------------------------------------------------------
// <copyright file="DomainIdentifier.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Model
{
    using System;

    /// <summary>
    /// Represent the Domain Identifier for a particular tenant.
    /// </summary>
    public class DomainIdentifier
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DomainIdentifier"/> class.
        /// </summary>
        public DomainIdentifier()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainIdentifier"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="domainName">The domain name.</param>
        /// <param name="environment">The environment.</param>
        /// <param name="created">The created date.</param>
        /// <param name="tenantId">The tenant Id.</param>
        public DomainIdentifier(int id, string domainName, string environment, DateTime created, int tenantId)
        {
            (this.Id, this.DomainName, this.Environment, this.Created, this.TenantId) = (id, domainName, environment, created, tenantId);
        }

        /// <summary>
        /// Gets or sets the unique id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the domain, or subdomain name.
        /// </summary>
        public string DomainName { get; set; }

        /// <summary>
        /// Gets or sets the environment for the domain.
        /// </summary>
        public string Environment { get; set; }

        /// <summary>
        /// Gets or sets when the record was created.
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Gets or sets when the tenant Id.
        /// </summary>
        private int TenantId { get; set; }
    }
}
