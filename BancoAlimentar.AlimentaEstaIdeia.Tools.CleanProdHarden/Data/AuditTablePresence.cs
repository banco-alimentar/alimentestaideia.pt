// -----------------------------------------------------------------------
// <copyright file="AuditTablePresence.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data
{
    /// <summary>
    /// Which reconciliation audit tables exist in the target database.
    /// </summary>
    /// <param name="Reconciliation">Whether <c>dbo.EasyPayPaymentIdReconciliationAudit</c> exists.</param>
    /// <param name="Plans">Whether <c>dbo.EasyPayPaymentIdReconciliationPlans</c> exists.</param>
    /// <param name="Retirement">Whether <c>dbo.EasyPayPaymentIdRetirementAudit</c> exists.</param>
    /// <param name="Deletion">Whether <c>dbo.EasyPayPaymentDeletionAudit</c> exists.</param>
    public sealed record AuditTablePresence(bool Reconciliation, bool Plans, bool Retirement, bool Deletion);
}
