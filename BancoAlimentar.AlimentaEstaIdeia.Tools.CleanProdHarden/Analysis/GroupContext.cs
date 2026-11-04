// -----------------------------------------------------------------------
// <copyright file="GroupContext.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis
{
    using System.Collections.Generic;

    /// <summary>
    /// Extra evidence, beyond the SQL rules, used to explain a group and recommend an owner.
    /// </summary>
    public sealed record GroupContext
    {
        /// <summary>
        /// Gets an empty context.
        /// </summary>
        public static GroupContext Empty { get; } = new GroupContext();

        /// <summary>
        /// Gets the related donation IDs that have at least one invoice.
        /// </summary>
        public IReadOnlySet<int> DonationIdsWithInvoice { get; init; } = new HashSet<int>();

        /// <summary>
        /// Gets the subscription ID for related donations created by a subscription.
        /// </summary>
        public IReadOnlyDictionary<int, int> SubscriptionIdByDonationId { get; init; } = new Dictionary<int, int>();

        /// <summary>
        /// Gets whether DeleteGroup passes the dependency, permission, and trigger checks; null when unknown.
        /// </summary>
        public bool? CanDeleteGroup { get; init; }
    }
}
