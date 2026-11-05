// -----------------------------------------------------------------------
// <copyright file="ExecutionRequest.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Execution
{
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis;

    /// <summary>
    /// A reviewer decision to run against the target database.
    /// </summary>
    /// <param name="GroupId">Group ID.</param>
    /// <param name="Action">Action; validation rejects a missing action.</param>
    /// <param name="CanonicalPaymentId">Owner for ReconcileIdentifiers.</param>
    /// <param name="Reason">Approval reason.</param>
    /// <param name="ApprovedBy">Reviewer.</param>
    /// <param name="ExpectedEvidenceHash">Evidence hash the reviewer saw.</param>
    /// <param name="DryRun">Roll back instead of committing.</param>
    /// <param name="ConfirmDatabaseName">Database name typed by the reviewer; required to commit.</param>
    public sealed record ExecutionRequest(
        int GroupId,
        ReconciliationAction? Action,
        int? CanonicalPaymentId,
        string Reason,
        string ApprovedBy,
        string ExpectedEvidenceHash,
        bool DryRun,
        string? ConfirmDatabaseName);
}
