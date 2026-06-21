// -----------------------------------------------------------------------
// <copyright file="ReferralLinkOpen.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using System;

    /// <summary>
    /// Records a single open of a referral donation link (direct or via QR code).
    /// </summary>
    public class ReferralLinkOpen
    {
        /// <summary>
        /// Gets or sets the event identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the referral identifier.
        /// </summary>
        public int ReferralId { get; set; }

        /// <summary>
        /// Gets or sets when the link was opened (UTC).
        /// </summary>
        public DateTime OpenedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the related referral.
        /// </summary>
        public virtual Referral Referral { get; set; }
    }
}
