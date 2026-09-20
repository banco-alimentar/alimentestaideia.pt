// -----------------------------------------------------------------------
// <copyright file="SimulationChecksumService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Globalization;
    using System.Security.Cryptography;
    using System.Text;
    using BancoAlimentar.AlimentaEstaIdeia.Model;

    /// <summary>
    /// Calculates deterministic local checksums for development-only receipt simulations.
    /// </summary>
    public class SimulationChecksumService : ISimulationChecksumService
    {
        /// <inheritdoc />
        public string Calculate(SimulatedFiscalDocument document)
        {
            if (document == null || document.DonationId <= 0 || document.Sequence <= 0 ||
                string.IsNullOrWhiteSpace(document.ScopeKey) || string.IsNullOrWhiteSpace(document.SyntheticIdentifier) ||
                !string.Equals(document.OriginEnvironment, "Development", StringComparison.Ordinal))
            {
                throw new ArgumentException("A complete Development simulation document is required for a local checksum.", nameof(document));
            }

            string canonicalValue = string.Join(
                "|",
                "AEI-SIM-CHECKSUM/v1",
                document.DonationId.ToString(CultureInfo.InvariantCulture),
                document.PaymentId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                document.ScopeKey,
                document.Sequence.ToString(CultureInfo.InvariantCulture),
                document.SyntheticIdentifier,
                document.OriginEnvironment);
            byte[] checksum = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalValue));
            return "SIMCHK-" + Convert.ToHexString(checksum);
        }
    }
}
