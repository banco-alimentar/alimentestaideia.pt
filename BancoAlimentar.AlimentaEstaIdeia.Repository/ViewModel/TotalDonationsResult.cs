// -----------------------------------------------------------------------
// <copyright file="TotalDonationsResult.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel;

/// <summary>
/// Total donation item.
/// </summary>
public class TotalDonationsResult
{
    /// <summary>
    /// Gets or sets the total for this product catalogue.
    /// </summary>
    public double Total { get; set; }

    /// <summary>
    /// Gets or sets the total cost.
    /// </summary>
    public double TotalCost { get; set; }

    /// <summary>
    /// Gets or sets the product catalogue id.
    /// </summary>
    public int ProductCatalogueId { get; set; }

    /// <summary>
    /// Gets or sets the name of the product catalogue.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the icon url of the product catalogue.
    /// </summary>
    public string IconUrl { get; set; }

    /// <summary>
    /// Gets or sets the unit of measure used in the product catalogue.
    /// </summary>
    public string UnitOfMeasure { get; set; }

    /// <summary>
    /// Gets or sets the description of the product catalogue.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the cost of the product catalogue.
    /// </summary>
    public double Cost { get; set; }

    /// <summary>
    /// Gets or sets the quantity for total item.
    /// </summary>
    public double? Quantity { get; set; }
}
