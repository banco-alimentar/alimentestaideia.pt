// -----------------------------------------------------------------------
// <copyright file="NotificationInfo.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data
{
    using System;

    /// <summary>
    /// Payment notification sent to the donor.
    /// </summary>
    /// <param name="Id">Notification ID.</param>
    /// <param name="PaymentId">Payment.</param>
    /// <param name="Created">Creation time.</param>
    /// <param name="Subject">Subject.</param>
    /// <param name="NotificationType">Type.</param>
    public sealed record NotificationInfo(int Id, int PaymentId, DateTime Created, string? Subject, string NotificationType);
}
