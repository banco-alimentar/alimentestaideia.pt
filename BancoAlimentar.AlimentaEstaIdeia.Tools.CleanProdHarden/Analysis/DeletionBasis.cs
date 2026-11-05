// -----------------------------------------------------------------------
// <copyright file="DeletionBasis.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis
{
    /// <summary>
    /// The evidence that makes a member safe to delete.
    /// </summary>
    public enum DeletionBasis
    {
        /// <summary>
        /// Unpaid, uncompleted, unconfirmed attempt whose donation is paid by a different successful payment.
        /// </summary>
        SupersededByReplacement,

        /// <summary>
        /// Unpaid, uncompleted, unconfirmed attempt without a donation link or successful status.
        /// </summary>
        AbandonedOrphan,
    }
}
