// -----------------------------------------------------------------------
// <copyright file="Donation2ItemInputModel.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Model.Pages.Donation2
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Represents a product quantity submitted by the Donation2 form.
    /// </summary>
    public sealed class Donation2ItemInputModel
    {
        /// <summary>
        /// Gets or sets the product catalogue identifier.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int ProductId { get; set; }

        /// <summary>
        /// Gets or sets the requested product quantity.
        /// </summary>
        [Range(0, 9999)]
        public int Quantity { get; set; }
    }
}