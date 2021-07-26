// -----------------------------------------------------------------------
// <copyright file="Mail.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Extensions
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.Mail;
    using System.Net.Mime;
    using System.Text;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.Extensions.Configuration;

    public class Mail
    {
        // public static bool SendReportImage(DonorsPictureEntity donorIdEntity, string requestUrl)
        // {
        //    const string subject = "[Imagem Abusiva]: Banco Alimentar";
        //    string body = string.Empty;
        //    string mailTo = ConfigurationManager.AppSettings["Report.Image.Mail"];

        // string imageUrl = String.Format("{0}Donation/DisplayImage/?donorId={1}", requestUrl, donorIdEntity.DonorId);

        // body = string.Format("Link para imagem reportada como abusiva. Link: {0}.", imageUrl);

        // return SendMail(body, subject, mailTo);
        // }
        public static bool SendConfirmedPaymentMailToDonor(
            IConfiguration configuration,
            Donation donation,
            string paymentIds,
            string subject,
            string messageBodyPath,
            Stream stream = null,
            string attachmentName = null)
        {
            string body = string.Empty;
            string mailTo = donation.User.Email;

            if (File.Exists(messageBodyPath))
            {
                string mailBody = File.ReadAllText(messageBodyPath);
                mailBody = mailBody.Replace("{donationId}", donation.Id.ToString());
                mailBody = mailBody.Replace("{paymentId}", paymentIds);
                return SendMail(mailBody, subject, mailTo, stream, attachmentName, configuration);
            }
            else
            {
                return false;
            }
        }

        public static bool SendReferenceMailToDonor(IConfiguration configuration, Donation donation, string messageBodyPath)
        {
            string subject = configuration["Email.ReferenceToDonor.Subject"];
            string body = string.Empty;
            string mailTo = donation.User.Email;

            if (File.Exists(messageBodyPath))
            {
                string mailBody = File.ReadAllText(messageBodyPath);
                body = string.Format(mailBody, donation.ServiceEntity, donation.ServiceReference, donation.DonationAmount.ToString("F2", CultureInfo.GetCultureInfo("pt-PT")));
                return SendMail(body, subject, mailTo, null, null, configuration);
            }
            else
            {
                return false;
            }
        }

        public static bool SendMail(string body, string subject, string mailTo, Stream stream, string attachmentName, IConfiguration configuration)
        {
            var client = new SmtpClient
            {
                Host = configuration["Smtp:Host"],
                Port = Convert.ToInt32(configuration["Smtp:Port"]),
            };

            bool useCredentials = Convert.ToBoolean(configuration["Smtp:UseCredentials"]);
            if (useCredentials)
            {
                var smtpUserInfo = new NetworkCredential(
                    configuration["Smtp:User"],
                    configuration["Smtp:Password"]);

                client.UseDefaultCredentials = false;
                client.Credentials = smtpUserInfo;
            }

            bool enableSsl = Convert.ToBoolean(configuration["Smtp:EnableSsl"]);
            client.EnableSsl = enableSsl;

            client.DeliveryMethod = SmtpDeliveryMethod.Network;

            Attachment attachment = null;

            if (stream != null)
            {
                attachment = new Attachment(stream, attachmentName);
                attachment.ContentType = new ContentType("application/pdf; charset=UTF-8");
            }

            var message = new MailMessage
            {
                From = new MailAddress(configuration["EmailFrom"]),
                Body = body,
                Subject = subject,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8,
                IsBodyHtml = true,
            };

            message.To.Add(new MailAddress(mailTo));
            if (attachment != null)
            {
                message.Attachments.Add(attachment);
            }

            client.Send(message);
            return true;
        }
    }
}
