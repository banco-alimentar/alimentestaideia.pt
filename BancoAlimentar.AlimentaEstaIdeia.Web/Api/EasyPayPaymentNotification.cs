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
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry;
    using Easypay.Rest.Client.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Easypay generic notification handler.
    /// </summary>
    [Route("easypay/payment")]
    [ApiController]
    public class EasyPayPaymentNotification : EasyPayControllerBase
    {
        private readonly IUnitOfWork context;

        /// <summary>
        /// Initializes a new instance of the <see cref="EasyPayPaymentNotification"/> class.
        /// </summary>
        /// <param name="context">Unit of context.</param>
        /// <param name="configuration">Configuration.</param>
        /// <param name="telemetryClient">Telemetry client.</param>
        /// <param name="mail">Mail service.</param>
        public EasyPayPaymentNotification(
            IUnitOfWork context,
            IConfiguration configuration,
            TelemetryClient telemetryClient,
            IMail mail)
            : base(context, configuration, telemetryClient, mail)
        {
            this.context = context;
        }

        /// <summary>
        /// Execute the post operation.
        /// </summary>
        /// <param name="value">Easypay transaction notification value.</param>
        /// <returns>A json with what we process.</returns>
        public async Task<IActionResult> PostAsync(TransactionNotificationRequest value)
        {
            this.HttpContext.Items.Add(KeyNames.PaymentNotificationKey, value);
            if (value != null)
            {
                int donationId = 0;
                if (string.Equals(value.Method, "MBW", StringComparison.OrdinalIgnoreCase))
                {
                    donationId = this.context.Donation.CompleteEasyPayPayment<MBWayPayment>(
                        value.Id.ToString(),
                        value.Transaction.Key,
                        value.Transaction.Id.ToString(),
                        DateTime.Parse(value.Transaction.Date),
                        (float)value.Transaction.Values.Requested,
                        (float)value.Transaction.Values.Paid,
                        (float)value.Transaction.Values.FixedFee,
                        (float)value.Transaction.Values.VariableFee,
                        (float)value.Transaction.Values.Tax,
                        (float)value.Transaction.Values.Transfer);
                }
                else if (string.Equals(value.Method, "CC", StringComparison.OrdinalIgnoreCase))
                {
                    donationId = this.context.Donation.CompleteEasyPayPayment<CreditCardPayment>(
                        value.Id.ToString(),
                        value.Transaction.Key,
                        value.Transaction.Id.ToString(),
                        DateTime.Parse(value.Transaction.Date),
                        (float)value.Transaction.Values.Requested,
                        (float)value.Transaction.Values.Paid,
                        (float)value.Transaction.Values.FixedFee,
                        (float)value.Transaction.Values.VariableFee,
                        (float)value.Transaction.Values.Tax,
                        (float)value.Transaction.Values.Transfer);

                    if (this.context.Donation.IsTransactionKeySubcriptionBased(value.Transaction.Key))
                    {
                        await this.SendInvoiceEmail(donationId);
                    }
                }
                else if (string.Equals(value.Method, "MB", StringComparison.OrdinalIgnoreCase))
                {
                    donationId = this.context.Donation.CompleteEasyPayPayment<MultiBankPayment>(
                        value.Id.ToString(),
                        value.Transaction.Key,
                        value.Transaction.Id.ToString(),
                        DateTime.Parse(value.Transaction.Date),
                        (float)value.Transaction.Values.Requested,
                        (float)value.Transaction.Values.Paid,
                        (float)value.Transaction.Values.FixedFee,
                        (float)value.Transaction.Values.VariableFee,
                        (float)value.Transaction.Values.Tax,
                        (float)value.Transaction.Values.Transfer);
                }

                this.HttpContext.Items.Add(KeyNames.DonationIdKey, donationId);

                return new JsonResult(new StatusDetails()
                {
                    Status = "ok",
                    Message = new Collection<string>() { $"Alimenteestaideia: Payment Completed for donation {donationId}" },
                })
                {
                    StatusCode = donationId == 0 ? (int)HttpStatusCode.NotFound : (int)HttpStatusCode.OK,
                };
            }
            else
            {
                return new JsonResult(new StatusDetails() { Status = "not found" }) { StatusCode = (int)HttpStatusCode.NotFound };
            }
        }
    }
}
