// -----------------------------------------------------------------------
// <copyright file="EasyPayGenericNotification.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Api
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services.EasyPay;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry;
    using Easypay.Rest.Client.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Easypay payment notification handler.
    /// </summary>
    [Route("easypay/generic")]
    [ApiController]
    public class EasyPayGenericNotification : EasyPayControllerBase
    {
        private readonly IUnitOfWork context;
        private readonly IConfiguration configuration;
        private readonly IMail mail;
        private readonly TelemetryClient telemetryClient;
        private readonly IEasyPayWebhookVerifier webhookVerifier;

        /// <summary>
        /// Initializes a new instance of the <see cref="EasyPayGenericNotification"/> class.
        /// </summary>
        /// <param name="context">Unit of context.</param>
        /// <param name="configuration">Configuration.</param>
        /// <param name="telemetryClient">Telemetry client.</param>
        /// <param name="mail">Mail service.</param>
        /// <param name="webhookVerifier">Easypay webhook verifier.</param>
        public EasyPayGenericNotification(
            IUnitOfWork context,
            IConfiguration configuration,
            TelemetryClient telemetryClient,
            IMail mail,
            IEasyPayWebhookVerifier webhookVerifier)
            : base(context, configuration, telemetryClient, mail)
        {
            this.context = context;
            this.configuration = configuration;
            this.mail = mail;
            this.telemetryClient = telemetryClient;
            this.webhookVerifier = webhookVerifier;
        }

        /// <summary>
        /// Execute the post operation.
        /// </summary>
        /// <param name="notificationRequest">Easypay transaction payment notification value.</param>
        /// <returns>A json with what we process.</returns>
        [HttpPost]
        public async Task<IActionResult> PostAsync(NotificationGeneric notificationRequest)
        {
            int paymentId = 0;
            int donationId = -1;
            Collection<string> messages = new Collection<string>();
            this.HttpContext.Items.Add(KeyNames.GenericNotificationKey, notificationRequest);
            if (notificationRequest == null)
            {
                return this.WebhookVerificationFailed(EasyPayWebhookVerificationResult.Invalid("empty_body"));
            }

            if (notificationRequest.Type != NotificationGeneric.TypeEnum.Capture
                && notificationRequest.Type != NotificationGeneric.TypeEnum.SubscriptionCreate
                && notificationRequest.Type != NotificationGeneric.TypeEnum.SubscriptionCapture)
            {
                this.telemetryClient.TrackEvent(
                    "EasypayGenericNotificationIgnored",
                    new System.Collections.Generic.Dictionary<string, string>
                    {
                        { "Type", notificationRequest.Type?.ToString() ?? "missing" },
                    });
                return this.Ok();
            }

            var verification = await this.webhookVerifier.VerifyGenericNotificationAsync(notificationRequest);
            if (!verification.IsValid)
            {
                return this.WebhookVerificationFailed(verification);
            }

            if (notificationRequest.Type == NotificationGeneric.TypeEnum.SubscriptionCreate)
                {
                    int subscriptionId = this.context.SubscriptionRepository.CompleteSubcriptionCreate(
                        notificationRequest.Key,
                        notificationRequest.Status.Value);
                    messages.Add($"Subscription created {subscriptionId}");
                }
                else if (notificationRequest.Type == NotificationGeneric.TypeEnum.SubscriptionCapture)
                {
                    if (verification.VerifiedSubscriptionPayment == null || !notificationRequest.Status.HasValue)
                    {
                        return this.WebhookVerificationFailed(
                            EasyPayWebhookVerificationResult.Invalid("missing_subscription_payment_evidence"));
                    }

                    (int subcriptionDonationId, string reason) = this.context.SubscriptionRepository.CompleteSubscriptionCapture(
                        verification.VerifiedSubscriptionPayment,
                        notificationRequest.Status.Value);

                    if (notificationRequest.Status == NotificationGeneric.StatusEnum.Success
                        && subcriptionDonationId > 0)
                    {
                        Donation subscriptionDonation = this.context.Donation.GetFullDonationById(subcriptionDonationId);
                        int subscriptionPaymentId = subscriptionDonation?.ConfirmedPayment?.Id ?? 0;
                        if (subscriptionPaymentId > 0)
                        {
                            await this.SendInvoiceEmail(subcriptionDonationId, notificationRequest.Key, subscriptionPaymentId);
                        }
                    }
                    else if (notificationRequest.Status == NotificationGeneric.StatusEnum.Failed
                        && subcriptionDonationId > 0)
                    {
                        IActionResult emailResult = this.SendSubscriptionPaymentFailedEmail(
                            subcriptionDonationId,
                            verification.VerifiedSubscriptionPayment.EasypayPaymentId);
                        if (emailResult != null)
                        {
                            return emailResult;
                        }
                    }

                    messages.Add($"Subcription capture, new donation id {subcriptionDonationId}");
                    messages.Add($"{NotificationGeneric.TypeEnum.SubscriptionCapture} exit reason {reason}");
                }
                else
                {
                    (paymentId, donationId) = this.context.Donation.UpdatePaymentTransaction(
                        notificationRequest.Id.ToString(),
                        notificationRequest.Key,
                        notificationRequest.Status,
                        notificationRequest.Messages.FirstOrDefault());

                    messages.Add($"Alimenteestaideia: Generic notification completed for payment id {paymentId}, multibanco donation id {donationId} (it maybe null)");
                }

            this.HttpContext.Items.Add(KeyNames.DonationIdKey, donationId);

            int statusCode = (int)HttpStatusCode.OK;
            if (donationId == 0)
            {
                statusCode = (int)HttpStatusCode.NotFound;
            }

            return new JsonResult(new StatusDetails()
            {
                Status = "ok",
                Message = messages,
            })
            {
                StatusCode = statusCode,
            };
        }

        private IActionResult SendSubscriptionPaymentFailedEmail(int donationId, string easyPayPaymentId)
        {
            if (!this.configuration.IsSendingEmailEnabled())
            {
                this.telemetryClient.TrackEvent("SubscriptionPaymentFailureEmailDisabled");
                return null;
            }

            Donation donation = this.context.Donation.GetFullDonationById(donationId);
            BancoAlimentar.AlimentaEstaIdeia.Model.Subscription subscription =
                this.context.SubscriptionRepository.GetSubscriptionFromDonationId(donationId);
            var matchingPayments = this.context.Donation.GetPaymentsForDonation(donationId)
                .OfType<CreditCardPayment>()
                .Where(payment => string.Equals(
                    payment.EasyPayPaymentId,
                    easyPayPaymentId,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (donation?.User == null || subscription == null || matchingPayments.Count != 1)
            {
                throw new InvalidOperationException("The failed subscription payment could not be resolved for donor notification.");
            }

            CreditCardPayment payment = matchingPayments[0];
            string subject = this.configuration["Email.Subscription.PaymentFailed.Subject"];
            if (!this.context.PaymentNotificationRepository.TryAddEmailNotification(
                donation.User,
                payment,
                subject,
                NotificationType.SubscriptionPaymentFailureEmail))
            {
                return null;
            }

            try
            {
                bool sent = this.mail.SendSubscriptionPaymentFailedEmail(
                    donation,
                    subscription.PublicId,
                    this.Request,
                    payment.Id);
                if (!sent)
                {
                    this.context.PaymentNotificationRepository.MarkEmailNotificationFailed(
                        payment.Id,
                        this.mail.LastSendError ?? "The subscription payment failure email could not be sent.",
                        NotificationType.SubscriptionPaymentFailureEmail);
                    return this.StatusCode((int)HttpStatusCode.ServiceUnavailable);
                }

                this.context.PaymentNotificationRepository.MarkEmailNotificationSent(
                    payment.Id,
                    NotificationType.SubscriptionPaymentFailureEmail);
                return null;
            }
            catch (Exception exception)
            {
                this.context.PaymentNotificationRepository.MarkEmailNotificationFailed(
                    payment.Id,
                    exception.Message,
                    NotificationType.SubscriptionPaymentFailureEmail);
                this.telemetryClient.TrackException(exception);
                throw;
            }
        }
    }
}
