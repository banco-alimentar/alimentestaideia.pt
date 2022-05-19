// -----------------------------------------------------------------------
// <copyright file="DomainIdentifier.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Model;

using System;

/// <summary>
/// Represent the Domain Identifier for a particular tenant.
/// </summary>
public class DomainIdentifier
{
    /// <summary>
    /// Gets or sets the unique id.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the domain, or subdomain name.
    /// </summary>
    public string DomainName { get; set; }

    /// <summary>
    /// Gets or sets the environment for the domain.
    /// </summary>
    public string Environment { get; set; }

    /// <summary>
    /// Gets or sets when the record was created.
    /// </summary>
    public DateTime Created { get; set; }
}
