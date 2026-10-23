// -----------------------------------------------------------------------
// <copyright file="SimulatedFiscalReceiptViewModelFactory.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Model;

    /// <summary>
    /// Projects persisted simulated-document evidence into the simulation receipt model.
    /// </summary>
    public sealed class SimulatedFiscalReceiptViewModelFactory
    {
        /// <summary>
        /// Creates the model used solely to render a development simulation receipt.
        /// </summary>
        /// <param name="document">The persisted simulation document.</param>
        /// <returns>The simulation receipt model with its mandatory warning.</returns>
        public SimulatedFiscalReceiptViewModel Create(SimulatedFiscalDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return new SimulatedFiscalReceiptViewModel
            {
                SyntheticIdentifier = document.SyntheticIdentifier ?? string.Empty,
                LocalChecksum = document.LocalChecksum ?? string.Empty,
                SimulationQrPayload = document.SimulationQrPayload ?? string.Empty,
                ArtifactReference = document.ArtifactReference ?? string.Empty,
                OriginEnvironment = document.OriginEnvironment ?? string.Empty,
            };
        }
    }
}
