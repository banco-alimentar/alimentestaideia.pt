// -----------------------------------------------------------------------
// <copyright file="IFiscalQrPayloadBuilder.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    /// <summary>
    /// Builds the Portuguese fiscal QR payload from a final document snapshot.
    /// </summary>
    public interface IFiscalQrPayloadBuilder
    {
        /// <summary>
        /// Builds the official QR payload.
        /// </summary>
        /// <param name="snapshot">The immutable issued-document fiscal snapshot.</param>
        /// <returns>The asterisk-delimited QR payload.</returns>
        string Build(FiscalQrPayloadSnapshot snapshot);
    }
}
