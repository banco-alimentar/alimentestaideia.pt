// -----------------------------------------------------------------------
// <copyright file="Display.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Components
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Decisions;

    /// <summary>
    /// Labels, descriptions, and formatting shared by the pages.
    /// </summary>
    public static class Display
    {
        /// <summary>
        /// Placeholder for missing values.
        /// </summary>
        public const string None = "—";

        /// <summary>
        /// Gets every action in display order.
        /// </summary>
        public static IReadOnlyList<ReconciliationAction> Actions { get; } = new[]
        {
            ReconciliationAction.ReconcileIdentifiers,
            ReconciliationAction.RetireIdentifier,
            ReconciliationAction.DeleteGroup,
        };

        /// <summary>
        /// Gets every decision rule in evaluation order.
        /// </summary>
        public static IReadOnlyList<DecisionRule> Rules { get; } = Enum.GetValues<DecisionRule>();

        /// <summary>
        /// Gets every pattern.
        /// </summary>
        public static IReadOnlyList<GroupPattern> Patterns { get; } = Enum.GetValues<GroupPattern>();

        /// <summary>
        /// Formats a date in UTC as stored.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <returns>Text.</returns>
        public static string Date(DateTime? value)
        {
            return value.HasValue ? value.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) : None;
        }

        /// <summary>
        /// Formats an amount.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <returns>Text.</returns>
        public static string Amount(double? value)
        {
            return value.HasValue ? RecommendationEngine.FormatAmount(value) : None;
        }

        /// <summary>
        /// Formats a nullable value.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <returns>Text.</returns>
        public static string Text(object? value)
        {
            return value switch
            {
                null => None,
                string text when text.Length == 0 => "(empty)",
                bool flag => flag ? "yes" : "no",
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString() ?? None,
            };
        }

        /// <summary>
        /// Gets the short label of an action.
        /// </summary>
        /// <param name="action">Action.</param>
        /// <returns>Label.</returns>
        public static string ActionLabel(ReconciliationAction? action)
        {
            return action switch
            {
                ReconciliationAction.ReconcileIdentifiers => "Reconcile",
                ReconciliationAction.RetireIdentifier => "Retire",
                ReconciliationAction.DeleteGroup => "Delete",
                _ => "No action",
            };
        }

        /// <summary>
        /// Describes what an action changes.
        /// </summary>
        /// <param name="action">Action.</param>
        /// <returns>Description.</returns>
        public static string ActionDescription(ReconciliationAction action)
        {
            return action switch
            {
                ReconciliationAction.ReconcileIdentifiers => "Keep the provider ID on the chosen owner and set it to NULL on every other member. Every cleared row is written to EasyPayPaymentIdReconciliationAudit. No rows are deleted.",
                ReconciliationAction.RetireIdentifier => "Set the provider ID to NULL on every member and keep all rows. Every row is written to EasyPayPaymentIdRetirementAudit. Allowed only when no member is confirmed by a donation.",
                ReconciliationAction.DeleteGroup => "Archive every member as JSON in EasyPayPaymentDeletionAudit and delete the rows. Allowed only when every member has safe deletion evidence and nothing references the rows.",
                _ => string.Empty,
            };
        }

        /// <summary>
        /// Describes a decision rule.
        /// </summary>
        /// <param name="rule">Rule.</param>
        /// <returns>Description.</returns>
        public static string RuleDescription(DecisionRule rule)
        {
            return rule switch
            {
                DecisionRule.MultiConfirmed => "More than one member is confirmed by a donation.",
                DecisionRule.ReviewUnconfirmedPaidDonation => "A member is linked to a Payed donation that has no confirmed payment.",
                DecisionRule.ConfirmedPaid => "Exactly one member is confirmed once by a Payed donation, its status is successful, and its paid amount matches.",
                DecisionRule.ReviewConfirmedOwner => "Exactly one member is confirmed, but the paid checks fail.",
                DecisionRule.NoOwnerRetire => "No member is confirmed, completed, paid, or successful by its own evidence.",
                DecisionRule.ReviewUnconfirmedCapture => "Any other combination. A member may have captured money without a confirmation.",
                _ => string.Empty,
            };
        }

        /// <summary>
        /// Gets the label of a decision status.
        /// </summary>
        /// <param name="status">Status, or null when undecided.</param>
        /// <returns>Label.</returns>
        public static string StatusLabel(DecisionStatus? status)
        {
            return status switch
            {
                null => "Undecided",
                DecisionStatus.Draft => "Draft",
                DecisionStatus.Deferred => "Deferred",
                DecisionStatus.DryRunPassed => "Dry run passed",
                DecisionStatus.DryRunFailed => "Dry run failed",
                DecisionStatus.Applied => "Applied",
                DecisionStatus.ApplyFailed => "Apply failed",
                _ => status.Value.ToString(),
            };
        }

        /// <summary>
        /// Gets the badge class of a decision status.
        /// </summary>
        /// <param name="status">Status.</param>
        /// <returns>CSS class.</returns>
        public static string StatusCss(DecisionStatus? status)
        {
            return status switch
            {
                DecisionStatus.DryRunPassed => "badge ok",
                DecisionStatus.Applied => "badge done",
                DecisionStatus.DryRunFailed or DecisionStatus.ApplyFailed => "badge bad",
                DecisionStatus.Deferred => "badge warn",
                DecisionStatus.Draft => "badge info",
                _ => "badge muted",
            };
        }

        /// <summary>
        /// Gets the badge class of a confidence level.
        /// </summary>
        /// <param name="confidence">Confidence.</param>
        /// <returns>CSS class.</returns>
        public static string ConfidenceCss(RecommendationConfidence confidence)
        {
            return confidence switch
            {
                RecommendationConfidence.High => "badge ok",
                RecommendationConfidence.Medium => "badge info",
                RecommendationConfidence.Low => "badge warn",
                _ => "badge bad",
            };
        }

        /// <summary>
        /// Gets a value indicating whether a group still needs the reviewer's attention.
        /// </summary>
        /// <param name="status">Stored status, or null.</param>
        /// <returns>True when the group needs attention.</returns>
        public static bool NeedsAttention(DecisionStatus? status)
        {
            return status is null or DecisionStatus.Draft or DecisionStatus.DryRunFailed or DecisionStatus.ApplyFailed;
        }

        /// <summary>
        /// Shortens a hash for display.
        /// </summary>
        /// <param name="hash">Hash.</param>
        /// <returns>Short hash.</returns>
        public static string ShortHash(string? hash)
        {
            return string.IsNullOrEmpty(hash) ? None : hash.Length <= 12 ? hash : hash[..12] + "…";
        }

        /// <summary>
        /// Joins integers.
        /// </summary>
        /// <param name="values">Values.</param>
        /// <returns>Text.</returns>
        public static string Ids(IEnumerable<int> values)
        {
            string joined = string.Join(", ", values.Select(value => value.ToString(CultureInfo.InvariantCulture)));
            return joined.Length == 0 ? None : joined;
        }
    }
}
