// -----------------------------------------------------------------------
// <copyright file="ISimulatedFiscalReceiptPdfBlobStore.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services.Invoices
{
    /// <summary>
    /// Stores Development-only simulation PDFs below their isolated blob prefix.
    /// </summary>
    public interface ISimulatedFiscalReceiptPdfBlobStore
    {
        /// <summary>
        /// Determines whether an artifact is already persisted.
        /// </summary>
        /// <param name="artifactReference">The simulation-only blob reference.</param>
        /// <returns>A value indicating whether the artifact exists.</returns>
        bool Exists(string artifactReference);

        /// <summary>
        /// Loads an existing simulation artifact.
        /// </summary>
        /// <param name="artifactReference">The simulation-only blob reference.</param>
        /// <returns>The stored PDF bytes.</returns>
        byte[] Load(string artifactReference);

        /// <summary>
        /// Saves a new immutable simulation artifact.
        /// </summary>
        /// <param name="artifactReference">The simulation-only blob reference.</param>
        /// <param name="content">The PDF bytes.</param>
        void Save(string artifactReference, byte[] content);
    }
}
