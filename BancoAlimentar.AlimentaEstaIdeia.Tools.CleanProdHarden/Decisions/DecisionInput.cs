// -----------------------------------------------------------------------
// <copyright file="DecisionInput.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Decisions
{
    using System;
    using System.Globalization;
    using System.Security.Cryptography;
    using System.Text;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis;

    /// <summary>
    /// What the reviewer entered for a group.
    /// </summary>
    /// <param name="GroupId">Group ID.</param>
    /// <param name="EasyPayPaymentId">Provider ID.</param>
    /// <param name="EvidenceHash">Evidence hash the reviewer saw.</param>
    /// <param name="Action">Chosen action.</param>
    /// <param name="CanonicalPaymentId">Owner for ReconcileIdentifiers.</param>
    /// <param name="Reason">Approval reason.</param>
    /// <param name="ApprovedBy">Reviewer.</param>
    /// <param name="Notes">Private notes, not written to the target database.</param>
    public sealed record DecisionInput(
        int GroupId,
        string EasyPayPaymentId,
        string EvidenceHash,
        ReconciliationAction? Action,
        int? CanonicalPaymentId,
        string Reason,
        string ApprovedBy,
        string? Notes)
    {
        /// <summary>
        /// Fingerprints everything that the apply must repeat exactly from the dry run.
        /// </summary>
        /// <param name="targetKey">Target key.</param>
        /// <returns>Uppercase SHA-256 hex.</returns>
        public string Fingerprint(string targetKey)
        {
            string input = string.Join(
                '\u001f',
                targetKey,
                this.GroupId.ToString(CultureInfo.InvariantCulture),
                this.EvidenceHash,
                this.Action?.ToString() ?? string.Empty,
                this.CanonicalPaymentId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                this.Reason.Trim(),
                this.ApprovedBy.Trim());
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)));
        }
    }
}
