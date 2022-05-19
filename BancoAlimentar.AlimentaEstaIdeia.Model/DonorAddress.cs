// -----------------------------------------------------------------------
// <copyright file="DonorAddress.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

/// <summary>
/// Represent the donor address.
/// </summary>
public partial class DonorAddress
{
    /// <summary>
    /// Gets or sets the unique id.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the address 1.
    /// </summary>
    [Required]
    [StringLength(256)]
    [PersonalData]
    public string Address1 { get; set; }

    /// <summary>
    /// Gets or sets the address 2.
    /// </summary>
    [StringLength(256)]
    [PersonalData]
    public string Address2 { get; set; }

    /// <summary>
    /// Gets or sets the City.
    /// </summary>
    [StringLength(64)]
    [PersonalData]
    public string City { get; set; }

    /// <summary>
    /// Gets or sets the Postal Code.
    /// </summary>
    [StringLength(20)]
    [PersonalData]
    public string PostalCode { get; set; }

    /// <summary>
    /// Gets or sets the Country.
    /// </summary>
    [PersonalData]
    public string Country { get; set; }
}
