// -----------------------------------------------------------------------
// <copyright file="Donation2ProductViewModel.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Model.Pages.Donation2
{
    /// <summary>
    /// Represents a product displayed on the Donation2 page.
    /// </summary>
    public sealed class Donation2ProductViewModel
    {
        /// <summary>
        /// Gets or sets the product identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the product name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the product icon file name.
        /// </summary>
        public string IconUrl { get; set; }

        /// <summary>
        /// Gets or sets the unit of measure.
        /// </summary>
        public string UnitOfMeasure { get; set; }

        /// <summary>
        /// Gets or sets the physical quantity represented by one donation unit.
        /// </summary>
        public double QuantityPerUnit { get; set; }

        /// <summary>
        /// Gets or sets the price of one donation unit.
        /// </summary>
        public double Cost { get; set; }

        /// <summary>
        /// Gets or sets the selected quantity.
        /// </summary>
        public int SelectedQuantity { get; set; }
    }
}