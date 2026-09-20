// -----------------------------------------------------------------------
// <copyright file="SimulatedFiscalDocument.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;

    /// <summary>
    /// Stores development-only receipt evidence produced by the fiscal simulation workflow.
    /// This is deliberately not a fiscal document and cannot hold AT, certificate or signing data.
    /// </summary>
    public class SimulatedFiscalDocument
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the donation represented by the simulated receipt.
        /// </summary>
        public int DonationId { get; set; }

        /// <summary>
        /// Gets or sets the payment used to demonstrate the receipt workflow, when available.
        /// </summary>
        public int? PaymentId { get; set; }

        /// <summary>
        /// Gets or sets the synthetic tenant or food-bank scope used by the simulation.
        /// </summary>
        public string ScopeKey { get; set; }

        /// <summary>
        /// Gets or sets the invoicing strategy captured when the simulation was issued.
        /// </summary>
        public InvoicingStrategy InvoicingStrategy { get; set; }

        /// <summary>
        /// Gets or sets the food-bank boundary for the multi-table invoicing strategy.
        /// </summary>
        public int? FoodBankId { get; set; }

        /// <summary>
        /// Gets or sets the synthetic sequence reserved within <see cref="ScopeKey"/>.
        /// </summary>
        public int Sequence { get; set; }

        /// <summary>
        /// Gets or sets the non-fiscal identifier displayed on the simulated receipt.
        /// </summary>
        public string SyntheticIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the local integrity checksum for the simulation artifact.
        /// This value is not a fiscal signature.
        /// </summary>
        public string LocalChecksum { get; set; }

        /// <summary>
        /// Gets or sets the private, versioned QR payload for the simulated receipt.
        /// </summary>
        public string SimulationQrPayload { get; set; }

        /// <summary>
        /// Gets or sets the PNG representation of the private simulation QR payload.
        /// </summary>
        public byte[] SimulationQrPng { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Gets or sets the simulation-only artifact reference.
        /// </summary>
        public string ArtifactReference { get; set; }

        /// <summary>
        /// Gets or sets the exact host environment in which the artifact was produced.
        /// </summary>
        public string OriginEnvironment { get; set; }

        /// <summary>
        /// Gets or sets when the simulated receipt was issued.
        /// </summary>
        public DateTimeOffset IssuedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets when this immutable simulation record was created.
        /// </summary>
        public DateTimeOffset CreatedAtUtc { get; set; }
    }
}
