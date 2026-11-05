// -----------------------------------------------------------------------
// <copyright file="ProposedDecision.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis
{
    /// <summary>
    /// A reviewer's decision for one group.
    /// </summary>
    /// <param name="GroupId">Group identifier.</param>
    /// <param name="Action">Chosen action.</param>
    /// <param name="CanonicalPaymentId">Owner for <see cref="ReconciliationAction.ReconcileIdentifiers"/>.</param>
    /// <param name="Reason">Approval reason.</param>
    /// <param name="ApprovedBy">Reviewer name.</param>
    /// <param name="ExpectedEvidenceHash">Evidence hash the reviewer saw.</param>
    public sealed record ProposedDecision(
        int GroupId,
        ReconciliationAction? Action,
        int? CanonicalPaymentId,
        string? Reason,
        string? ApprovedBy,
        string? ExpectedEvidenceHash);
}
