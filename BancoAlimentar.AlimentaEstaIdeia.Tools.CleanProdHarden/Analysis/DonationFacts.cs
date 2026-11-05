// -----------------------------------------------------------------------
// <copyright file="DonationFacts.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;

    /// <summary>
    /// Donation columns used by the decision rules.
    /// </summary>
    /// <param name="Id">Donation identifier.</param>
    /// <param name="PaymentStatus">Donation payment status.</param>
    /// <param name="DonationAmount">Donation amount.</param>
    /// <param name="ConfirmedPaymentId">Payment that confirms the donation.</param>
    public sealed record DonationFacts(int Id, PaymentStatus PaymentStatus, double DonationAmount, int? ConfirmedPaymentId);
}
