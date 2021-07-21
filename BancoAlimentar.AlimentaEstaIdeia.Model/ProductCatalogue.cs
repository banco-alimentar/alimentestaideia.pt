// <copyright file="ProductCatalogue.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    /// <summary>
    /// Represent a product catalog. Things that people can donate.
    /// </summary>
    public partial class ProductCatalogue
    {
        /// <summary>
        /// Gets or sets the unique id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [Required]
        [StringLength(64)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        [StringLength(256)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the unit of measure.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string UnitOfMeasure { get; set; }

        /// <summary>
        /// Gets or sets the cost for the donation.
        /// </summary>
        [Column(TypeName = "decimal(5, 2)")]
        public double Cost { get; set; }

        /// <summary>
        /// Gets or sets the icon url.
        /// </summary>
        [Required]
        [StringLength(1024)]
        public string IconUrl { get; set; }

        /// <summary>
        /// Gets or sets the quantity.
        /// </summary>
        [Column(TypeName = "decimal(5, 3)")]
        public double? Quantity { get; set; }

        /// <summary>
        /// Gets or sets the campaign associated with this product catalogue.
        /// </summary>
        public Campaign Campaign { get; set; }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>Return the hash code.</returns>
        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }
    }
}
