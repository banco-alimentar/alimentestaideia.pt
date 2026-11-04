// -----------------------------------------------------------------------
// <copyright file="EmailInfo.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data
{
    using System;

    /// <summary>
    /// Email communication linked to a payment.
    /// </summary>
    /// <param name="Id">Email ID.</param>
    /// <param name="PaymentId">Payment.</param>
    /// <param name="SentAtUtc">Send time.</param>
    /// <param name="Subject">Subject.</param>
    /// <param name="ToAddress">Recipient.</param>
    public sealed record EmailInfo(int Id, int PaymentId, DateTime SentAtUtc, string? Subject, string ToAddress);
}
