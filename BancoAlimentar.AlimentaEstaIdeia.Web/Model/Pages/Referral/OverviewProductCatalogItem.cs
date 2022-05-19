// -----------------------------------------------------------------------
// <copyright file="OverviewProductCatalogItem.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Model.Pages.Referral;

/// <summary>
/// Represent a product catalog overview item.
/// </summary>
public class OverviewProductCatalogItem
{
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// Gets or sets the unit.
    /// </summary>
    public string Unit { get; set; }
}
