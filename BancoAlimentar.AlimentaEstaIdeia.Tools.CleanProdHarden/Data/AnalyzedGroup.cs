// -----------------------------------------------------------------------
// <copyright file="AnalyzedGroup.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data
{
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis;

    /// <summary>
    /// A duplicate group with its evidence hash and recommendation.
    /// </summary>
    /// <param name="Evaluation">Rule evaluation.</param>
    /// <param name="EvidenceHash">SQL evidence hash.</param>
    /// <param name="Context">Invoice and subscription evidence.</param>
    /// <param name="Recommendation">Recommendation.</param>
    /// <param name="CanDelete">Whether DeleteGroup passes every evidence and environment check.</param>
    public sealed record AnalyzedGroup(
        GroupEvaluation Evaluation,
        string EvidenceHash,
        GroupContext Context,
        GroupRecommendation Recommendation,
        bool CanDelete)
    {
        /// <summary>
        /// Gets the group ID.
        /// </summary>
        public int GroupId => this.Evaluation.GroupId;
    }
}
