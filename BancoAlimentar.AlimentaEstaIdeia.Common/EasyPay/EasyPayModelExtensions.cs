// -----------------------------------------------------------------------
// <copyright file="EasyPayModelExtensions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Common.EasyPay
{
    using System;
    using Easypay.Rest.Client.Api;
    using Easypay.Rest.Client.Model;

    /// <summary>
    /// Helpers for mapping Easypay SDK models to application payment semantics.
    /// </summary>
    public static class EasyPayModelExtensions
    {
        /// <summary>
        /// Resolves the payment status for a single-payment lookup response.
        /// </summary>
        /// <param name="payment">Easypay single payment payload.</param>
        /// <returns>Mapped payment status.</returns>
        public static SinglePaymentStatus ResolvePaymentStatus(this InlineObject9 payment)
        {
            if (payment == null)
            {
                return SinglePaymentStatus.Error;
            }

            if (!string.IsNullOrWhiteSpace(payment.PaidAt))
            {
                return SinglePaymentStatus.Paid;
            }

            if (payment.Capture?.Status == CaptureStatus.Success)
            {
                return SinglePaymentStatus.Paid;
            }

            string methodStatus = payment.Method?.Status;
            if (string.IsNullOrWhiteSpace(methodStatus))
            {
                return SinglePaymentStatus.Error;
            }

            return methodStatus.ToLowerInvariant() switch
            {
                "pending" => SinglePaymentStatus.Pending,
                "paid" => SinglePaymentStatus.Paid,
                "failed" => SinglePaymentStatus.Failed,
                "error" => SinglePaymentStatus.Error,
                "deleted" => SinglePaymentStatus.Deleted,
                "authorised" => SinglePaymentStatus.Authorised,
                "active" => SinglePaymentStatus.Active,
                _ => SinglePaymentStatus.Error,
            };
        }

        /// <summary>
        /// Resolves the credit-card checkout URL for a subscription create response.
        /// </summary>
        /// <param name="response">Subscription create response.</param>
        /// <param name="subscriptionApi">Subscription API client.</param>
        /// <returns>Checkout URL when available.</returns>
        public static string ResolveCheckoutUrl(
            this SubscriptionPost201Response response,
            ISubscriptionPaymentApi subscriptionApi)
        {
            if (response == null || subscriptionApi == null || !Guid.TryParse(response.Id, out Guid subscriptionId))
            {
                return null;
            }

            SubscriptionIdGet200Response details = subscriptionApi.SubscriptionIdGet(subscriptionId);
            return details?.Method?.Url;
        }
    }
}
