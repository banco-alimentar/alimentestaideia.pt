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
        /// <inheritdoc />
        public Task GenerateInvoiceAndSendByEmail(Donation donation, HttpRequest request, IUnitOfWork context, Tenant tenant)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public bool SendMail(string body, string subject, string mailTo, Stream stream, string attachmentName, IConfiguration configuration)
        {
            return true;
        }

        /// <inheritdoc />
        public bool SendMultibancoReferenceMailToDonor(IConfiguration configuration, Donation donation, string messageBodyPath)
        {
            return true;
        }
    }
}
