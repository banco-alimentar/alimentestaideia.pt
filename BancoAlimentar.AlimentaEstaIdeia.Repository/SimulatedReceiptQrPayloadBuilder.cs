// -----------------------------------------------------------------------
// <copyright file="SimulatedReceiptQrPayloadBuilder.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Model;

    /// <summary>
    /// Builds a deliberately private QR representation that cannot match the AT QR grammar.
    /// </summary>
    public sealed class SimulatedReceiptQrPayloadBuilder : ISimulatedReceiptQrPayloadBuilder
    {
        private const string PayloadVersion = "AEI-SIM/v1";

        /// <inheritdoc />
        public string Build(SimulatedFiscalDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (string.IsNullOrWhiteSpace(document.SyntheticIdentifier))
            {
                throw new InvalidOperationException("A simulation QR payload requires a synthetic identifier.");
            }

            return PayloadVersion + ";simulation-id=" + Uri.EscapeDataString(document.SyntheticIdentifier);
        }
    }
}
