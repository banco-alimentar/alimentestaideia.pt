// <copyright file="DonationItem.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using System.ComponentModel.DataAnnotations.Schema;

    /// <summary>
    /// Represent a donation item.
    /// </summary>
    public partial class DonationItem
    {
        /// <summary>
        /// Gets or sets the unique id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ProductCatalogue"/>.
        /// </summary>
        public ProductCatalogue ProductCatalogue { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Donation"/>.
        /// </summary>
        public Donation Donation { get; set; }

        /// <summary>
        /// Gets or sets the Quantity for the donation item.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets the original price that the <see cref="ProductCatalogue"/> has.
        /// </summary>
        [Column(TypeName = "decimal(5, 2)")]
        public double Price { get; set; }
    }
}
