// -----------------------------------------------------------------------
// <copyright file="InvoiceRenderModel.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Model;

/// <summary>
/// Represent the render model for a Invoice.
/// </summary>
public class InvoiceRenderModel
{
    /// <summary>
    /// Gets or sets the header image for the invoice.
    /// </summary>
    public string HeaderImage { get; set; }

    /// <summary>
    /// Gets or sets the page title. (Can be the tenant name).
    /// </summary>
    public string PageTitle { get; set; }

    /// <summary>
    /// Gets or sets the footer signature image.
    /// </summary>
    public string FooterSignatureImage { get; set; }
}
