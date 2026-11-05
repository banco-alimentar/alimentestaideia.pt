// -----------------------------------------------------------------------
// <copyright file="RecommendationConfidence.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis
{
    /// <summary>
    /// How strongly the evidence supports a recommendation.
    /// </summary>
    public enum RecommendationConfidence
    {
        /// <summary>
        /// The evidence does not support an automatic recommendation.
        /// </summary>
        None,

        /// <summary>
        /// A reviewer must verify external evidence before deciding.
        /// </summary>
        Low,

        /// <summary>
        /// The recommendation is safe, but a reviewer should confirm a business detail.
        /// </summary>
        Medium,

        /// <summary>
        /// The SQL rules prove ownership or retirement safety.
        /// </summary>
        High,
    }
}
