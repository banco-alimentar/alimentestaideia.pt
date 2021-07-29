// -----------------------------------------------------------------------
// <copyright file="EmailSender.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services
{
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.Extensions.Configuration;

    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration configuration;
        private readonly IMail mail;

        public EmailSender(IConfiguration configuration, IMail mail)
        {
            this.configuration = configuration;
            this.mail = mail;
        }

        public Task SendEmailAsync(string email, string subject, string message)
        {
            this.mail.SendMail(message, subject, email, null, null, this.configuration);

            return Task.CompletedTask;
        }
    }
}
