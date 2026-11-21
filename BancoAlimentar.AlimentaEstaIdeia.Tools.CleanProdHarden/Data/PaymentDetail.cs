// -----------------------------------------------------------------------
// <copyright file="PaymentDetail.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data
{
    using System.Collections.Generic;

    /// <summary>
    /// Every stored column and related record for one payment.
    /// </summary>
    /// <param name="Id">Payment ID.</param>
    /// <param name="Columns">Raw columns as stored, including columns not in the model.</param>
    /// <param name="Notifications">Payment notifications.</param>
    /// <param name="Emails">Email communications.</param>
    /// <param name="LegacyPaymentItemDonationIds">Donations linked through the legacy <c>PaymentItems</c> table.</param>
    public sealed record PaymentDetail(
        int Id,
        IReadOnlyList<KeyValuePair<string, string?>> Columns,
        IReadOnlyList<NotificationInfo> Notifications,
        IReadOnlyList<EmailInfo> Emails,
        IReadOnlyList<int?> LegacyPaymentItemDonationIds);
}
