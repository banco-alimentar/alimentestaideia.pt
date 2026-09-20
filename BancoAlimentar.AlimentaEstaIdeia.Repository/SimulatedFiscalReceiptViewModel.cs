// -----------------------------------------------------------------------
// <copyright file="SimulatedFiscalReceiptViewModel.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    /// <summary>
    /// Represents the immutable, explicitly non-fiscal data displayed on a simulated receipt.
    /// </summary>
    public sealed class SimulatedFiscalReceiptViewModel
    {
        /// <summary>
        /// Gets the mandatory notice printed on every simulation artifact.
        /// </summary>
        public string SimulationNotice { get; init; } = "SIMULAÇÃO — SEM VALIDADE FISCAL";

        /// <summary>
        /// Gets the reserved synthetic receipt identifier.
        /// </summary>
        public string SyntheticIdentifier { get; init; } = string.Empty;

        /// <summary>
        /// Gets the locally-computed simulation checksum.
        /// </summary>
        public string LocalChecksum { get; init; } = string.Empty;

        /// <summary>
        /// Gets the private simulation QR payload.
        /// </summary>
        public string SimulationQrPayload { get; init; } = string.Empty;

        /// <summary>
        /// Gets the immutable simulation artifact reference.
        /// </summary>
        public string ArtifactReference { get; init; } = string.Empty;

        /// <summary>
        /// Gets the host environment that created the simulation.
        /// </summary>
        public string OriginEnvironment { get; init; } = string.Empty;
    }
}
