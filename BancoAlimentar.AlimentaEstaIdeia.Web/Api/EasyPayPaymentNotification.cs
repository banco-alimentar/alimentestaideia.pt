// -----------------------------------------------------------------------
// <copyright file="EasyPayPaymentNotification.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Api
{
    using System;
    using System.Collections.ObjectModel;
    using System.Net;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Api.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services.EasyPay;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry;
    using Easypay.Rest.Client.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore.Metadata.Internal;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Easypay generic notification handler.
    /// </summary>
    [Route("easypay/payment")]
    [ApiController]
    public class EasyPayPaymentNotification : EasyPayControllerBase
    {
        private readonly IUnitOfWork context;
        private readonly IConfiguration configuration;
        private readonly TelemetryClient telemetryClient;
        private readonly IEasyPayWebhookVerifier webhookVerifier;

        /// <summary>
        /// Initializes a new instance of the <see cref="EasyPayPaymentNotification"/> class.
        /// </summary>
        /// <param name="context">Unit of context.</param>
        /// <param name="configuration">Configuration.</param>
        /// <param name="telemetryClient">Telemetry client.</param>
        /// <param name="mail">Mail service.</param>
        /// <param name="webhookVerifier">Easypay webhook verifier.</param>
        public EasyPayPaymentNotification(
            IUnitOfWork context,
            IConfiguration configuration,
            TelemetryClient telemetryClient,
            IMail mail,
            IEasyPayWebhookVerifier webhookVerifier)
            : base(context, configuration, telemetryClient, mail)
        {
            this.context = context;
            this.configuration = configuration;
            this.telemetryClient = telemetryClient;
            this.webhookVerifier = webhookVerifier;
        }

        /// <summary>
        /// Execute the post operation.
        /// </summary>
        /// <param name="value">Easypay transaction notification value.</param>
        /// <returns>A json with what we process.</returns>
        [HttpPost]
        public async Task<IActionResult> PostAsync(TransactionNotificationRequest value)
        {
            this.HttpContext.Items.Add(KeyNames.PaymentNotificationKey, value);
            if (value == null)
            {
                return this.WebhookVerificationFailed(EasyPayWebhookVerificationResult.Invalid("empty_body"));
            }

            var verification = await this.webhookVerifier.VerifyTransactionNotificationAsync(value);
            if (!verification.IsValid)
            {
                return this.WebhookVerificationFailed(verification);
            }

            (int DonationId, int PaymentId) result = (0, 0);
            if (string.Equals(value.Method, "MBW", StringComparison.OrdinalIgnoreCase))
            {
                result = await this.context.Donation.CompleteEasyPayPaymentAsync<MBWayPayment>(
                    value.Id.ToString(),
                    value.Transaction.Key,
                    value.Transaction.Id.ToString(),
                    DateTime.Parse(value.Transaction.Date),
                    (float)value.Transaction.Values.Requested,
                    (float)value.Transaction.Values.Paid,
                    (float)value.Transaction.Values.FixedFee,
                    (float)value.Transaction.Values.VariableFee,
                    (float)value.Transaction.Values.Tax,
                    (float)value.Transaction.Values.Transfer,
                    this.configuration);
            }
            else if (string.Equals(value.Method, "CC", StringComparison.OrdinalIgnoreCase))
            {
                result = await this.context.Donation.CompleteEasyPayPaymentAsync<CreditCardPayment>(
                    value.Id.ToString(),
                    value.Transaction.Key,
                    value.Transaction.Id.ToString(),
                    DateTime.Parse(value.Transaction.Date),
                    (float)value.Transaction.Values.Requested,
                    (float)value.Transaction.Values.Paid,
                    (float)value.Transaction.Values.FixedFee,
                    (float)value.Transaction.Values.VariableFee,
                    (float)value.Transaction.Values.Tax,
                    (float)value.Transaction.Values.Transfer,
                    this.configuration);
            }
            else if (string.Equals(value.Method, "MB", StringComparison.OrdinalIgnoreCase))
            {
                result = await this.context.Donation.CompleteEasyPayPaymentAsync<MultiBankPayment>(
                    value.Id.ToString(),
                    value.Transaction.Key,
                    value.Transaction.Id.ToString(),
                    DateTime.Parse(value.Transaction.Date),
                    (float)value.Transaction.Values.Requested,
                    (float)value.Transaction.Values.Paid,
                    (float)value.Transaction.Values.FixedFee,
                    (float)value.Transaction.Values.VariableFee,
                    (float)value.Transaction.Values.Tax,
                    (float)value.Transaction.Values.Transfer,
                    this.configuration);
            }

            if (result.DonationId == 0)
            {
                result.DonationId = this.context.Donation.GetDonationIdFromPaymentTransactionId(value.Key);
            }

            // Here is only place where we sent the invoice to the customer.
            // After easypay notified us that the payment is correct.
            await this.SendInvoiceEmail(result.DonationId, value.Transaction.Key, result.PaymentId);
            this.HttpContext.Items.Add(KeyNames.DonationIdKey, result.DonationId);

            return new JsonResult(new StatusDetails()
            {
                Status = "ok",
                Message = new Collection<string>() { $"Alimenteestaideia: Payment Completed for donation {result.DonationId}" },
            })
            {
                StatusCode = result.DonationId == 0 ? (int)HttpStatusCode.NotFound : (int)HttpStatusCode.OK,
            };
        }
    }
}
