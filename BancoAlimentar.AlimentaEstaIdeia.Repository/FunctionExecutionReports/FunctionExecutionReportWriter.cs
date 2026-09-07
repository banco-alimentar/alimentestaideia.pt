// -----------------------------------------------------------------------
// <copyright file="FunctionExecutionReportWriter.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>Builds bounded execution report activity and summary data.</summary>
    public sealed class FunctionExecutionReportWriter
    {
        private readonly FunctionExecutionReportOptions options;
        private readonly List<FunctionExecutionReportActivity> activities = new List<FunctionExecutionReportActivity>();
        private readonly List<string> warnings = new List<string>();
        private readonly List<string> errors = new List<string>();
        private readonly Dictionary<string, long> counters = new Dictionary<string, long>(StringComparer.Ordinal);
        private bool truncationWarningAdded;
        private bool counterLimitReached;

        /// <summary>Initializes a new writer.</summary>
        /// <param name="options">Report limits.</param>
        public FunctionExecutionReportWriter(FunctionExecutionReportOptions options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            FunctionExecutionReportOptions.Validate(this.options);
        }

        /// <summary>Records one safe activity if the activity limit has not been reached.</summary>
        public void RecordActivity(
            string activityKey,
            FunctionExecutionReportActivitySeverity severity,
            string message,
            string scopeKey = null,
            IReadOnlyDictionary<string, long> activityCounters = null,
            DateTime? timestampUtc = null)
        {
            string safeMessage = ValidateMessage(message);
            if (this.activities.Count >= this.options.MaxActivities)
            {
                this.AddTruncationWarning();
                return;
            }

            var activity = new FunctionExecutionReportActivity
            {
                Ordinal = this.activities.Count + 1,
                TimestampUtc = (timestampUtc ?? DateTime.UtcNow).ToUniversalTime(),
                ActivityKey = FunctionExecutionReportPaths.NormalizeSegment(activityKey, nameof(activityKey)),
                Severity = severity,
                ScopeKey = scopeKey,
                Message = safeMessage,
            };

            if (activityCounters != null)
            {
                foreach (KeyValuePair<string, long> counter in activityCounters)
                {
                    if (activity.Counters.Count >= this.options.MaxCounters)
                    {
                        this.AddCounterLimitWarning();
                        break;
                    }

                    activity.Counters[FunctionExecutionReportPaths.NormalizeSegment(counter.Key, nameof(counter.Key))] = counter.Value;
                }
            }

            this.activities.Add(activity);
            if (severity == FunctionExecutionReportActivitySeverity.Warning)
            {
                this.AddWarning(safeMessage);
            }
            else if (severity == FunctionExecutionReportActivitySeverity.Error)
            {
                this.AddError(safeMessage);
            }
        }

        /// <summary>Records a warning outside the activity list.</summary>
        public void RecordWarning(string message) => this.AddWarning(ValidateMessage(message));

        /// <summary>Records an error outside the activity list.</summary>
        public void RecordError(string message) => this.AddError(ValidateMessage(message));

        /// <summary>Sets a summary counter.</summary>
        public void SetCounter(string key, long value)
        {
            string normalizedKey = FunctionExecutionReportPaths.NormalizeSegment(key, nameof(key));
            if (!this.counters.ContainsKey(normalizedKey) && this.counters.Count >= this.options.MaxCounters)
            {
                this.AddCounterLimitWarning();
                return;
            }

            this.counters[normalizedKey] = value;
        }

        /// <summary>Increments a summary counter.</summary>
        public void IncrementCounter(string key, long increment = 1)
        {
            string normalizedKey = FunctionExecutionReportPaths.NormalizeSegment(key, nameof(key));
            if (!this.counters.ContainsKey(normalizedKey) && this.counters.Count >= this.options.MaxCounters)
            {
                this.AddCounterLimitWarning();
                return;
            }

            this.counters[normalizedKey] = this.counters.TryGetValue(normalizedKey, out long value) ? value + increment : increment;
        }

        /// <summary>Creates the final report document.</summary>
        public FunctionExecutionReport Complete(
            string functionKey,
            string executionId,
            string invocationId,
            FunctionExecutionReportOutcome outcome,
            DateTime startedAtUtc,
            DateTime completedAtUtc,
            bool businessDataChanged,
            string functionName = null,
            string environment = null,
            string slotKey = null,
            string scopeKey = null,
            string summaryMessage = null,
            string triggerType = null,
            string correlationId = null)
        {
            DateTime started = startedAtUtc.ToUniversalTime();
            DateTime completed = completedAtUtc.ToUniversalTime();
            var summary = new FunctionExecutionReportSummary
            {
                ActivitiesAttempted = this.activities.Count,
                ActivitiesCompleted = this.activities.Count,
                Warnings = this.warnings.Count,
                Errors = this.errors.Count,
                RecordsChanged = this.GetCounter("recordsChanged"),
                TenantsDiscovered = this.GetCounterAsInt("tenantsDiscovered"),
                TenantsProcessed = this.GetCounterAsInt("tenantsProcessed"),
                TenantsSucceeded = this.GetCounterAsInt("tenantsSucceeded"),
                TenantsFailed = this.GetCounterAsInt("tenantsFailed"),
                TenantsSkipped = this.GetCounterAsInt("tenantsSkipped"),
                Counters = new Dictionary<string, long>(this.counters, StringComparer.Ordinal),
                Message = summaryMessage == null ? null : ValidateMessage(summaryMessage),
            };

            return new FunctionExecutionReport
            {
                SchemaVersion = 1,
                FunctionKey = FunctionExecutionReportPaths.NormalizeSegment(functionKey, nameof(functionKey)),
                FunctionName = functionName ?? functionKey,
                ExecutionId = FunctionExecutionReportPaths.NormalizeSegment(executionId, nameof(executionId)),
                InvocationId = invocationId,
                CorrelationId = correlationId == null ? null : ValidateMetadata(correlationId, nameof(correlationId)),
                TriggerType = string.IsNullOrWhiteSpace(triggerType) ? "Unknown" : ValidateMetadata(triggerType, nameof(triggerType)),
                Environment = environment == null ? null : ValidateMetadata(environment, nameof(environment)),
                SlotKey = slotKey,
                ScopeKey = scopeKey,
                StartedAtUtc = started,
                CompletedAtUtc = completed,
                DurationMilliseconds = Math.Max(0, (long)(completed - started).TotalMilliseconds),
                Outcome = outcome,
                BusinessDataChanged = businessDataChanged,
                Activities = new List<FunctionExecutionReportActivity>(this.activities),
                Summary = summary,
                Warnings = new List<string>(this.warnings),
                Errors = new List<string>(this.errors),
            };
        }

        private string ValidateMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("A report message is required.", nameof(message));
            }

            if (ContainsSensitiveText(message))
            {
                throw new ArgumentException("Report messages must not contain credentials or authorization data.", nameof(message));
            }

            if (message.Length <= this.options.MaxActivityMessageLength)
            {
                return message;
            }

            this.AddTruncationWarning();
            return message.Substring(0, this.options.MaxActivityMessageLength);
        }

        private static bool ContainsSensitiveText(string message)
        {
            const RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
            string sensitiveAssignmentPattern = @"\b(?:authorization|account[\s._-]*key|api[\s._-]*key|access[\s._-]*token|client[\s._-]*secret|connection[\s._-]*string|password|secret|refresh[\s._-]*token|api[\s._-]*certificate[\s._-]*v3|x[\s._-]*api[\s._-]*key)\s*[:=]\s*\S+";
            string sensitivePhrasePattern = @"\b(?:client[\s._-]*secret|connection[\s._-]*string)\b";
            string bearerTokenPattern = @"\bbearer\s*[:=]?\s+[A-Za-z0-9._~+/=-]+\b";

            return Regex.IsMatch(message, sensitiveAssignmentPattern, options)
                || Regex.IsMatch(message, sensitivePhrasePattern, options)
                || Regex.IsMatch(message, bearerTokenPattern, options);
        }

        private void AddTruncationWarning()
        {
            if (!this.truncationWarningAdded)
            {
                this.truncationWarningAdded = true;
                this.AddWarningInternal("The execution report activity limit was reached; additional activities were truncated and omitted.");
            }
        }

        private void AddWarning(string message)
        {
            if (this.warnings.Contains(message, StringComparer.Ordinal))
            {
                return;
            }

            if (this.warnings.Count >= this.options.MaxWarnings)
            {
                return;
            }

            this.AddWarningInternal(message);
        }

        private void AddError(string message)
        {
            if (this.errors.Contains(message, StringComparer.Ordinal))
            {
                return;
            }

            if (this.errors.Count >= this.options.MaxErrors)
            {
                return;
            }

            this.errors.Add(message);
        }

        private void AddWarningInternal(string message)
        {
            if (!this.warnings.Contains(message, StringComparer.Ordinal) && this.warnings.Count < this.options.MaxWarnings)
            {
                this.warnings.Add(message);
            }
        }

        private void AddCounterLimitWarning()
        {
            if (!this.counterLimitReached)
            {
                this.counterLimitReached = true;
                this.AddWarning("The execution report counter limit was reached; additional counters were omitted.");
            }
        }

        private string ValidateMetadata(string value, string parameterName)
        {
            if (value.Length > 200 || ContainsSensitiveText(value))
            {
                throw new ArgumentException("Report metadata is invalid or sensitive.", parameterName);
            }

            return value;
        }

        private int GetCounterAsInt(string key)
        {
            return (int)Math.Clamp(this.GetCounter(key), int.MinValue, int.MaxValue);
        }

        private long GetCounter(string key)
        {
            string normalizedKey = FunctionExecutionReportPaths.NormalizeSegment(key, nameof(key));
            return this.counters.TryGetValue(normalizedKey, out long value) ? value : 0;
        }
    }
}
