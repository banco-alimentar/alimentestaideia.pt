// -----------------------------------------------------------------------
// <copyright file="GroupDetail.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data
{
    using System.Collections.Generic;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis;

    /// <summary>
    /// Everything the reviewer sees for one group.
    /// </summary>
    public sealed record GroupDetail
    {
        /// <summary>
        /// Gets the analyzed group.
        /// </summary>
        public required AnalyzedGroup Group { get; init; }

        /// <summary>
        /// Gets the database facts used for validation.
        /// </summary>
        public required ExecutionEnvironment Environment { get; init; }

        /// <summary>
        /// Gets the member payments.
        /// </summary>
        public IReadOnlyList<PaymentDetail> Members { get; init; } = new List<PaymentDetail>();

        /// <summary>
        /// Gets the outside payments that confirm members' donations.
        /// </summary>
        public IReadOnlyList<PaymentDetail> Replacements { get; init; } = new List<PaymentDetail>();

        /// <summary>
        /// Gets the related donations.
        /// </summary>
        public IReadOnlyList<DonationDetail> Donations { get; init; } = new List<DonationDetail>();

        /// <summary>
        /// Gets existing audit history for members.
        /// </summary>
        public IReadOnlyList<AuditEntry> AuditHistory { get; init; } = new List<AuditEntry>();
    }
}
