// -----------------------------------------------------------------------
// <copyright file="EmailTest.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Displays email configuration and sends an administrator test email.
    /// </summary>
    [Authorize(Policy = "AdminArea")]
    public class EmailTestModel : PageModel
    {
        private readonly IConfiguration configuration;
        private readonly IMail mail;
        private readonly IStringLocalizer<AdminSharedResources> localizer;
        private readonly IStringLocalizer<IdentitySharedResources> identityLocalizer;
        private readonly ILogger<EmailTestModel> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmailTestModel"/> class.
        /// </summary>
        /// <param name="configuration">Tenant-resolved application configuration.</param>
        /// <param name="mail">Email sender.</param>
        /// <param name="localizer">Page localizer.</param>
        /// <param name="identityLocalizer">Identity email localizer.</param>
        /// <param name="logger">Page logger.</param>
        public EmailTestModel(
            IConfiguration configuration,
            IMail mail,
            IStringLocalizer<AdminSharedResources> localizer,
            IStringLocalizer<IdentitySharedResources> identityLocalizer,
            ILogger<EmailTestModel> logger)
        {
            this.configuration = configuration;
            this.mail = mail;
            this.localizer = localizer;
            this.identityLocalizer = identityLocalizer;
            this.logger = logger;
        }

        /// <summary>
        /// Gets the current email settings safe for display.
        /// </summary>
        public EmailSettings Settings { get; private set; } = new EmailSettings();

        /// <summary>
        /// Gets or sets the test email recipient.
        /// </summary>
        [BindProperty]
        [Required]
        [EmailAddress]
        public string TestEmailAddress { get; set; }

        /// <summary>
        /// Gets or sets the non-payment communication to test.
        /// </summary>
        [BindProperty]
        public string? TestCommunicationType { get; set; }

        /// <summary>
        /// Gets the non-payment communication types supported by the test page.
        /// </summary>
        public IReadOnlyList<CommunicationOption> CommunicationOptions => new List<CommunicationOption>
        {
            new CommunicationOption("EmailConfirmation", this.localizer["EmailCommunicationTypeEmailConfirmation"].Value),
            new CommunicationOption("PasswordReset", this.localizer["EmailCommunicationTypePasswordReset"].Value),
            new CommunicationOption("EmailLoginCode", this.localizer["EmailCommunicationTypeEmailLoginCode"].Value),
            new CommunicationOption("ExternalLoginVerification", this.localizer["EmailCommunicationTypeExternalLoginVerification"].Value),
        };

        /// <summary>
        /// Gets or sets the success message.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets the technical details of the failed send attempt.
        /// </summary>
        public string ErrorDetail { get; private set; }

        /// <summary>
        /// Displays the current email settings.
        /// </summary>
        public void OnGet()
        {
            this.LoadSettings();
        }

        /// <summary>
        /// Sends a test email using the current email configuration.
        /// </summary>
        /// <returns>The result page.</returns>
        public IActionResult OnPostSend()
        {
            this.LoadSettings();

            if (!this.ModelState.IsValid)
            {
                return this.Page();
            }

            if (!this.Settings.IsEmailEnabled)
            {
                this.ErrorMessage = this.localizer["EmailTestDisabled"].Value;
                return this.Page();
            }

            if (!this.Settings.ConfigurationComplete)
            {
                this.ErrorMessage = this.localizer["EmailTestConfigurationIncomplete"].Value;
                return this.Page();
            }

            string subject = this.localizer["EmailTestSubject"].Value;
            string body = this.localizer["EmailTestBody", DateTimeOffset.UtcNow.ToString("u")].Value;
            bool sent = this.mail.SendMail(
                body,
                subject,
                this.TestEmailAddress,
                null,
                null,
                this.configuration);

            if (sent)
            {
                this.logger.LogInformation("Administrator email test sent successfully.");
                this.StatusMessage = this.localizer["EmailTestSent", this.TestEmailAddress].Value;
            }
            else
            {
                this.logger.LogWarning("Administrator email test failed.");
                this.ErrorMessage = this.localizer["EmailTestSendFailed"].Value;
                this.ErrorDetail = this.mail.LastSendError ?? this.localizer["EmailTestUnknownError"].Value;
                return this.Page();
            }

            return this.RedirectToPage();
        }

        /// <summary>
        /// Sends a representative non-payment communication using the normal mail pipeline.
        /// </summary>
        /// <returns>The result page.</returns>
        public IActionResult OnPostSendCommunication()
        {
            this.LoadSettings();

            if (!this.ModelState.IsValid)
            {
                return this.Page();
            }

            if (!this.Settings.IsEmailEnabled)
            {
                this.ErrorMessage = this.localizer["EmailCommunicationTestDisabled"].Value;
                return this.Page();
            }

            if (!this.Settings.ConfigurationComplete)
            {
                this.ErrorMessage = this.localizer["EmailCommunicationTestConfigurationIncomplete"].Value;
                return this.Page();
            }

            if (string.IsNullOrWhiteSpace(this.TestCommunicationType))
            {
                this.ModelState.AddModelError(
                    nameof(this.TestCommunicationType),
                    this.localizer["EmailCommunicationTestInvalidType"].Value);
                return this.Page();
            }

            (string Subject, string Body) communication = this.BuildCommunication(this.TestCommunicationType);
            if (communication == default)
            {
                this.ModelState.AddModelError(
                    nameof(this.TestCommunicationType),
                    this.localizer["EmailCommunicationTestInvalidType"].Value);
                return this.Page();
            }

            bool sent = this.mail.SendMail(
                communication.Body,
                communication.Subject,
                this.TestEmailAddress,
                null,
                null,
                this.configuration);

            if (sent)
            {
                this.logger.LogInformation(
                    "Administrator tested non-payment communication {CommunicationType} to {EmailAddress}.",
                    this.TestCommunicationType,
                    this.TestEmailAddress);
                this.StatusMessage = this.localizer["EmailCommunicationTestSent", this.TestEmailAddress].Value;
            }
            else
            {
                this.logger.LogWarning(
                    "Administrator non-payment communication test failed for {CommunicationType}.",
                    this.TestCommunicationType);
                this.ErrorMessage = this.localizer["EmailCommunicationTestSendFailed"].Value;
                this.ErrorDetail = this.mail.LastSendError ?? this.localizer["EmailCommunicationTestUnknownError"].Value;
                return this.Page();
            }

            return this.RedirectToPage();
        }

        private static bool HasConfiguredValue(string value)
        {
            return !string.IsNullOrWhiteSpace(value)
                && !(value.StartsWith("#{", StringComparison.Ordinal) && value.EndsWith("}#", StringComparison.Ordinal));
        }

        private (string Subject, string Body) BuildCommunication(string communicationType)
        {
            string testMarker = this.localizer["EmailCommunicationTestBodyPrefix", DateTimeOffset.UtcNow.ToString("u")].Value;
            string sampleConfirmationUrl = this.Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = "test-user", code = "test-code" },
                protocol: this.Request.Scheme);
            string sampleResetUrl = this.Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { area = "Identity", code = "test-code" },
                protocol: this.Request.Scheme);

            return communicationType switch
            {
                "EmailConfirmation" => (
                    this.identityLocalizer["ConfirmEmailSubject"].Value,
                    $"<p>{testMarker}</p>{string.Format(this.identityLocalizer["ConfirmEmailBody"].Value, System.Net.WebUtility.HtmlEncode(sampleConfirmationUrl))}"),
                "PasswordReset" => (
                    this.identityLocalizer["ResetPassword"].Value,
                    $"<p>{testMarker}</p>{this.identityLocalizer["ResetPasswordMessage"].Value} <a href='{System.Net.WebUtility.HtmlEncode(sampleResetUrl)}'>{this.identityLocalizer["ResetPasswordClickHere"].Value}</a>."),
                "EmailLoginCode" => (
                    this.identityLocalizer["EmailLoginCodeSubject"].Value,
                    $"<p>{testMarker}</p>{string.Format(this.identityLocalizer["EmailLoginCodeBody"].Value, "123456")}"),
                "ExternalLoginVerification" => (
                    this.identityLocalizer["ExternalLoginEmailVerificationSubject"].Value,
                    $"<p>{testMarker}</p>{string.Format(this.identityLocalizer["ExternalLoginEmailVerificationBody"].Value, "123456", "Microsoft")}"),
                _ => default,
            };
        }

        private void LoadSettings()
        {
            this.Settings = new EmailSettings
            {
                IsEmailEnabled = this.GetBoolean("IsEmailEnabled"),
                EmailFrom = this.GetDisplayValue("EmailFrom"),
                SmtpHost = this.GetDisplayValue("Smtp:Host"),
                SmtpPort = this.GetDisplayValue("Smtp:Port"),
                SmtpUser = this.GetDisplayValue("Smtp:User"),
                UseCredentials = this.GetBooleanDisplayValue("Smtp:UseCredentials"),
                EnableSsl = this.GetBooleanDisplayValue("Smtp:EnableSsl"),
                PasswordConfigured = HasConfiguredValue(this.configuration["Smtp:Password"]),
                ConfigurationComplete = this.IsConfigurationComplete(),
            };
        }

        private bool IsConfigurationComplete()
        {
            string emailFrom = this.configuration["EmailFrom"];
            string smtpHost = this.configuration["Smtp:Host"];
            string smtpPort = this.configuration["Smtp:Port"];
            string useCredentials = this.configuration["Smtp:UseCredentials"];
            string enableSsl = this.configuration["Smtp:EnableSsl"];

            if (!HasConfiguredValue(emailFrom)
                || !HasConfiguredValue(smtpHost)
                || !int.TryParse(smtpPort, out int port)
                || port < 1
                || port > 65535
                || !bool.TryParse(useCredentials, out bool credentialsEnabled)
                || !bool.TryParse(enableSsl, out _))
            {
                return false;
            }

            try
            {
                _ = new System.Net.Mail.MailAddress(emailFrom);
            }
            catch (FormatException)
            {
                return false;
            }

            return !credentialsEnabled
                || (HasConfiguredValue(this.configuration["Smtp:User"])
                    && HasConfiguredValue(this.configuration["Smtp:Password"]));
        }

        private bool GetBoolean(string key)
        {
            return bool.TryParse(this.configuration[key], out bool value) && value;
        }

        private string GetBooleanDisplayValue(string key)
        {
            string value = this.configuration[key];
            if (!bool.TryParse(value, out bool parsedValue))
            {
                return this.localizer["NotConfigured"].Value;
            }

            return this.localizer[parsedValue ? "Enabled" : "Disabled"].Value;
        }

        private string GetDisplayValue(string key)
        {
            return HasConfiguredValue(this.configuration[key])
                ? this.configuration[key]
                : this.localizer["NotConfigured"].Value;
        }

        /// <summary>
        /// Email settings that are safe to display to an administrator.
        /// </summary>
        public sealed class EmailSettings
        {
            /// <summary>
            /// Gets or sets a value indicating whether email sending is enabled.
            /// </summary>
            public bool IsEmailEnabled { get; set; }

            /// <summary>
            /// Gets or sets the configured sender address.
            /// </summary>
            public string EmailFrom { get; set; }

            /// <summary>
            /// Gets or sets the SMTP host.
            /// </summary>
            public string SmtpHost { get; set; }

            /// <summary>
            /// Gets or sets the SMTP port.
            /// </summary>
            public string SmtpPort { get; set; }

            /// <summary>
            /// Gets or sets the SMTP username.
            /// </summary>
            public string SmtpUser { get; set; }

            /// <summary>
            /// Gets or sets the credential usage setting.
            /// </summary>
            public string UseCredentials { get; set; }

            /// <summary>
            /// Gets or sets the SSL setting.
            /// </summary>
            public string EnableSsl { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether an SMTP password is configured.
            /// </summary>
            public bool PasswordConfigured { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the settings are ready to send email.
            /// </summary>
            public bool ConfigurationComplete { get; set; }
        }

        /// <summary>
        /// Describes a communication that can be tested without creating account or payment data.
        /// </summary>
        public sealed class CommunicationOption
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="CommunicationOption"/> class.
            /// </summary>
            /// <param name="value">Stable option value.</param>
            /// <param name="displayName">Localized display name.</param>
            public CommunicationOption(string value, string displayName)
            {
                this.Value = value;
                this.DisplayName = displayName;
            }

            /// <summary>
            /// Gets the stable option value.
            /// </summary>
            public string Value { get; }

            /// <summary>
            /// Gets the localized display name.
            /// </summary>
            public string DisplayName { get; }
        }
    }
}
