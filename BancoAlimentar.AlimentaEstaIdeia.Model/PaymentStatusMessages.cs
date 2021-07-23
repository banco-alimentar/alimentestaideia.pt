// -----------------------------------------------------------------------
// <copyright file="PaymentStatusMessages.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using System.Collections.Generic;

    /// <summary>
    /// Represent all the string that the payment system use in the status column.
    /// </summary>
    public static class PaymentStatusMessages
    {
        private static readonly IEnumerable<string> SuccessPaymentMessagesValue;
        private static readonly IEnumerable<string> FailedPaymentMessagesValue;

        static PaymentStatusMessages()
        {
            SuccessPaymentMessagesValue = new List<string>()
            {
                "Success",
                "ok",
            };

            FailedPaymentMessagesValue = new List<string>()
            {
                "Failed",
                "err",
            };
        }

        /// <summary>
        /// Gets the success payment messages.
        /// </summary>
        public static IEnumerable<string> SuccessPaymentMessages { get => SuccessPaymentMessagesValue; }

        /// <summary>
        /// Gets the failed payment messages.
        /// </summary>
        public static IEnumerable<string> FailedPaymentMessages { get => FailedPaymentMessagesValue; }
    }
}
