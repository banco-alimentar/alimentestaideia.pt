// -----------------------------------------------------------------------
// <copyright file="UserLoginEvent.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model.Identity
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using BancoAlimentar.AlimentaEstaIdeia.Model;

    /// <summary>
    /// Records a successful user sign-in for reporting.
    /// </summary>
    public class UserLoginEvent
    {
        /// <summary>
        /// Gets or sets the event identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        [Required]
        [StringLength(450)]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the login provider key.
        /// </summary>
        [Required]
        [StringLength(128)]
        public string LoginProvider { get; set; }

        /// <summary>
        /// Gets or sets the active campaign when the login occurred.
        /// </summary>
        public int? CampaignId { get; set; }

        /// <summary>
        /// Gets or sets when the login occurred (UTC).
        /// </summary>
        public DateTime OccurredAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the related user.
        /// </summary>
        public virtual WebUser User { get; set; }

        /// <summary>
        /// Gets or sets the related campaign.
        /// </summary>
        public virtual Campaign Campaign { get; set; }
    }
}
