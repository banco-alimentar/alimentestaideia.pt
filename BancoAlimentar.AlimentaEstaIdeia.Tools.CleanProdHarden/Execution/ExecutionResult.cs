// -----------------------------------------------------------------------
// <copyright file="ExecutionResult.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Execution
{
    using System;
    using System.Collections.Generic;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data;

    /// <summary>
    /// Outcome of a dry run or apply.
    /// </summary>
    public sealed record ExecutionResult
    {
        /// <summary>
        /// Gets the request.
        /// </summary>
        public required ExecutionRequest Request { get; init; }

        /// <summary>
        /// Gets whether every step and check passed.
        /// </summary>
        public bool Succeeded { get; init; }

        /// <summary>
        /// Gets whether the transaction was committed.
        /// </summary>
        public bool Committed { get; init; }

        /// <summary>
        /// Gets the target the request ran against.
        /// </summary>
        public TargetIdentity? Target { get; init; }

        /// <summary>
        /// Gets the blocking issues.
        /// </summary>
        public IReadOnlyList<ValidationIssue> Issues { get; init; } = Array.Empty<ValidationIssue>();

        /// <summary>
        /// Gets the step log.
        /// </summary>
        public IReadOnlyList<string> Log { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the payments whose provider ID was cleared or whose row was deleted.
        /// </summary>
        public IReadOnlyList<int> AffectedPaymentIds { get; init; } = Array.Empty<int>();

        /// <summary>
        /// Gets the plan hash written to <c>EasyPayPaymentIdReconciliationPlans</c>.
        /// </summary>
        public string? PlanEvidenceHash { get; init; }

        /// <summary>
        /// Gets the plan JSON written to <c>EasyPayPaymentIdReconciliationPlans</c>.
        /// </summary>
        public string? ApprovedPlanJson { get; init; }

        /// <summary>
        /// Gets the evidence hash seen inside the transaction.
        /// </summary>
        public string? ActualEvidenceHash { get; init; }

        /// <summary>
        /// Gets when the run finished.
        /// </summary>
        public DateTime FinishedAtUtc { get; init; } = DateTime.UtcNow;
    }
}
