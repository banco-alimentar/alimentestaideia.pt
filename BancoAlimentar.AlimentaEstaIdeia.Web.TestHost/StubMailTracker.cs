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
        /// Gets the body of the most recent generic email.
        /// </summary>
        public string LastBody { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the subject of the most recent generic email.
        /// </summary>
        public string LastSubject { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the recipient of the most recent generic email.
        /// </summary>
        public string LastRecipient { get; private set; } = string.Empty;

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
        /// Records the details of a generic email send.
        /// </summary>
        /// <param name="body">Email body.</param>
        /// <param name="subject">Email subject.</param>
        /// <param name="recipient">Email recipient.</param>
        public void RecordSendMail(string body, string subject, string recipient)
        {
            this.SendMailCalls++;
            this.LastBody = body;
            this.LastSubject = subject;
            this.LastRecipient = recipient;
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
