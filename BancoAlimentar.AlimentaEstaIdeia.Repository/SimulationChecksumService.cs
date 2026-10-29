// -----------------------------------------------------------------------
// <copyright file="SimulationChecksumService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Security.Cryptography;
    using System.Text;
    using BancoAlimentar.AlimentaEstaIdeia.Model;

    /// <summary>
    /// Calculates a local SHA-256 checksum for simulated receipt data.
    /// </summary>
    public class SimulationChecksumService : ISimulationChecksumService
    {
        /// <inheritdoc />
        public string Calculate(SimulatedFiscalDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            string content = string.Join(
                ";",
                document.TenantId,
                document.DonationId,
                document.PaymentId,
                document.ScopeKey,
                document.Sequence,
                document.SyntheticIdentifier,
                document.SimulationQrPayload,
                document.OriginEnvironment);
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
        }
    }
}
