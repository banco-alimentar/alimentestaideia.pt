// -----------------------------------------------------------------------
// <copyright file="ReconciliationPlanRow.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data
{
    using System;

    /// <summary>
    /// Row in <c>dbo.EasyPayPaymentIdReconciliationPlans</c>.
    /// </summary>
    public class ReconciliationPlanRow
    {
        /// <summary>
        /// Gets or sets the plan hash.
        /// </summary>
        public string PlanEvidenceHash { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the reviewer.
        /// </summary>
        public string ApprovedBy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the approval reason.
        /// </summary>
        public string ApprovalReason { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the approved plan JSON.
        /// </summary>
        public string ApprovedPlan { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets when the plan was applied.
        /// </summary>
        public DateTime AppliedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the SQL login that applied the plan.
        /// </summary>
        public string ExecutedBy { get; set; } = string.Empty;
    }
}
