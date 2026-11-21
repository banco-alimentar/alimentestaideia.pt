// -----------------------------------------------------------------------
// <copyright file="SubscriptionInfo.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data
{
    using System;

    /// <summary>
    /// Subscription related to a donation.
    /// </summary>
    /// <param name="Id">Subscription ID.</param>
    /// <param name="Status">Status.</param>
    /// <param name="SubscriptionType">Payment type.</param>
    /// <param name="Frequency">Frequency.</param>
    /// <param name="Created">Creation time.</param>
    /// <param name="StartTime">Start time.</param>
    /// <param name="ExpirationTime">Expiration time.</param>
    /// <param name="EasyPaySubscriptionId">Provider subscription ID.</param>
    /// <param name="TransactionKey">Transaction key.</param>
    /// <param name="IsDeleted">Whether the subscription is deleted.</param>
    /// <param name="InitialDonationId">Initial donation.</param>
    /// <param name="DonationCount">Number of linked donations.</param>
    public sealed record SubscriptionInfo(
        int Id,
        string Status,
        string SubscriptionType,
        string? Frequency,
        DateTime Created,
        DateTime StartTime,
        DateTime ExpirationTime,
        string? EasyPaySubscriptionId,
        string? TransactionKey,
        bool IsDeleted,
        int? InitialDonationId,
        int DonationCount);
}
