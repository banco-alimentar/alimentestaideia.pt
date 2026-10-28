// -----------------------------------------------------------------------
// <copyright file="PaymentNotifications.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;

    /// <summary>
    /// Represent a payment notification sent.
    /// </summary>
    public class PaymentNotifications
    {
        /// <summary>
        /// Gets or sets the entity id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the associated payment.
        /// </summary>
        public BasePayment Payment { get; set; }

        /// <summary>
        /// Gets or sets the User.
        /// </summary>
        public WebUser User { get; set; }

        /// <summary>
        /// Gets or sets or set when the notification was sent.
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Gets or sets the subject of the email sent.
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// Gets or sets the type of notification.
        /// </summary>
        public NotificationType NotificationType { get; set; }

        /// <summary>
        /// Gets or sets the delivery state.
        /// </summary>
        public PaymentNotificationDeliveryStatus DeliveryStatus { get; set; }

        /// <summary>
        /// Gets or sets the latest delivery attempt time.
        /// </summary>
        public DateTime? LastAttemptAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the successful delivery time.
        /// </summary>
        public DateTime? SentAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the number of delivery attempts.
        /// </summary>
        public int AttemptCount { get; set; }

        /// <summary>
        /// Gets or sets the latest delivery error.
        /// </summary>
        public string LastError { get; set; }

        /// <summary>
        /// Gets or sets the optimistic concurrency token.
        /// </summary>
        public byte[] RowVersion { get; set; }
    }
}
