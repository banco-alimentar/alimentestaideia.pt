// -----------------------------------------------------------------------
// <copyright file="PaymentNotifications.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model;

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
    /// Gets or sets the type of notification.
    /// </summary>
    public NotificationType NotificationType { get; set; }
}
