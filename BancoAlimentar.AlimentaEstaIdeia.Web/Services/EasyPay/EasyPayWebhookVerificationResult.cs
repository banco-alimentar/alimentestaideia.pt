// -----------------------------------------------------------------------
// <copyright file="EasyPayWebhookVerificationResult.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services.EasyPay
{
    /// <summary>
    /// Outcome of Easypay webhook verification.
    /// </summary>
    public sealed class EasyPayWebhookVerificationResult
    {
        private EasyPayWebhookVerificationResult(bool isValid, string failureReason)
        {
            this.IsValid = isValid;
            this.FailureReason = failureReason;
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
        /// Creates a successful verification result.
        /// </summary>
        /// <returns>Valid result.</returns>
        public static EasyPayWebhookVerificationResult Valid()
        {
            return new EasyPayWebhookVerificationResult(true, null);
        }

        /// <summary>
        /// Creates a failed verification result.
        /// </summary>
        /// <param name="reason">Failure reason for logging.</param>
        /// <returns>Invalid result.</returns>
        public static EasyPayWebhookVerificationResult Invalid(string reason)
        {
            return new EasyPayWebhookVerificationResult(false, reason ?? "verification_failed");
        }
    }
}
