using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Mail;
using Link.BA.Donate.Models;
using System.Globalization;
using Link.PT.Telegramas.CommonLibrary.Template;

namespace Link.BA.Donate.Business
{
    public class Mail
    {
        public static bool SendReportImage(DonorsPictureEntity donorIdEntity, string requestUrl)
        {
            const string subject = "[Imagem Abusiva]: Banco Alimentar";
            string body = string.Empty;
            string mailTo = ConfigurationManager.AppSettings["Report.Image.Mail"];

            string imageUrl = String.Format("{0}Donation/DisplayImage/?donorId={1}", requestUrl, donorIdEntity.DonorId);

            body = string.Format("Link para imagem reportada como abusiva. Link: {0}.", imageUrl);

            return SendMail(body, subject, mailTo);
        }

        public static bool SendReferenceMailToDonor(Models.Donation donation, string messageBodyPath)
        {
            string subject = ConfigurationManager.AppSettings["Email.ReferenceToDonor.Subject"];
            string body = string.Empty;
            string mailTo = donation.Donor.Email;

            string mailBody = File.ReadAllText(messageBodyPath);

            body = string.Format(mailBody, donation.ServiceEntity, donation.ServiceReference, donation.ServiceAmount.Value.ToString("F2", CultureInfo.GetCultureInfo("pt-PT")));

            return SendMail(body, subject, mailTo);
        }

        public static bool SendPaymentMailToDonor(DonationByReferenceEntity donationEntity, IList<DonationItemsEntity> donatedItems,
                                                  string messageBodyPath)
        {
            string subject = ConfigurationManager.AppSettings["Email.PaymentToDonor.Subject"];
            string body = string.Empty;
            string mailTo = donationEntity.Email;

            body = File.ReadAllText(messageBodyPath);

            return SendMail(body, subject, mailTo);
        }

        public static bool SendReceiptMailToDonor(DonationByReferenceEntity donationEntity, IList<DonationItemsEntity> donatedItems,
                                                  string messageBodyPath, string receiptBodyPath)
        {
            string subject = ConfigurationManager.AppSettings["Email.PaymentToDonor.Subject"];
            string body = File.ReadAllText(messageBodyPath);
            string mailTo = donationEntity.Email;

            body = File.ReadAllText(messageBodyPath);

            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add("<Ano>", DateTime.Now.Year.ToString());
            dictionary.Add("<Recibo>", donationEntity.Coluna2.ToString());
            dictionary.Add("<Nome>", donationEntity.DonorName);
            dictionary.Add("<NIF>", donationEntity.NIF);
            dictionary.Add("<Quantia>", donationEntity.ServiceAmount.ToString());
            dictionary.Add("<Extenso>", Converstion.MoneyToString(donationEntity.ServiceAmount.ToString()));
            dictionary.Add("<Data>", DateTime.Now.ToString(""));

            var destFilename = string.Format("{0}.docx", Path.GetRandomFileName());
            var destFile = string.Format("{0}{1}.docx", Path.GetTempPath(), destFilename);
            var templateDirectory = ConfigurationManager.AppSettings["InvoiceDirectory"];
            var invoice = string.Format("{0}ReceiptTemplate.docx", System.Web.HttpContext.Current.Server.MapPath(templateDirectory));

            TemplateService.ApplyDocxTemplate(invoice, dictionary, destFile);

            bool mailResult;

            using (Stream stream = new FileStream(destFile, FileMode.Open))
            {
                var bytes = new byte[stream.Length];
                stream.Read(bytes, 0, (int)stream.Length);
                stream.Position = 0;
                mailResult = SendMail(body, subject, mailTo, stream, "recibo.docx");
            }

            File.Delete(destFile);

            return mailResult;
        }

        public static bool SendPaymentMailToBancoAlimentar(DonationByReferenceEntity donationEntity, IList<
                                                                                                         DonationItemsEntity
                                                                                                         >
                                                                                                         donatedItems,
                                                           string messageBodyPath)
        {
            string subject = ConfigurationManager.AppSettings["Email.PaymentToBancoAlimentar.Subject"];
            string body = string.Empty;
            string mailTo = ConfigurationManager.AppSettings["Email.BancoAlimentar"];

            string mailBody = File.ReadAllText(messageBodyPath);

            body = string.Format(mailBody, donationEntity.DonorName, donationEntity.Address1, donationEntity.PostalCode, donationEntity.NIF, donationEntity.ServiceAmount);

            return SendMail(body, subject, mailTo);
        }

        private static bool SendMail(string body, string subject, string mailTo)
        {
            return SendMail(body, subject, mailTo, null, null);
        }

        private static bool SendMail(string body, string subject, string mailTo, Stream stream, string attachmentName)
        {
            var client = new SmtpClient
                             {
                                 Host = ConfigurationManager.AppSettings["Smtp.Host"],
                                 Port = Convert.ToInt32(ConfigurationManager.AppSettings["Smtp.Port"])
                             };

            bool useCredentials = Convert.ToBoolean(ConfigurationManager.AppSettings["Smtp.UseCredentials"]);
            if (useCredentials)
            {
                var smtpUserInfo = new NetworkCredential(ConfigurationManager.AppSettings["Smtp.User"],
                                                         ConfigurationManager.AppSettings["Smtp.Password"]);

                client.UseDefaultCredentials = false;
                client.Credentials = smtpUserInfo;
            }
            
            bool enableSsl = Convert.ToBoolean(ConfigurationManager.AppSettings["Smtp.EnableSsl"]);
            client.EnableSsl = enableSsl;

            client.DeliveryMethod = SmtpDeliveryMethod.Network;

            Attachment attachment = null;

            if (stream != null)
                attachment = new Attachment(stream, attachmentName
                    /*, "application/vnd.openxmlformats-officedocument.wordprocessingml.document"*/);

            var message = new MailMessage(ConfigurationManager.AppSettings["Email.From"], mailTo)
                              {
                                  Body = body,
                                  Subject = subject,
                                  BodyEncoding = System.Text.Encoding.UTF8,
                                  SubjectEncoding = System.Text.Encoding.UTF8,
                                  IsBodyHtml = true
                              };

            if (attachment != null) message.Attachments.Add(attachment);

            client.Send(message);

            return true;
        }



    }
}