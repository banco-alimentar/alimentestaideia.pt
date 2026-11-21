// -----------------------------------------------------------------------
// <copyright file="TenantInitializationStatus.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Middleware
{
    using System;

    /// <summary>Secret-free initialization timestamps and outcome.</summary>
    public sealed record TenantInitializationStatus
    {
        /// <summary>Gets a value indicating whether initialization succeeded.</summary>
        public bool Initialized { get; init; }

        /// <summary>Gets a value indicating whether an attempt is running.</summary>
        public bool InProgress { get; init; }

        /// <summary>Gets the last attempt time.</summary>
        public DateTimeOffset? LastAttemptAt { get; init; }

        /// <summary>Gets the last successful initialization time.</summary>
        public DateTimeOffset? LastSuccessAt { get; init; }

        /// <summary>Gets the last failure category without its message.</summary>
        public string? LastFailureType { get; init; }
    }
}
