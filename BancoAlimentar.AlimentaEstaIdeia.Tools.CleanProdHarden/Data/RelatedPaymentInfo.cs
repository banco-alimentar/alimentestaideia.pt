// -----------------------------------------------------------------------
// <copyright file="RelatedPaymentInfo.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data
{
    using System;

    /// <summary>
    /// A payment related to a donation.
    /// </summary>
    /// <param name="Id">Payment ID.</param>
    /// <param name="PaymentType">Discriminator.</param>
    /// <param name="Status">Status.</param>
    /// <param name="Created">Creation time.</param>
    /// <param name="Completed">Completion time.</param>
    /// <param name="Paid">Paid amount.</param>
    /// <param name="EasyPayPaymentId">Provider ID.</param>
    /// <param name="TransactionKey">Transaction key.</param>
    /// <param name="DonationId">Forward donation link.</param>
    public sealed record RelatedPaymentInfo(
        int Id,
        string PaymentType,
        string? Status,
        DateTime Created,
        DateTime? Completed,
        float? Paid,
        string? EasyPayPaymentId,
        string? TransactionKey,
        int? DonationId);
}
