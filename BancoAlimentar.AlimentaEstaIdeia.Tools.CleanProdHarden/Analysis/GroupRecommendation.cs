// -----------------------------------------------------------------------
// <copyright file="GroupRecommendation.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The explanation and suggested decision for a duplicate group.
    /// </summary>
    public sealed record GroupRecommendation
    {
        /// <summary>
        /// Gets the recognised pattern.
        /// </summary>
        public GroupPattern Pattern { get; init; }

        /// <summary>
        /// Gets the narrative of what most likely happened.
        /// </summary>
        public string Narrative { get; init; } = string.Empty;

        /// <summary>
        /// Gets the recommended action, when the evidence supports one.
        /// </summary>
        public ReconciliationAction? Action { get; init; }

        /// <summary>
        /// Gets the recommended owner for <see cref="ReconciliationAction.ReconcileIdentifiers"/>.
        /// </summary>
        public int? CanonicalPaymentId { get; init; }

        /// <summary>
        /// Gets the confidence in the recommendation.
        /// </summary>
        public RecommendationConfidence Confidence { get; init; }

        /// <summary>
        /// Gets the reasoning behind the recommendation.
        /// </summary>
        public IReadOnlyList<string> Rationale { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the approval reason to prefill in the decision form.
        /// </summary>
        public string SuggestedReason { get; init; } = string.Empty;

        /// <summary>
        /// Gets other actions that are valid for the group.
        /// </summary>
        public IReadOnlyList<ReconciliationAction> Alternatives { get; init; } = Array.Empty<ReconciliationAction>();

        /// <summary>
        /// Gets manual follow-up tasks that the identifier cleanup does not resolve.
        /// </summary>
        public IReadOnlyList<string> FollowUps { get; init; } = Array.Empty<string>();
    }
}
