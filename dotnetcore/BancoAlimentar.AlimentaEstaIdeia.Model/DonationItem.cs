// <copyright file="DonationItem.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
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
    }
}
