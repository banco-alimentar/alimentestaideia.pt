// -----------------------------------------------------------------------
// <copyright file="EasyPayWebhookVerificationResult.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services.EasyPay
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Common.EasyPay;
    using Easypay.Rest.Client.Model;

    /// <summary>
    /// Outcome of Easypay webhook verification.
    /// </summary>
    public sealed class EasyPayWebhookVerificationResult
    {
        private EasyPayWebhookVerificationResult(
            bool isValid,
            string failureReason,
            InlineObject9 verifiedPayment,
            DateTime? verifiedPaymentDate,
            EasyPaySubscriptionPaymentEvidence verifiedSubscriptionPayment)
        {
            this.IsValid = isValid;
            this.FailureReason = failureReason;
            this.VerifiedPayment = verifiedPayment;
            this.VerifiedPaymentDate = verifiedPaymentDate;
            this.VerifiedSubscriptionPayment = verifiedSubscriptionPayment;
        }

        /// <summary>
        /// Gets a value indicating whether the webhook may be processed.
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Gets a short reason when <see cref="IsValid"/> is false.
        /// </summary>
        public string FailureReason { get; }

        /// <summary>
        /// Gets the payment returned by Easypay when a subscription capture was verified.
        /// </summary>
        public InlineObject9 VerifiedPayment { get; }

        /// <summary>
        /// Gets the capture date associated with <see cref="VerifiedPayment"/>.
        /// </summary>
        public DateTime? VerifiedPaymentDate { get; }

        /// <summary>
        /// Gets the embedded transaction evidence when a subscription capture was verified.
        /// </summary>
        public EasyPaySubscriptionPaymentEvidence VerifiedSubscriptionPayment { get; }

        /// <summary>
        /// Creates a successful verification result.
        /// </summary>
        /// <returns>Valid result.</returns>
        public static EasyPayWebhookVerificationResult Valid()
        {
            return new EasyPayWebhookVerificationResult(true, null, null, null, null);
        }

        /// <summary>
        /// Creates a successful verification result containing the provider payment evidence.
        /// </summary>
        /// <param name="verifiedPayment">Payment returned by Easypay.</param>
        /// <param name="verifiedPaymentDate">Provider capture date.</param>
        /// <returns>Valid result.</returns>
        public static EasyPayWebhookVerificationResult Valid(
            InlineObject9 verifiedPayment,
            DateTime verifiedPaymentDate)
        {
            return new EasyPayWebhookVerificationResult(
                true,
                null,
                verifiedPayment,
                verifiedPaymentDate,
                null);
        }

        /// <summary>
        /// Creates a successful verification result containing embedded subscription payment evidence.
        /// </summary>
        /// <param name="verifiedSubscriptionPayment">Verified subscription transaction evidence.</param>
        /// <returns>Valid result.</returns>
        public static EasyPayWebhookVerificationResult Valid(
            EasyPaySubscriptionPaymentEvidence verifiedSubscriptionPayment)
        {
            return new EasyPayWebhookVerificationResult(
                true,
                null,
                null,
                verifiedSubscriptionPayment?.PaymentDate,
                verifiedSubscriptionPayment);
        }

        /// <summary>
        /// Creates a failed verification result.
        /// </summary>
        /// <param name="reason">Failure reason for logging.</param>
        /// <returns>Invalid result.</returns>
        public static EasyPayWebhookVerificationResult Invalid(string reason)
        {
            return new EasyPayWebhookVerificationResult(false, reason ?? "verification_failed", null, null, null);
        }
    }
}
