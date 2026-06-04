// -----------------------------------------------------------------------
// <copyright file="StubMailTracker.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.TestHost
{
    /// <summary>
    /// Records outbound mail calls from <see cref="StubMail"/> for integration test assertions.
    /// </summary>
    public sealed class StubMailTracker
    {
        /// <summary>
        /// Gets the number of invoice confirmation emails sent.
        /// </summary>
        public int InvoiceEmailsSent { get; private set; }

        /// <summary>
        /// Gets the number of generic <see cref="StubMail.SendMail"/> calls.
        /// </summary>
        public int SendMailCalls { get; private set; }

        /// <summary>
        /// Gets the number of multibanco reference emails sent.
        /// </summary>
        public int MultibancoReferenceEmailsSent { get; private set; }

        /// <summary>
        /// Records an invoice email send.
        /// </summary>
        public void RecordInvoiceEmail()
        {
            this.InvoiceEmailsSent++;
        }

        /// <summary>
        /// Records a generic send-mail call.
        /// </summary>
        public void RecordSendMail()
        {
            this.SendMailCalls++;
        }

        /// <summary>
        /// Records a multibanco reference email send.
        /// </summary>
        public void RecordMultibancoReferenceEmail()
        {
            this.MultibancoReferenceEmailsSent++;
        }
    }
}
