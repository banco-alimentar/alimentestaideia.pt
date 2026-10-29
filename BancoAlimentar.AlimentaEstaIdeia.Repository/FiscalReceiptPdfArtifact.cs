// -----------------------------------------------------------------------
// <copyright file="FiscalReceiptPdfArtifact.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;

    /// <summary>
    /// Represents one immutable, persisted fiscal PDF artifact.
    /// </summary>
    public sealed class FiscalReceiptPdfArtifact
    {
        /// <summary>Gets the durable immutable PDF blob name.</summary>
        public Guid BlobName { get; init; }

        /// <summary>Gets the SHA-256 checksum of the persisted PDF.</summary>
        public string Checksum { get; init; } = string.Empty;

        /// <summary>Gets the immutable PDF content.</summary>
        public byte[] Content { get; init; } = Array.Empty<byte>();

        /// <summary>Gets the persisted receipt template version.</summary>
        public string TemplateVersion { get; init; } = string.Empty;
    }
}
