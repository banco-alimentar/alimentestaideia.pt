// -----------------------------------------------------------------------
// <copyright file="ReplacementPaymentFacts.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis
{
    /// <summary>
    /// The payment that confirms a member's forward donation.
    /// </summary>
    /// <param name="Id">Payment identifier.</param>
    /// <param name="PaymentType">Payment discriminator.</param>
    /// <param name="Status">Provider status.</param>
    /// <param name="Paid">Paid amount, when the payment type stores amounts.</param>
    /// <param name="DonationId">Forward donation link of the replacement payment.</param>
    /// <param name="PayPalPaymentId">PayPal payment identifier.</param>
    /// <param name="PayerId">PayPal payer identifier.</param>
    public sealed record ReplacementPaymentFacts(
        int Id,
        string PaymentType,
        string? Status,
        float? Paid,
        int? DonationId,
        string? PayPalPaymentId,
        string? PayerId);
}
