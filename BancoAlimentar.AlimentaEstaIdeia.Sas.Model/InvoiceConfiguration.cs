// -----------------------------------------------------------------------
// <copyright file="InvoiceConfiguration.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Model;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Invoice configuration for the tenant.
/// </summary>
public class InvoiceConfiguration
{
    /// <summary>
    /// Gets or sets the unique id.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the header image for the invoice.
    /// </summary>
    public string HeaderImage { get; set; }

    /// <summary>
    /// Gets or sets the footer signature image.
    /// </summary>
    public string FooterSignatureImage { get; set; }
}
