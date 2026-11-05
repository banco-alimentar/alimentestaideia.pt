// -----------------------------------------------------------------------
// <copyright file="GroupActionExecutor.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Execution
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data;
    using Microsoft.Data.SqlClient;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Storage;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Runs one group's decision in its own serializable transaction, with the same locks, validations,
    /// audit rows, and final check as <c>MergeDuplicateEasyPayPaymentIds.sql</c>.
    /// </summary>
    public sealed class GroupActionExecutor
    {
        /// <summary>
        /// Version of the per-group plan JSON written to the plans table.
        /// </summary>
        public const int PlanVersion = 1;

        private const string SessionSetupSql = "SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000;";

        private const string SessionResetSql = "SET XACT_ABORT OFF; SET LOCK_TIMEOUT -1;";

        private const string LockSql = @"
DECLARE @LockedRows bigint;
SELECT @LockedRows = COUNT_BIG(*) FROM dbo.Payments WITH (TABLOCKX, HOLDLOCK);
SELECT @LockedRows = COUNT_BIG(*) FROM dbo.Donations WITH (TABLOCK, HOLDLOCK);
SELECT @LockedRows = COUNT_BIG(*) FROM dbo.Invoices WITH (TABLOCK, HOLDLOCK);";

        private const string ArchiveSql = @"
SELECT payment.Id,
       (SELECT archived.* FROM dbo.Payments archived WHERE archived.Id = payment.Id
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES)
FROM dbo.Payments payment
WHERE payment.Id IN (SELECT CONVERT(int, value) FROM OPENJSON(@Ids));";

        private static readonly JsonSerializerOptions PlanJsonOptions = new JsonSerializerOptions { WriteIndented = false };

        private readonly IDbContextFactory<ApplicationDbContext> factory;
        private readonly ILogger<GroupActionExecutor> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="GroupActionExecutor"/> class.
        /// </summary>
        /// <param name="factory">Context factory.</param>
        /// <param name="logger">Logger.</param>
        public GroupActionExecutor(IDbContextFactory<ApplicationDbContext> factory, ILogger<GroupActionExecutor> logger)
        {
            this.factory = factory;
            this.logger = logger;
        }

        /// <summary>
        /// Computes the per-group plan hash. Each applied group gets its own row in the plans table.
        /// </summary>
        /// <param name="target">Target identity.</param>
        /// <param name="groupId">Group ID.</param>
        /// <param name="evidenceHash">Evidence hash.</param>
        /// <returns>Uppercase SHA-256 hex.</returns>
        public static string ComputePlanHash(TargetIdentity target, int groupId, string evidenceHash)
        {
            ArgumentNullException.ThrowIfNull(target);
            string input = string.Join('|', "CleanProdHarden", target.ServerName, target.DatabaseName, groupId.ToString(System.Globalization.CultureInfo.InvariantCulture), evidenceHash);
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)));
        }

        /// <summary>
        /// Runs a decision. Dry runs always roll back; applies commit only when every check passes.
        /// </summary>
        /// <param name="request">Request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result. Failures are reported in the result, not thrown.</returns>
        public async Task<ExecutionResult> ExecuteAsync(ExecutionRequest request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var log = new List<string>();
            var stopwatch = Stopwatch.StartNew();
            void Log(string message)
            {
                log.Add(string.Create(CultureInfo.InvariantCulture, $"{stopwatch.Elapsed.TotalSeconds,6:0.000}s  {message}"));
            }

            TargetIdentity? target = null;
            string? actualHash = null;
            string? planHash = null;
            string? planJson = null;
            IReadOnlyList<int> affected = Array.Empty<int>();

            await using ApplicationDbContext db = await this.factory.CreateDbContextAsync(cancellationToken);
            await db.Database.OpenConnectionAsync(cancellationToken);
            try
            {
                await SqlRunner.ExecuteAsync(db.Database, SessionSetupSql, cancellationToken);
                await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
                try
                {
                    Log(request.DryRun ? "Started dry run (serializable transaction, will roll back)." : "Started apply (serializable transaction).");
                    target = await TargetDatabaseService.GetIdentityAsync(db, cancellationToken);
                    Log($"Target {target.ServerName} / {target.DatabaseName} as {target.Login}.");
                    if (!request.DryRun && !string.Equals(request.ConfirmDatabaseName?.Trim(), target.DatabaseName, StringComparison.Ordinal))
                    {
                        throw new ExecutionAbortedException(ValidationCodes.TargetNotConfirmed, $"Type the database name '{target.DatabaseName}' exactly to apply.");
                    }

                    await SqlRunner.ExecuteAsync(db.Database, LockSql, cancellationToken);
                    Log("Locked Payments (TABLOCKX, HOLDLOCK), Donations and Invoices (TABLOCK, HOLDLOCK).");

                    GroupAnalyzer.AnalysisResult analysis = await GroupAnalyzer.AnalyzeAsync(db, request.GroupId, lockDependencies: true, cancellationToken);
                    AnalyzedGroup? group = analysis.Groups.SingleOrDefault(candidate => candidate.GroupId == request.GroupId)
                        ?? throw new ExecutionAbortedException(ValidationCodes.GroupNotFound, $"Group {request.GroupId} no longer has duplicate provider IDs.");
                    ExecutionEnvironment environment = analysis.Environments[request.GroupId];
                    actualHash = group.EvidenceHash;
                    Log($"Re-evaluated group {group.GroupId}: {group.Evaluation.PaymentCount} payments, rule {group.Evaluation.DecisionRule}, evidence {actualHash}.");

                    var decision = new ProposedDecision(request.GroupId, request.Action, request.CanonicalPaymentId, request.Reason, request.ApprovedBy, request.ExpectedEvidenceHash);
                    IReadOnlyList<ValidationIssue> issues = ActionValidator.Validate(group.Evaluation, decision, environment);
                    if (issues.Count > 0)
                    {
                        throw new ExecutionAbortedException(issues);
                    }

                    Log("All validations passed.");
                    ReconciliationAction action = request.Action!.Value;
                    affected = ActionValidator.AffectedPaymentIds(group.Evaluation, action, request.CanonicalPaymentId);

                    await SqlRunner.ExecuteAsync(db.Database, AuditDbContext.CreateTablesSql, cancellationToken);
                    Log("Audit tables exist (created inside the transaction when missing).");

                    planHash = ComputePlanHash(target, group.GroupId, group.EvidenceHash);
                    DateTime appliedAtUtc = (DateTime)(await SqlRunner.ScalarAsync(db.Database, "SELECT SYSUTCDATETIME();", cancellationToken))!;
                    planJson = BuildPlanJson(target, group, request, action, affected, appliedAtUtc);

                    DbTransaction dbTransaction = transaction.GetDbTransaction();
                    DbContextOptions<AuditDbContext> auditOptions = new DbContextOptionsBuilder<AuditDbContext>()
                        .UseSqlServer(db.Database.GetDbConnection())
                        .Options;
                    await using (var audit = new AuditDbContext(auditOptions))
                    {
                        await audit.Database.UseTransactionAsync(dbTransaction, cancellationToken);
                        if (await audit.Plans.AnyAsync(plan => plan.PlanEvidenceHash == planHash, cancellationToken))
                        {
                            throw new ExecutionAbortedException(ValidationCodes.AuditHistoryExists, $"Plan {planHash} was already applied.");
                        }

                        audit.Plans.Add(new ReconciliationPlanRow
                        {
                            PlanEvidenceHash = planHash,
                            ApprovedBy = request.ApprovedBy.Trim(),
                            ApprovalReason = request.Reason.Trim(),
                            ApprovedPlan = planJson,
                            AppliedAtUtc = appliedAtUtc,
                            ExecutedBy = target.Login,
                        });

                        await AddAuditRowsAsync(db, audit, group, action, request.CanonicalPaymentId, affected, planHash, appliedAtUtc, cancellationToken);
                        int written = await audit.SaveChangesAsync(cancellationToken);
                        Log($"Wrote plan {planHash} and {written - 1} audit rows.");
                    }

                    List<int> affectedIds = affected.ToList();
                    int changed = action == ReconciliationAction.DeleteGroup
                        ? await db.Payments.Where(payment => affectedIds.Contains(payment.Id)).ExecuteDeleteAsync(cancellationToken)
                        : await db.Payments.OfType<EasyPayBaseClass>()
                            .Where(payment => affectedIds.Contains(payment.Id))
                            .ExecuteUpdateAsync(setters => setters.SetProperty(payment => payment.EasyPayPaymentId, (string?)null), cancellationToken);
                    if (changed != affected.Count)
                    {
                        throw new ExecutionAbortedException(ValidationCodes.RowCountMismatch, $"Expected to change {affected.Count} payments but changed {changed}.");
                    }

                    Log(action == ReconciliationAction.DeleteGroup
                        ? $"Deleted payments {string.Join(", ", affected)}."
                        : $"Cleared EasyPayPaymentId on payments {string.Join(", ", affected)}.");

                    await VerifyAsync(db, group, action, request.CanonicalPaymentId, affected, cancellationToken);
                    Log("Post-checks passed: the provider ID is now unique and every change is audited.");

                    if (request.DryRun)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        Log("Dry run complete. Rolled back every change, including audit tables created by this run.");
                    }
                    else
                    {
                        await transaction.CommitAsync(cancellationToken);
                        Log("Committed.");
                        this.logger.LogInformation(
                            "Applied {Action} to group {GroupId} on {Server}/{Database}; plan {PlanHash}; affected {Affected}.",
                            action,
                            request.GroupId,
                            target.ServerName,
                            target.DatabaseName,
                            planHash,
                            affected);
                    }

                    return new ExecutionResult
                    {
                        Request = request,
                        Succeeded = true,
                        Committed = !request.DryRun,
                        Target = target,
                        Log = log,
                        AffectedPaymentIds = affected,
                        PlanEvidenceHash = planHash,
                        ApprovedPlanJson = planJson,
                        ActualEvidenceHash = actualHash,
                        FinishedAtUtc = DateTime.UtcNow,
                    };
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    await RollbackQuietlyAsync(transaction);
                    IReadOnlyList<ValidationIssue> issues = ToIssues(exception);
                    Log("Rolled back: " + string.Join(" ", issues.Select(issue => $"[{issue.Code}] {issue.Message}")));
                    if (exception is not ExecutionAbortedException)
                    {
                        this.logger.LogError(exception, "Execution of group {GroupId} failed.", request.GroupId);
                    }

                    return new ExecutionResult
                    {
                        Request = request,
                        Succeeded = false,
                        Committed = false,
                        Target = target,
                        Issues = issues,
                        Log = log,
                        AffectedPaymentIds = affected,
                        PlanEvidenceHash = planHash,
                        ApprovedPlanJson = planJson,
                        ActualEvidenceHash = actualHash,
                        FinishedAtUtc = DateTime.UtcNow,
                    };
                }
                catch (OperationCanceledException)
                {
                    await RollbackQuietlyAsync(transaction);
                    throw;
                }
            }
            finally
            {
                try
                {
                    await SqlRunner.ExecuteAsync(db.Database, SessionResetSql, CancellationToken.None);
                }
                catch (DbException exception)
                {
                    this.logger.LogWarning(exception, "Could not reset session options.");
                }

                await db.Database.CloseConnectionAsync();
            }
        }

        private static IReadOnlyList<ValidationIssue> ToIssues(Exception exception)
        {
            return exception switch
            {
                ExecutionAbortedException aborted => aborted.Issues,
                SqlException sql => new[] { new ValidationIssue(sql.Number is >= 50000 and < 60000 ? sql.Number : ValidationCodes.UnexpectedError, $"SQL error {sql.Number}: {sql.Message}") },
                _ => new[] { new ValidationIssue(ValidationCodes.UnexpectedError, exception.GetType().Name + ": " + exception.Message) },
            };
        }

        private static async Task RollbackQuietlyAsync(IDbContextTransaction transaction)
        {
            try
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
            catch (InvalidOperationException)
            {
                // XACT_ABORT already rolled the transaction back.
            }
            catch (DbException)
            {
                // XACT_ABORT already rolled the transaction back.
            }
        }

        private static string BuildPlanJson(TargetIdentity target, AnalyzedGroup group, ExecutionRequest request, ReconciliationAction action, IReadOnlyList<int> affected, DateTime generatedAtUtc)
        {
            var plan = new
            {
                Tool = "CleanProdHarden",
                PlanVersion,
                target.ServerName,
                target.DatabaseName,
                GeneratedAtUtc = generatedAtUtc,
                group.GroupId,
                group.Evaluation.EasyPayPaymentId,
                ExpectedEvidenceHash = group.EvidenceHash,
                DecisionRule = group.Evaluation.DecisionRule.ToString(),
                Pattern = group.Recommendation.Pattern.ToString(),
                Recommended = new
                {
                    Action = group.Recommendation.Action?.ToString(),
                    group.Recommendation.CanonicalPaymentId,
                    Confidence = group.Recommendation.Confidence.ToString(),
                },
                Action = action.ToString(),
                request.CanonicalPaymentId,
                ApprovedBy = request.ApprovedBy.Trim(),
                ApprovalReason = request.Reason.Trim(),
                AffectedPaymentIds = affected,
                Members = group.Evaluation.Members.Select(member => new
                {
                    member.PaymentId,
                    member.Input.PaymentType,
                    member.Input.Status,
                    member.Input.DonationId,
                    member.ConfirmationCount,
                    member.Input.Paid,
                    member.Input.Completed,
                    DeletionBasis = member.DeletionBasis?.ToString(),
                }),
            };
            return JsonSerializer.Serialize(plan, PlanJsonOptions);
        }

        private static async Task AddAuditRowsAsync(
            ApplicationDbContext db,
            AuditDbContext audit,
            AnalyzedGroup group,
            ReconciliationAction action,
            int? canonicalPaymentId,
            IReadOnlyList<int> affected,
            string planHash,
            DateTime appliedAtUtc,
            CancellationToken cancellationToken)
        {
            Dictionary<int, MemberEvaluation> members = group.Evaluation.Members.ToDictionary(member => member.PaymentId);
            switch (action)
            {
                case ReconciliationAction.ReconcileIdentifiers:
                    foreach (int id in affected)
                    {
                        audit.ReconciliationAudit.Add(new ReconciliationAuditRow
                        {
                            PaymentId = id,
                            CanonicalPaymentId = canonicalPaymentId!.Value,
                            OriginalEasyPayPaymentId = members[id].Input.EasyPayPaymentId,
                            ReconciledAtUtc = appliedAtUtc,
                        });
                    }

                    break;

                case ReconciliationAction.RetireIdentifier:
                    foreach (int id in affected)
                    {
                        audit.RetirementAudit.Add(new RetirementAuditRow
                        {
                            PaymentId = id,
                            GroupId = group.GroupId,
                            OriginalEasyPayPaymentId = members[id].Input.EasyPayPaymentId,
                            PlanEvidenceHash = planHash,
                            RetiredAtUtc = appliedAtUtc,
                        });
                    }

                    break;

                case ReconciliationAction.DeleteGroup:
                    List<(int Id, string Json)> archived = await SqlRunner.QueryAsync(
                        db.Database,
                        ArchiveSql,
                        reader => (reader.GetInt32(0), reader.GetString(1)),
                        cancellationToken,
                        SqlRunner.Ids("@Ids", affected));
                    if (archived.Count != affected.Count)
                    {
                        throw new ExecutionAbortedException(ValidationCodes.RowCountMismatch, $"Expected to archive {affected.Count} payments but found {archived.Count}.");
                    }

                    foreach ((int id, string json) in archived)
                    {
                        audit.DeletionAudit.Add(new DeletionAuditRow
                        {
                            PaymentId = id,
                            GroupId = group.GroupId,
                            ReplacementPaymentId = members[id].DonationConfirmedPaymentId,
                            OriginalEasyPayPaymentId = members[id].Input.EasyPayPaymentId,
                            DeletedPaymentJson = json,
                            PlanEvidenceHash = planHash,
                            DeletedAtUtc = appliedAtUtc,
                        });
                    }

                    break;

                default:
                    throw new ExecutionAbortedException(ValidationCodes.UnsupportedAction, $"Unsupported action {action}.");
            }
        }

        private static async Task VerifyAsync(ApplicationDbContext db, AnalyzedGroup group, ReconciliationAction action, int? canonicalPaymentId, IReadOnlyList<int> affected, CancellationToken cancellationToken)
        {
            long remaining = (long)(await SqlRunner.ScalarAsync(
                db.Database,
                "SELECT COUNT_BIG(*) FROM dbo.Payments WHERE EasyPayPaymentId = @Original;",
                cancellationToken,
                SqlRunner.Text("@Original", group.Evaluation.EasyPayPaymentId)))!;
            long expected = action == ReconciliationAction.ReconcileIdentifiers ? 1 : 0;
            if (remaining != expected)
            {
                throw new ExecutionAbortedException(ValidationCodes.DuplicatesRemain, $"Expected {expected} payments to keep provider ID {group.Evaluation.EasyPayPaymentId}, found {remaining}.");
            }

            if (action == ReconciliationAction.ReconcileIdentifiers)
            {
                bool ownerKept = await db.Payments.OfType<EasyPayBaseClass>()
                    .AnyAsync(payment => payment.Id == canonicalPaymentId && payment.EasyPayPaymentId != null, cancellationToken);
                if (!ownerKept)
                {
                    throw new ExecutionAbortedException(ValidationCodes.DuplicatesRemain, $"Owner payment {canonicalPaymentId} lost its provider ID.");
                }
            }

            string auditTable = action switch
            {
                ReconciliationAction.ReconcileIdentifiers => "EasyPayPaymentIdReconciliationAudit",
                ReconciliationAction.RetireIdentifier => "EasyPayPaymentIdRetirementAudit",
                _ => "EasyPayPaymentDeletionAudit",
            };
            long audited = (long)(await SqlRunner.ScalarAsync(
                db.Database,
                $"SELECT COUNT_BIG(*) FROM dbo.{SqlRunner.QuoteName(auditTable)} WHERE PaymentId IN (SELECT CONVERT(int, value) FROM OPENJSON(@Ids));",
                cancellationToken,
                SqlRunner.Ids("@Ids", affected)))!;
            if (audited != affected.Count)
            {
                throw new ExecutionAbortedException(ValidationCodes.RowCountMismatch, $"Expected {affected.Count} audit rows in {auditTable}, found {audited}.");
            }
        }
    }
}
