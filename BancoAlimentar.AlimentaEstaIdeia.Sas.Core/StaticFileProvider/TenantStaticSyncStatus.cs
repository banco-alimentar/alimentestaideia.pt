// -----------------------------------------------------------------------
// <copyright file="TenantStaticSyncStatus.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.StaticFileProvider
{
    using System;

    /// <summary>
    /// A safe, immutable snapshot of a tenant's static asset refresh.
    /// </summary>
    public sealed record TenantStaticSyncStatus
    {
        /// <summary>
        /// Gets the time of the latest attempt.
        /// </summary>
        public DateTimeOffset? LastAttemptAt { get; init; }

        /// <summary>
        /// Gets the time of the latest completed refresh.
        /// </summary>
        public DateTimeOffset? LastSuccessAt { get; init; }

        /// <summary>
        /// Gets the failure category, without exception messages or credentials.
        /// </summary>
        public string? LastFailureType { get; init; }

        /// <summary>
        /// Gets the next scheduled attempt, if a background refresh is scheduled.
        /// </summary>
        public DateTimeOffset? NextAttemptAt { get; init; }
    }
}
