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
    /// Builds a private, clearly versioned QR payload for simulated receipts.
    /// </summary>
    public class SimulatedReceiptQrPayloadBuilder : ISimulatedReceiptQrPayloadBuilder
    {
        /// <inheritdoc />
        public string Build(SimulatedFiscalDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (string.IsNullOrWhiteSpace(document.SyntheticIdentifier))
            {
                throw new InvalidOperationException("Simulation QR payload requires a synthetic identifier.");
            }

            return $"AEI-SIM/v1;id={Uri.EscapeDataString(document.SyntheticIdentifier)}";
        }
    }
}
