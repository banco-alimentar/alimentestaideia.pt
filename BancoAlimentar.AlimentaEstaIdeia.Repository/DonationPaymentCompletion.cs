// -----------------------------------------------------------------------
// <copyright file="DonationPaymentCompletion.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Easypay.Rest.Client.Model;

    /// <summary>
    /// Shared rules for deciding when a donation payment is successful and amounts reconcile.
    /// </summary>
    public static class DonationPaymentCompletion
    {
        /// <summary>
        /// Returns true when a multibanco payment is still waiting and may receive pay-by-reference communication.
        /// </summary>
        /// <param name="donation">Donation linked to the payment.</param>
        /// <param name="payment">Multibanco payment row.</param>
        /// <returns>True when the donor should still be asked to pay.</returns>
        public static bool IsAwaitingMultiBankPayment(Donation donation, BasePayment payment)
        {
            if (donation == null || payment == null)
            {
                return false;
            }

            if (donation.PaymentStatus == PaymentStatus.Payed)
            {
                return false;
            }

            if (payment.Completed.HasValue)
            {
                return false;
            }

            if (IsSuccessfulPaymentStatus(payment.Status))
            {
                return false;
            }

            return donation.PaymentStatus == PaymentStatus.WaitingPayment
                || donation.PaymentStatus == PaymentStatus.NotPayed;
        }

        /// <summary>
        /// Returns true when the provider status represents a successful payment.
        /// </summary>
        /// <param name="status">Provider payment status.</param>
        /// <returns>True when the status is a known success value.</returns>
        public static bool IsSuccessfulPaymentStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return false;
            }

            if (PaymentStatusMessages.SuccessPaymentMessages.Any(message =>
                    string.Equals(message, status, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return string.Equals(status, "COMPLETED", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Paid", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "paid", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns true when the payment can be treated as a successful completion for the donation.
        /// </summary>
        /// <param name="donation">Donation being paid.</param>
        /// <param name="payment">Candidate confirmed payment.</param>
        /// <param name="requested">Optional requested amount from the provider.</param>
        /// <param name="paid">Optional paid amount from the provider.</param>
        /// <param name="trustProviderPaidStatus">When true, accept a provider-paid signal without stored amounts.</param>
        /// <returns>True when the payment should mark the donation as paid.</returns>
        public static bool CanCompleteDonationPayment(
            Donation donation,
            BasePayment payment,
            float? requested,
            float? paid,
            bool trustProviderPaidStatus = false)
        {
            if (donation == null || payment == null || donation.DonationAmount <= 0)
            {
                return false;
            }

            if (requested.HasValue && paid.HasValue)
            {
                return PaymentAmountReconciliation.AmountsMatchDonation(
                    donation.DonationAmount,
                    requested.Value,
                    paid.Value);
            }

            if (payment is EasyPayWithValuesBaseClass easyPayPayment
                && easyPayPayment.Paid > 0
                && easyPayPayment.Requested > 0)
            {
                return PaymentAmountReconciliation.AmountsMatchDonation(
                    donation.DonationAmount,
                    easyPayPayment.Requested,
                    easyPayPayment.Paid);
            }

            if (payment is PayPalPayment payPalPayment
                && !string.IsNullOrEmpty(payPalPayment.PayPalPaymentId)
                && !string.IsNullOrEmpty(payPalPayment.PayerId)
                && string.Equals(payPalPayment.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (trustProviderPaidStatus)
            {
                return true;
            }

            if (payment is EasyPayWithValuesBaseClass easyPayWithPaidOnly
                && easyPayWithPaidOnly.Paid > 0)
            {
                return PaymentAmountReconciliation.ProviderValueMatchesDonation(
                    donation.DonationAmount,
                    easyPayWithPaidOnly.Paid);
            }

            return payment.Completed.HasValue && IsSuccessfulPaymentStatus(payment.Status);
        }

        /// <summary>
        /// Ensures EasyPay payment rows have requested and paid values before completion.
        /// </summary>
        /// <param name="donation">Donation being paid.</param>
        /// <param name="payment">Payment row to populate when amounts are missing.</param>
        public static void EnsureEasyPayAmountsFromDonation(Donation donation, BasePayment payment)
        {
            if (donation == null || payment is not EasyPayWithValuesBaseClass easyPayPayment)
            {
                return;
            }

            if (easyPayPayment.Requested <= 0)
            {
                easyPayPayment.Requested = (float)donation.DonationAmount;
            }

            if (easyPayPayment.Paid <= 0)
            {
                easyPayPayment.Paid = (float)donation.DonationAmount;
            }
        }

        /// <summary>
        /// Records completion metadata on a successfully paid EasyPay payment row.
        /// </summary>
        /// <param name="payment">Payment that completed successfully.</param>
        public static void MarkSuccessfulEasyPayPayment(BasePayment payment)
        {
            if (payment == null)
            {
                return;
            }

            if (!payment.Completed.HasValue)
            {
                payment.Completed = DateTime.UtcNow;
            }

            if (string.IsNullOrEmpty(payment.Status) || !IsSuccessfulPaymentStatus(payment.Status))
            {
                payment.Status = NotificationGeneric.StatusEnum.Success.ToString();
            }
        }
    }
}
