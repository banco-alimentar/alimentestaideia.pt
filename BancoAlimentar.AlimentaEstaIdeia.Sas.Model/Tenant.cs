// -----------------------------------------------------------------------
// <copyright file="Tenant.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Model
{
    using System.ComponentModel.DataAnnotations.Schema;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;

    /// <summary>
    /// Define a tenant.
    /// </summary>
    public class Tenant
    {
        private static Tenant instance;

        /// <summary>
        /// Initializes static members of the <see cref="Tenant"/> class.
        /// </summary>
        static Tenant()
        {
            instance = new Tenant()
            {
                Id = 0,
                Name = "EmptyTenant",
            };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tenant"/> class.
        /// </summary>
        public Tenant()
        {
            this.PaymentStrategy = PaymentStrategy.SharedPaymentProcessor;
        }

        /// <summary>
        /// Gets an empty tenant to avoid null issues.
        /// </summary>
        public static Tenant EmptyTenant
        {
            get
            {
                return instance;
            }
        }

        /// <summary>
        /// Gets or sets the unique id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the tenant.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the collection of domains that this tenant belong to.
        /// </summary>
        public ICollection<DomainIdentifier> Domains { get; set; }

        /// <summary>
        /// Gets the current domain for the tenant.
        /// </summary>
        [NotMapped]
        public DomainIdentifier CurrentDomain
        {
            get
            {
                return this.Domains.FirstOrDefault();
            }
        }

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

        /// <summary>
        /// Gets or sets the default payment strategy.
        /// </summary>
        [Column(TypeName = "nvarchar(180)")]
        public PaymentStrategy PaymentStrategy { get; set; }

        /// <summary>
        /// Gets or sets the invoicing strategy.
        /// </summary>
        [Column(TypeName = "nvarchar(180)")]
        public InvoicingStrategy InvoicingStrategy { get; set; }
    }
}