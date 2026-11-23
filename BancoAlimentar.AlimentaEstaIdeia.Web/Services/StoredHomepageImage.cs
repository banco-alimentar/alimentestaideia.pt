// -----------------------------------------------------------------------
// <copyright file="StoredHomepageImage.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services
{
    /// <summary>
    /// Stored image file details.
    /// </summary>
    /// <param name="FileName">Generated filename.</param>
    /// <param name="ContentType">Verified content type.</param>
    public sealed record StoredHomepageImage(string FileName, string ContentType);
}
