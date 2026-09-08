// -----------------------------------------------------------------------
// <copyright file="EmailCommunication.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;

    /// <summary>
    /// Stores metadata for an email successfully sent by the application.
    /// The email body is intentionally not stored.
    /// </summary>
    public class EmailCommunication
    {
        /// <summary>
        /// Gets or sets the entity identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the sender address.
        /// </summary>
        public string FromAddress { get; set; }

        /// <summary>
        /// Gets or sets the recipient address.
        /// </summary>
        public string ToAddress { get; set; }

        /// <summary>
        /// Gets or sets the UTC date and time when the email was sent.
        /// </summary>
        public DateTime SentAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the email subject.
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// Gets or sets the optional recipient user identifier.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the optional recipient user.
        /// </summary>
        public WebUser User { get; set; }

        /// <summary>
        /// Gets or sets the optional related payment identifier.
        /// </summary>
        public int? PaymentId { get; set; }

        /// <summary>
        /// Gets or sets the optional related payment.
        /// </summary>
        public BasePayment Payment { get; set; }
    }
}
