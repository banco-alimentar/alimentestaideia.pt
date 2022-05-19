// -----------------------------------------------------------------------
// <copyright file="SubscriptionDonations.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model;

/// <summary>
/// Represent the One to many relationship between <see cref="Subscription"/> and <see cref="Donation"/>.
/// </summary>
public class SubscriptionDonations
{
    /// <summary>
    /// Gets or sets the table Id.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="Subscription"/>.
    /// </summary>
    public Subscription Subscription { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="Donation"/>.
    /// </summary>
    public Donation Donation { get; set; }
}
