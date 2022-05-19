// -----------------------------------------------------------------------
// <copyright file="EmailSender.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services;

using System.Threading.Tasks;
using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;

/// <summary>
/// ASP.NET Core email sender for security code.
/// </summary>
public class EmailSender : IEmailSender
{
    private readonly IConfiguration configuration;
    private readonly IMail mail;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailSender"/> class.
    /// </summary>
    /// <param name="configuration">Configuration.</param>
    /// <param name="mail">Mail service.</param>
    public EmailSender(IConfiguration configuration, IMail mail)
    {
        this.configuration = configuration;
        this.mail = mail;
    }

    /// <summary>
    /// Send the email with the security code.
    /// </summary>
    /// <param name="email">Email address.</param>
    /// <param name="subject">Email subject.</param>
    /// <param name="message">Email message.</param>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    public Task SendEmailAsync(string email, string subject, string message)
    {
        this.mail.SendMail(message, subject, email, null, null, this.configuration);

        return Task.CompletedTask;
    }
}
