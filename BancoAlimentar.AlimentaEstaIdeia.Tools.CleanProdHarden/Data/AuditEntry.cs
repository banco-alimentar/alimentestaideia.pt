// -----------------------------------------------------------------------
// <copyright file="AuditEntry.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data
{
    using System;

    /// <summary>
    /// Existing audit history for a payment.
    /// </summary>
    /// <param name="Kind">Audit table: Reconciliation, Retirement, or Deletion.</param>
    /// <param name="PaymentId">Payment.</param>
    /// <param name="RelatedPaymentId">Owner for reconciliation, replacement for deletion.</param>
    /// <param name="OriginalEasyPayPaymentId">Original provider ID.</param>
    /// <param name="AtUtc">When the change was made.</param>
    /// <param name="PlanEvidenceHash">Plan hash, when recorded.</param>
    public sealed record AuditEntry(string Kind, int PaymentId, int? RelatedPaymentId, string OriginalEasyPayPaymentId, DateTime AtUtc, string? PlanEvidenceHash);
}
