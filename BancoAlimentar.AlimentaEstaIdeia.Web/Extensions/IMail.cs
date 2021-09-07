// -----------------------------------------------------------------------
// <copyright file="IMail.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Extensions
{
    using System.IO;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Mail service.
    /// </summary>
    public interface IMail
    {
        /// <summary>
        /// Send the invoice email.
        /// </summary>
        /// <param name="donation">The donation to send the invoice.</param>
        /// <param name="request">Incoming http request.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task SendInvoiceEmail(Donation donation, HttpRequest request);

        /// <summary>
        /// Sends an email.
        /// </summary>
        /// <param name="body">Email body.</param>
        /// <param name="subject">Email subject.</param>
        /// <param name="mailTo">Email destination address.</param>
        /// <param name="stream">Attachment stream.</param>
        /// <param name="attachmentName">Attachment name.</param>
        /// <param name="configuration">Configuration.</param>
        /// <returns>True if the email was sent, false otherwise.</returns>
        bool SendMail(string body, string subject, string mailTo, Stream stream, string attachmentName, IConfiguration configuration);

        /// <summary>
        /// Send the multibanco email with the reference.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="donation">Donation.</param>
        /// <param name="messageBodyPath">Message body path.</param>
        /// <returns>True if the email was sent, false otherwise.</returns>
        bool SendMultibancoReferenceMailToDonor(IConfiguration configuration, Donation donation, string messageBodyPath);
    }
}