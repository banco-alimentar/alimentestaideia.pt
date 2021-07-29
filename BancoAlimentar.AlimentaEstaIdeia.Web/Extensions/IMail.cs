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
    using Microsoft.Extensions.Configuration;

    public interface IMail
    {
        Task SendInvoiceEmail(Donation donation);

        bool SendMail(string body, string subject, string mailTo, Stream stream, string attachmentName, IConfiguration configuration);

        bool SendMultibancoReferenceMailToDonor(IConfiguration configuration, Donation donation, string messageBodyPath);
    }
}