using Acn.BA.Gamification.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace Acn.BA.Gamification.Business.Services
{
    public class CustomerMessageService
    {

        const string POKE_TEMPLATE_KEY = "email_poke";
        const string INVITE_TEMPLATE_KEY = "email_convite";
        const string BADGE_TEMPLATE_KEY = "email_badge";
        const string PASSCODE_RECOVERY_TEMPLATE_KEY = "passcode-recovery";

        string _templatesLocation;
        ResourceManager _resourceManager;
        public CustomerMessageService(string templatesLocation, ResourceManager resourceManager)
        {
            _templatesLocation = templatesLocation;
            _resourceManager = resourceManager;
        }

        public void SendPokeMail(User fromUser, User toUser)
        {
            var tpl = GetTemplate(POKE_TEMPLATE_KEY)
                .Replace("${from_user_name}", fromUser.Name)
                .Replace("${to_user_name}", toUser.Name);
            SendMail(tpl, Messages.SubjectPokeEmail, toUser.Email);
        }

        public void SendInviteMail(Invite invite)
        {
            var tpl = GetTemplate(INVITE_TEMPLATE_KEY)
                .Replace("${from_user_name}", invite.UserFrom.Name)
                .Replace("${to_user_name}", invite.UserTo.Name);
            SendMail(tpl, Messages.SubjectInviteEmail, invite.UserTo.Email);
        }

        public void SendBadgeEmail(User user, List<Badge> badges)
        {
            if (badges != null && badges.Count > 0) 
            {
                var badge = badges.Last();
                var tpl = GetTemplate(BADGE_TEMPLATE_KEY)
                    .Replace("${to_user_name}", user.Name)
                    .Replace("${badge_name}", _resourceManager.GetString(badge.Name))
                    .Replace("${badge_desc}", _resourceManager.GetString(badge.Description))
                    .Replace("${badge_image}", $"badge{badge.Id}.png");
                SendMail(tpl, Messages.SubjectBadgeEmail, user.Email);
            }
        }

        public void SendSessionCodeRecoveryEmail(User user) 
        {
            var tpl = GetTemplate(PASSCODE_RECOVERY_TEMPLATE_KEY)
                .Replace("${session_code}", user.SessionCode);
            SendMail(tpl, Messages.SubjectSessioncodeEmail, user.Email);
        }


        private String GetTemplate(string key)
        {
            return File.ReadAllText($"{_templatesLocation}\\{key}.html")
                .Replace("${gmf_images_host}", ConfigurationManager.AppSettings["Gamification.EmailsImagesHost"]);
        }

        private void SendMail(string body, string subject, string mailTo, string stream = null, string attachmentName = null)
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

            var message = new MailMessage
            {
                From = new MailAddress(ConfigurationManager.AppSettings["Email.From"]),
                Body = body,
                Subject = subject,
                BodyEncoding = System.Text.Encoding.UTF8,
                SubjectEncoding = System.Text.Encoding.UTF8,
                IsBodyHtml = true
            };

            message.To.Add(new MailAddress(mailTo));
            message.Bcc.Add(new MailAddress(ConfigurationManager.AppSettings["Email.Bcc"]));

            if (attachment != null) message.Attachments.Add(attachment);

            client.Send(message);
        }
    }
}
