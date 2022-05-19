// <copyright file="Campaign.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>

namespace BancoAlimentar.AlimentaEstaIdeia.Model;

using System;
using System.Collections.Generic;

/// <summary>
/// Represent a donation campaign.
/// </summary>
public class Campaign
{
    /// <summary>
    /// Gets or sets the campaign id.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the campaign number.
    /// </summary>
    public string Number { get; set; }

    /// <summary>
    /// Gets or sets when the campaign starts.
    /// </summary>
    public DateTime Start { get; set; }

    /// <summary>
    /// Gets or sets when the campaign ends.
    /// </summary>
    public DateTime End { get; set; }

    /// <summary>
    /// Gets or sets when the campaign ends for reporting purposed.
    /// </summary>
    public DateTime ReportEnd { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets where this is the default campaign when there is no campaign.
    /// </summary>
    public bool IsDefaultCampaign { get; set; }

    /// <summary>
    /// Gets or sets the associated product catalogues that belong to this campaign.
    /// </summary>
    public ICollection<ProductCatalogue> ProductCatalogues { get; set; }
}
