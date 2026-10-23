// -----------------------------------------------------------------------
// <copyright file="IFiscalReceiptPdfBlobStore.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;

    /// <summary>
    /// Stores immutable fiscal receipt PDF artifacts.
    /// </summary>
    public interface IFiscalReceiptPdfBlobStore
    {
        /// <summary>
        /// Saves a new immutable PDF artifact at its durable blob name.
        /// </summary>
        /// <param name="blobName">The generated artifact name.</param>
        /// <param name="content">The PDF bytes to persist.</param>
        void Save(Guid blobName, byte[] content);

        /// <summary>
        /// Loads an immutable PDF artifact by its durable blob name.
        /// </summary>
        /// <param name="blobName">The persisted artifact name.</param>
        /// <returns>The persisted PDF bytes.</returns>
        byte[] Load(Guid blobName);
    }
}
