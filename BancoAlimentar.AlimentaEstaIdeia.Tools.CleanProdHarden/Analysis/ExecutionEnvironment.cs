// -----------------------------------------------------------------------
// <copyright file="ExecutionEnvironment.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Database facts read inside the execution transaction and used for validation.
    /// </summary>
    public sealed record ExecutionEnvironment
    {
        /// <summary>
        /// Gets the group's current evidence hash.
        /// </summary>
        public string CurrentEvidenceHash { get; init; } = string.Empty;

        /// <summary>
        /// Gets whether the login has VIEW DEFINITION on the database.
        /// </summary>
        public bool HasViewDefinitionPermission { get; init; }

        /// <summary>
        /// Gets foreign keys to Payments that are composite or do not reference Id.
        /// </summary>
        public IReadOnlyList<string> UnsupportedForeignKeys { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the number of enabled triggers on Payments.
        /// </summary>
        public int EnabledPaymentTriggerCount { get; init; }

        /// <summary>
        /// Gets the rows that reference group members.
        /// </summary>
        public IReadOnlyList<PaymentDependency> Dependencies { get; init; } = Array.Empty<PaymentDependency>();

        /// <summary>
        /// Gets the number of provider IDs longer than the migration's limit.
        /// </summary>
        public long OverlengthProviderIds { get; init; }

        /// <summary>
        /// Gets the number of conflicting invoice sequence groups.
        /// </summary>
        public long ConflictingInvoiceGroups { get; init; }

        /// <summary>
        /// Gets the members that already have reconciliation audit rows.
        /// </summary>
        public IReadOnlySet<int> ReconciliationAuditPaymentIds { get; init; } = new HashSet<int>();

        /// <summary>
        /// Gets the members that already have retirement audit rows.
        /// </summary>
        public IReadOnlySet<int> RetirementAuditPaymentIds { get; init; } = new HashSet<int>();

        /// <summary>
        /// Gets the members that already have deletion audit rows.
        /// </summary>
        public IReadOnlySet<int> DeletionAuditPaymentIds { get; init; } = new HashSet<int>();

        /// <summary>
        /// Gets whether dependency checks are reliable.
        /// </summary>
        public bool DependencyCheckSupported => this.HasViewDefinitionPermission && this.UnsupportedForeignKeys.Count == 0;

        /// <summary>
        /// Returns true when DeleteGroup passes the permission, trigger, and dependency checks for a group.
        /// </summary>
        /// <param name="group">Evaluated group.</param>
        /// <returns>True when the environment allows deletion.</returns>
        public bool AllowsDeletion(GroupEvaluation group)
        {
            ArgumentNullException.ThrowIfNull(group);
            HashSet<int> memberIds = group.Members.Select(member => member.PaymentId).ToHashSet();
            return this.DependencyCheckSupported
                && this.EnabledPaymentTriggerCount == 0
                && !this.Dependencies.Any(dependency => memberIds.Contains(dependency.PaymentId) && dependency.ReferenceCount > 0);
        }
    }
}
