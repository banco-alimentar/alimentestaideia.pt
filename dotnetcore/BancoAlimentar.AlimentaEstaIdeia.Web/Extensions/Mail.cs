namespace BancoAlimentar.AlimentaEstaIdeia.Web.Extensions
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.Mail;
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

        // public static bool SendPaymentMailToDonor(IConfiguration configuration, Donation donation, string messageBodyPath)
        // {
        //    string subject = configuration["Email.PaymentToDonor.Subject"];
        //    string body = string.Empty;
        //    string mailTo = donation.User.Email;

        // body = File.ReadAllText(messageBodyPath);

        // return SendMail(body, subject, mailTo);
        // }

        // public static bool SendReceiptMailToDonor(DonationByReferenceEntity donationEntity, IList<DonationItemsEntity> donatedItems,
        //                                          string messageBodyPath, string receiptBodyPath)
        // {
        //    string subject = ConfigurationManager.AppSettings["Email.PaymentToDonor.Subject"];
        //    string body = File.ReadAllText(messageBodyPath);
        //    //string mailTo = donationEntity.Email;
        //    string mailTo = ConfigurationManager.AppSettings["Email.ReceiptToBancoAlimentar"];

        // body = File.ReadAllText(messageBodyPath);

        // var nRecibo = donationEntity.Coluna2.ToString().PadLeft(4, '0');

        // Dictionary<string, string> dictionary = new Dictionary<string, string>();
        //    dictionary.Add("<Ano>", DateTime.Now.Year.ToString());
        //    dictionary.Add("<Recibo>", nRecibo);
        //    dictionary.Add("<Nome>", donationEntity.DonorName);
        //    dictionary.Add("<NIF>", donationEntity.NIF);
        //    dictionary.Add("<Quantia>", donationEntity.DonationAmount.ToString());
        //    dictionary.Add("<Extenso>", Converstion.MoneyToString(donationEntity.DonationAmount.ToString()));
        //    dictionary.Add("<Data>", DateTime.Now.ToString(""));

        // var destFilename = string.Format("{0}.docx", Path.GetRandomFileName());
        //    var destFile = string.Format("{0}{1}.docx", Path.GetTempPath(), destFilename);
        //    var templateDirectory = ConfigurationManager.AppSettings["InvoiceDirectory"];
        //    string invoice;

        // if (System.Web.HttpContext.Current != null)
        //    {
        //        invoice = string.Format("~{0}ReceiptTemplate.docx", System.Web.HttpContext.Current.Server.MapPath(templateDirectory));
        //    }
        //    else
        //    {
        //        invoice = string.Format("{0}ReceiptTemplate.docx", templateDirectory);
        //    }

        // var invoiceFile = TemplateService.ApplyDocxTemplate(invoice, dictionary, destFile);

        // /*bool mailResult;

        // using (Stream stream = new FileStream(destFile, FileMode.Open))
        //    {
        //        var bytes = new byte[stream.Length];
        //        stream.Read(bytes, 0, (int)stream.Length);
        //        stream.Position = 0;
        //        mailResult = SendMail(body, subject, mailTo, stream, string.Format("C{0}-{1}.docx", DateTime.Now.Year, nRecibo));
        //    }*/

        // var mailResult = SendMail(body, subject, mailTo, invoiceFile, string.Format("C{0}-{1}.docx", DateTime.Now.Year, nRecibo));

        // File.Delete(destFile);

        // return true;
        // }

        // public static bool SendPaymentMailToBancoAlimentar(DonationByReferenceEntity donationEntity, IList<
        //                                                                                                 DonationItemsEntity
        //                                                                                                 >
        //                                                                                                 donatedItems,
        //                                                   string messageBodyPath)
        // {
        //    string subject = ConfigurationManager.AppSettings["Email.PaymentToBancoAlimentar.Subject"];
        //    string body = string.Empty;
        //    string mailTo = ConfigurationManager.AppSettings["Email.BancoAlimentar"];

        // string mailBody = File.ReadAllText(messageBodyPath);

        // body = string.Format(mailBody, donationEntity.DonorName, donationEntity.Address1, donationEntity.PostalCode, donationEntity.NIF, donationEntity.DonationAmount);

        // return SendMail(body, subject, mailTo);
        // }

        public static bool SendMail(string body, string subject, string mailTo, string stream, string attachmentName, IConfiguration configuration)
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

            bool enableSsl = Convert.ToBoolean(configuration["Smtp:EanbleSSL"]);
            client.EnableSsl = enableSsl;

            client.DeliveryMethod = SmtpDeliveryMethod.Network;

            Attachment attachment = null;

            if (stream != null)
            {
                attachment = new Attachment(stream, attachmentName);
            }

            var message = new MailMessage
            {
                From = new MailAddress(configuration["Email.From"]),
                Body = body,
                Subject = subject,
                BodyEncoding = System.Text.Encoding.UTF8,
                SubjectEncoding = System.Text.Encoding.UTF8,
                IsBodyHtml = true,
            };

            message.To.Add(new MailAddress(mailTo));

           // message.Bcc.Add(new MailAddress(configuration["Email:Bcc"]));

            if (attachment != null)
            {
                message.Attachments.Add(attachment);
            }

            client.Send(message);

            return true;
        }
    }
}
