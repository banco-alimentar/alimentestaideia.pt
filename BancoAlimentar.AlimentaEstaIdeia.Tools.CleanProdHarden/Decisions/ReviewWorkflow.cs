// -----------------------------------------------------------------------
// <copyright file="ReviewWorkflow.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Execution;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Hosting;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Enforces the review order: save, dry run, then apply exactly what passed.
    /// </summary>
    public sealed class ReviewWorkflow
    {
        private readonly GroupActionExecutor executor;
        private readonly DecisionStore store;
        private readonly DuplicateGroupService groups;
        private readonly CleanProdHardenOptions options;
        private readonly SemaphoreSlim applyGate = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Initializes a new instance of the <see cref="ReviewWorkflow"/> class.
        /// </summary>
        /// <param name="executor">Executor.</param>
        /// <param name="store">Decision store.</param>
        /// <param name="groups">Group service.</param>
        /// <param name="options">Options.</param>
        public ReviewWorkflow(GroupActionExecutor executor, DecisionStore store, DuplicateGroupService groups, IOptions<CleanProdHardenOptions> options)
        {
            ArgumentNullException.ThrowIfNull(options);
            this.executor = executor;
            this.store = store;
            this.groups = groups;
            this.options = options.Value;
        }

        /// <summary>
        /// Gets a value indicating whether applying is enabled.
        /// </summary>
        public bool AllowApply => this.options.AllowApply;

        /// <summary>
        /// Runs the decision and rolls it back, then records the outcome.
        /// </summary>
        /// <param name="target">Target identity.</param>
        /// <param name="input">Decision.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result.</returns>
        public async Task<ExecutionResult> DryRunAsync(TargetIdentity target, DecisionInput input, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(input);
            ExecutionResult result = await this.RunExclusiveAsync(ToRequest(input, dryRun: true, confirmDatabaseName: null), cancellationToken);
            await this.store.RecordRunAsync(target.Key, input, result, cancellationToken);
            return result;
        }

        /// <summary>
        /// Commits the decision when the same decision passed a dry run on the same evidence.
        /// </summary>
        /// <param name="target">Target identity.</param>
        /// <param name="input">Decision.</param>
        /// <param name="confirmDatabaseName">Database name typed by the reviewer.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result.</returns>
        public async Task<ExecutionResult> ApplyAsync(TargetIdentity target, DecisionInput input, string? confirmDatabaseName, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(input);
            ExecutionRequest request = ToRequest(input, dryRun: false, confirmDatabaseName);
            if (!this.options.AllowApply)
            {
                return Rejected(request, ValidationCodes.ApplyDisabled, "Applying is disabled. Set CleanProdHarden:AllowApply to true to commit changes.");
            }

            GroupDecisionRecord? record = await this.store.GetAsync(target.Key, input.GroupId, cancellationToken);
            if (record?.Status != DecisionStatus.DryRunPassed || record.DryRunFingerprint != input.Fingerprint(target.Key))
            {
                return Rejected(request, ValidationCodes.DryRunRequired, "Run a dry run of this exact decision first. Changing the action, owner, reason, reviewer, or evidence requires a new dry run.");
            }

            ExecutionResult result = await this.RunExclusiveAsync(request, cancellationToken);
            await this.store.RecordRunAsync(target.Key, input, result, cancellationToken);
            if (result.Committed)
            {
                this.groups.Invalidate();
            }

            return result;
        }

        /// <summary>
        /// Dry-runs the selected decisions sequentially, stopping at the first failure.
        /// </summary>
        /// <param name="target">Target identity.</param>
        /// <param name="inputs">Selected decisions.</param>
        /// <param name="onCompleted">Reports each completed group.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The outcomes of the attempted groups.</returns>
        public async Task<IReadOnlyList<ExecutionResult>> DryRunBatchAsync(
            TargetIdentity target,
            IReadOnlyList<DecisionInput> inputs,
            Func<ExecutionResult, Task> onCompleted,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(target);
            DecisionInput[] snapshot = BatchSnapshot(inputs);
            foreach (DecisionInput input in snapshot)
            {
                GroupDecisionRecord? record = await this.store.GetAsync(target.Key, input.GroupId, cancellationToken);
                if (record?.Status is DecisionStatus.Applied or DecisionStatus.Deferred)
                {
                    throw new InvalidOperationException($"Group {input.GroupId} is applied or deferred. Review it individually instead of overwriting its decision.");
                }
            }

            return await RunBatchAsync(snapshot, input => this.DryRunAsync(target, input, cancellationToken), onCompleted, cancellationToken);
        }

        /// <summary>
        /// Checks every selected dry run before applying one group at a time. Earlier commits remain if a later group fails.
        /// </summary>
        /// <param name="target">Target identity.</param>
        /// <param name="inputs">Selected decisions.</param>
        /// <param name="confirmDatabaseName">Database name typed by the reviewer.</param>
        /// <param name="onCompleted">Reports each completed group.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The outcomes of the attempted groups, or a preflight rejection.</returns>
        public async Task<IReadOnlyList<ExecutionResult>> ApplyBatchAsync(
            TargetIdentity target,
            IReadOnlyList<DecisionInput> inputs,
            string? confirmDatabaseName,
            Func<ExecutionResult, Task> onCompleted,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(onCompleted);
            DecisionInput[] snapshot = BatchSnapshot(inputs);
            foreach (DecisionInput input in snapshot)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ExecutionRequest request = ToRequest(input, dryRun: false, confirmDatabaseName);
                ExecutionResult? rejection = null;
                if (!this.AllowApply)
                {
                    rejection = Rejected(request, ValidationCodes.ApplyDisabled, "Applying is disabled.");
                }
                else if (!string.Equals(confirmDatabaseName?.Trim(), target.DatabaseName, StringComparison.Ordinal))
                {
                    rejection = Rejected(request, ValidationCodes.TargetNotConfirmed, $"Type the database name '{target.DatabaseName}' exactly to apply the batch.");
                }
                else
                {
                    GroupDecisionRecord? record = await this.store.GetAsync(target.Key, input.GroupId, cancellationToken);
                    if (record?.Status != DecisionStatus.DryRunPassed || record.DryRunFingerprint != input.Fingerprint(target.Key))
                    {
                        rejection = Rejected(request, ValidationCodes.DryRunRequired, $"Group {input.GroupId} needs a passing dry run of this exact decision. No batch changes were started.");
                    }
                }

                if (rejection != null)
                {
                    await onCompleted(rejection);
                    return new[] { rejection };
                }
            }

            return await RunBatchAsync(snapshot, input => this.ApplyAsync(target, input, confirmDatabaseName, cancellationToken), onCompleted, cancellationToken);
        }

        /// <summary>
        /// Executes a fixed selection without starting later groups after failure or cancellation.
        /// </summary>
        /// <param name="inputs">Selected decisions.</param>
        /// <param name="run">Per-group workflow.</param>
        /// <param name="onCompleted">Per-group progress.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The completed outcomes.</returns>
        internal static async Task<IReadOnlyList<ExecutionResult>> RunBatchAsync(
            IReadOnlyList<DecisionInput> inputs,
            Func<DecisionInput, Task<ExecutionResult>> run,
            Func<ExecutionResult, Task> onCompleted,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(run);
            ArgumentNullException.ThrowIfNull(onCompleted);
            DecisionInput[] snapshot = BatchSnapshot(inputs);
            var results = new List<ExecutionResult>();
            foreach (DecisionInput input in snapshot)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ExecutionResult result = await run(input);
                results.Add(result);
                await onCompleted(result);
                if (!result.Succeeded || (!result.Request.DryRun && !result.Committed))
                {
                    break;
                }
            }

            return results;
        }

        private static DecisionInput[] BatchSnapshot(IReadOnlyList<DecisionInput> inputs)
        {
            ArgumentNullException.ThrowIfNull(inputs);
            DecisionInput[] snapshot = inputs.ToArray();
            if (snapshot.Length == 0 || snapshot.Select(input => input.GroupId).Distinct().Count() != snapshot.Length)
            {
                throw new ArgumentException("Select at least one group, without duplicate group IDs.", nameof(inputs));
            }

            return snapshot;
        }

        private static ExecutionRequest ToRequest(DecisionInput input, bool dryRun, string? confirmDatabaseName)
        {
            return new ExecutionRequest(
                input.GroupId,
                input.Action,
                input.CanonicalPaymentId,
                input.Reason,
                input.ApprovedBy,
                input.EvidenceHash,
                dryRun,
                confirmDatabaseName);
        }

        private static ExecutionResult Rejected(ExecutionRequest request, int code, string message)
        {
            return new ExecutionResult
            {
                Request = request,
                Issues = new[] { new ValidationIssue(code, message) },
                Log = new[] { "Not started: " + message },
            };
        }

        private async Task<ExecutionResult> RunExclusiveAsync(ExecutionRequest request, CancellationToken cancellationToken)
        {
            // One run at a time: each run takes an exclusive table lock on Payments.
            await this.applyGate.WaitAsync(cancellationToken);
            try
            {
                return await this.executor.ExecuteAsync(request, cancellationToken);
            }
            finally
            {
                this.applyGate.Release();
            }
        }
    }
}
