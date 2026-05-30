// -----------------------------------------------------------------------
// <copyright file="StubMail.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.TestHost
{
    using System.IO;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// No-op mail service for integration tests.
    /// </summary>
    public class StubMail : IMail
    {
        private readonly StubMailTracker tracker;

        /// <summary>
        /// Initializes a new instance of the <see cref="StubMail"/> class.
        /// </summary>
        /// <param name="tracker">Mail call tracker.</param>
        public StubMail(StubMailTracker tracker)
        {
            this.tracker = tracker;
        }

        /// <inheritdoc />
        public Task GenerateInvoiceAndSendByEmail(Donation donation, HttpRequest request, IUnitOfWork context, Tenant tenant)
        {
            this.tracker.RecordInvoiceEmail();
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public bool SendMail(string body, string subject, string mailTo, Stream stream, string attachmentName, IConfiguration configuration)
        {
            this.tracker.RecordSendMail();
            return true;
        }

        /// <inheritdoc />
        public bool SendMultibancoReferenceMailToDonor(IConfiguration configuration, Donation donation, string messageBodyPath)
        {
            this.tracker.RecordMultibancoReferenceEmail();
            return true;
        }
    }
}
