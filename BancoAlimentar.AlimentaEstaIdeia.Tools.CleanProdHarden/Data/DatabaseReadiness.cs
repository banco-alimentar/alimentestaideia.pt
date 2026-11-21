// -----------------------------------------------------------------------
// <copyright file="DatabaseReadiness.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data
{
    using System.Collections.Generic;

    /// <summary>
    /// Target identity and migration preconditions.
    /// </summary>
    public sealed record DatabaseReadiness
    {
        /// <summary>
        /// Gets <c>SERVERPROPERTY('ServerName')</c>.
        /// </summary>
        public string ServerName { get; init; } = string.Empty;

        /// <summary>
        /// Gets <c>DB_NAME()</c>.
        /// </summary>
        public string DatabaseName { get; init; } = string.Empty;

        /// <summary>
        /// Gets <c>ORIGINAL_LOGIN()</c>.
        /// </summary>
        public string Login { get; init; } = string.Empty;

        /// <summary>
        /// Gets the SQL Server version.
        /// </summary>
        public string ProductVersion { get; init; } = string.Empty;

        /// <summary>
        /// Gets the number of provider IDs used by more than one payment (all payment types).
        /// </summary>
        public long DuplicateGroups { get; init; }

        /// <summary>
        /// Gets the number of payments in duplicate groups.
        /// </summary>
        public long DuplicateRows { get; init; }

        /// <summary>
        /// Gets the number of provider IDs longer than 128 characters.
        /// </summary>
        public long OverlengthProviderIds { get; init; }

        /// <summary>
        /// Gets the number of conflicting invoice sequence groups.
        /// </summary>
        public long ConflictingInvoiceGroups { get; init; }

        /// <summary>
        /// Gets whether the hardening migration is applied.
        /// </summary>
        public bool HardenMigrationApplied { get; init; }

        /// <summary>
        /// Gets the most recent applied migrations, newest first.
        /// </summary>
        public IReadOnlyList<string> RecentMigrations { get; init; } = new List<string>();

        /// <summary>
        /// Gets which audit tables exist.
        /// </summary>
        public AuditTablePresence AuditTables { get; init; } = new AuditTablePresence(false, false, false, false);

        /// <summary>
        /// Gets audit row counts by table.
        /// </summary>
        public IReadOnlyDictionary<string, long> AuditRowCounts { get; init; } = new Dictionary<string, long>();

        /// <summary>
        /// Gets whether the hardening migration's preconditions hold.
        /// </summary>
        public bool ReadyForMigration => this.DuplicateGroups == 0 && this.OverlengthProviderIds == 0 && this.ConflictingInvoiceGroups == 0;
    }
}
