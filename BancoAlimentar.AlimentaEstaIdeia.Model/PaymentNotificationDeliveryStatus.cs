// -----------------------------------------------------------------------
// <copyright file="PaymentNotificationDeliveryStatus.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    /// <summary>
    /// Payment notification delivery lifecycle.
    /// </summary>
    public enum PaymentNotificationDeliveryStatus
    {
        /// <summary>The notification is currently claimed for delivery.</summary>
        Pending,

        /// <summary>The notification was delivered successfully.</summary>
        Sent,

        /// <summary>The latest delivery attempt failed and may be retried.</summary>
        Failed,
    }
}