// -----------------------------------------------------------------------
// <copyright file="DecisionStore.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Decisions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Execution;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Reads and writes reviewer decisions in the local store.
    /// </summary>
    public sealed class DecisionStore
    {
        private readonly IDbContextFactory<DecisionStoreContext> factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DecisionStore"/> class.
        /// </summary>
        /// <param name="factory">Context factory.</param>
        public DecisionStore(IDbContextFactory<DecisionStoreContext> factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// Raised with the group ID after a decision changes.
        /// </summary>
        public event EventHandler<int>? DecisionChanged;

        /// <summary>
        /// Creates the store when missing.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task.</returns>
        public async Task EnsureCreatedAsync(CancellationToken cancellationToken)
        {
            await using DecisionStoreContext db = await this.factory.CreateDbContextAsync(cancellationToken);
            await db.Database.EnsureCreatedAsync(cancellationToken);
        }

        /// <summary>
        /// Gets the decision for a group.
        /// </summary>
        /// <param name="targetKey">Target key.</param>
        /// <param name="groupId">Group ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The decision, or null.</returns>
        public async Task<GroupDecisionRecord?> GetAsync(string targetKey, int groupId, CancellationToken cancellationToken)
        {
            await using DecisionStoreContext db = await this.factory.CreateDbContextAsync(cancellationToken);
            return await db.Decisions.AsNoTracking().SingleOrDefaultAsync(record => record.TargetKey == targetKey && record.GroupId == groupId, cancellationToken);
        }

        /// <summary>
        /// Lists every decision for a target.
        /// </summary>
        /// <param name="targetKey">Target key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Decisions by group ID.</returns>
        public async Task<IReadOnlyDictionary<int, GroupDecisionRecord>> ListAsync(string targetKey, CancellationToken cancellationToken)
        {
            await using DecisionStoreContext db = await this.factory.CreateDbContextAsync(cancellationToken);
            return await db.Decisions.AsNoTracking().Where(record => record.TargetKey == targetKey).ToDictionaryAsync(record => record.GroupId, cancellationToken);
        }

        /// <summary>
        /// Lists history, newest first.
        /// </summary>
        /// <param name="targetKey">Target key.</param>
        /// <param name="groupId">Optional group filter.</param>
        /// <param name="take">Maximum rows.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The events.</returns>
        public async Task<IReadOnlyList<DecisionEventRecord>> ListEventsAsync(string targetKey, int? groupId, int take, CancellationToken cancellationToken)
        {
            await using DecisionStoreContext db = await this.factory.CreateDbContextAsync(cancellationToken);
            List<DecisionEventRecord> events = await db.Events.AsNoTracking()
                .Where(record => record.TargetKey == targetKey && (groupId == null || record.GroupId == groupId))
                .ToListAsync(cancellationToken);
            return events.OrderByDescending(record => record.AtUtc).ThenByDescending(record => record.Id).Take(take).ToList();
        }

        /// <summary>
        /// Saves or defers a decision. A change to anything the dry run covered resets the status.
        /// </summary>
        /// <param name="targetKey">Target key.</param>
        /// <param name="input">Decision.</param>
        /// <param name="defer">Mark the group as deferred.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The saved record.</returns>
        public async Task<GroupDecisionRecord> SaveAsync(string targetKey, DecisionInput input, bool defer, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(input);
            await using DecisionStoreContext db = await this.factory.CreateDbContextAsync(cancellationToken);
            GroupDecisionRecord record = await GetOrCreateAsync(db, targetKey, input, cancellationToken);
            if (record.Status == DecisionStatus.Applied)
            {
                throw new InvalidOperationException($"Group {input.GroupId} was already applied; its decision is final.");
            }

            Copy(input, record);
            if (defer)
            {
                record.Status = DecisionStatus.Deferred;
            }
            else if (record.Status != DecisionStatus.DryRunPassed || record.DryRunFingerprint != input.Fingerprint(targetKey))
            {
                record.Status = DecisionStatus.Draft;
            }

            db.Events.Add(NewEvent(targetKey, input, defer ? DecisionEventKind.Deferred : DecisionEventKind.Saved));
            await db.SaveChangesAsync(cancellationToken);
            this.DecisionChanged?.Invoke(this, input.GroupId);
            return record;
        }

        /// <summary>
        /// Records a dry run or apply.
        /// </summary>
        /// <param name="targetKey">Target key.</param>
        /// <param name="input">Decision that ran.</param>
        /// <param name="result">Result.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated record.</returns>
        public async Task<GroupDecisionRecord> RecordRunAsync(string targetKey, DecisionInput input, ExecutionResult result, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(result);
            await using DecisionStoreContext db = await this.factory.CreateDbContextAsync(cancellationToken);
            GroupDecisionRecord record = await GetOrCreateAsync(db, targetKey, input, cancellationToken);
            Copy(input, record);
            bool dryRun = result.Request.DryRun;
            if (dryRun)
            {
                record.Status = result.Succeeded ? DecisionStatus.DryRunPassed : DecisionStatus.DryRunFailed;
                record.DryRunFingerprint = result.Succeeded ? input.Fingerprint(targetKey) : null;
                record.DryRunAtUtc = result.FinishedAtUtc;
            }
            else if (result.Committed)
            {
                record.Status = DecisionStatus.Applied;
                record.AppliedAtUtc = result.FinishedAtUtc;
                record.PlanEvidenceHash = result.PlanEvidenceHash;
            }
            else
            {
                record.Status = DecisionStatus.ApplyFailed;
                record.DryRunFingerprint = null;
            }

            record.AffectedPaymentIds = string.Join(", ", result.AffectedPaymentIds);
            record.LastIssues = result.Issues.Count == 0 ? null : string.Join(Environment.NewLine, result.Issues.Select(issue => $"[{issue.Code}] {issue.Message}"));

            DecisionEventRecord history = NewEvent(targetKey, input, dryRun ? DecisionEventKind.DryRun : DecisionEventKind.Apply);
            history.AtUtc = result.FinishedAtUtc;
            history.Succeeded = result.Succeeded;
            history.PlanEvidenceHash = result.PlanEvidenceHash;
            history.Issues = record.LastIssues;
            history.Log = string.Join(Environment.NewLine, result.Log);
            db.Events.Add(history);
            await db.SaveChangesAsync(cancellationToken);
            this.DecisionChanged?.Invoke(this, input.GroupId);
            return record;
        }

        private static async Task<GroupDecisionRecord> GetOrCreateAsync(DecisionStoreContext db, string targetKey, DecisionInput input, CancellationToken cancellationToken)
        {
            GroupDecisionRecord? record = await db.Decisions.SingleOrDefaultAsync(candidate => candidate.TargetKey == targetKey && candidate.GroupId == input.GroupId, cancellationToken);
            if (record == null)
            {
                record = new GroupDecisionRecord { TargetKey = targetKey, GroupId = input.GroupId, CreatedAtUtc = DateTime.UtcNow, Status = DecisionStatus.Draft };
                db.Decisions.Add(record);
            }

            return record;
        }

        private static void Copy(DecisionInput input, GroupDecisionRecord record)
        {
            record.EasyPayPaymentId = input.EasyPayPaymentId;
            record.EvidenceHash = input.EvidenceHash;
            record.Action = input.Action;
            record.CanonicalPaymentId = input.CanonicalPaymentId;
            record.Reason = input.Reason.Trim();
            record.ApprovedBy = input.ApprovedBy.Trim();
            record.Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();
            record.UpdatedAtUtc = DateTime.UtcNow;
        }

        private static DecisionEventRecord NewEvent(string targetKey, DecisionInput input, DecisionEventKind kind)
        {
            return new DecisionEventRecord
            {
                TargetKey = targetKey,
                GroupId = input.GroupId,
                AtUtc = DateTime.UtcNow,
                Kind = kind,
                ApprovedBy = input.ApprovedBy.Trim(),
                Action = input.Action,
                CanonicalPaymentId = input.CanonicalPaymentId,
                Reason = input.Reason.Trim(),
                EvidenceHash = input.EvidenceHash,
            };
        }
    }
}
