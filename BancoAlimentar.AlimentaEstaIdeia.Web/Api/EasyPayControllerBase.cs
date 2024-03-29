﻿// -----------------------------------------------------------------------
// <copyright file="EasyPayControllerBase.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Api
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// This class represent the EasyPay base class used for the notification API.
    /// </summary>
    public class EasyPayControllerBase : ControllerBase
    {
        private readonly IUnitOfWork context;
        private readonly IConfiguration configuration;
        private readonly TelemetryClient telemetryClient;
        private readonly IMail mail;

        /// <summary>
        /// Initializes a new instance of the <see cref="EasyPayControllerBase"/> class.
        /// </summary>
        /// <param name="context">Unit of work.</param>
        /// <param name="configuration">Configuration.</param>
        /// <param name="telemetryClient">A reference to the <see cref="TelemetryClient"/>.</param>
        /// <param name="mail">Mail service.</param>
        public EasyPayControllerBase(
            IUnitOfWork context,
            IConfiguration configuration,
            TelemetryClient telemetryClient,
            IMail mail)
        {
            this.context = context;
            this.configuration = configuration;
            this.telemetryClient = telemetryClient;
            this.mail = mail;
        }

        /// <summary>
        /// Sends the invoice to the user.
        /// </summary>
        /// <param name="donationId">Donation id.</param>
        /// <param name="transactionKey">Transaction key.</param>
        /// <param name="paymentId">Payment id.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        protected async Task SendInvoiceEmail(int donationId, string transactionKey, int paymentId)
        {
            // send mail "Banco Alimentar: Confirmamos o pagamento da sua doação"
            // confirming that the multibank payment is processed.
            if (this.configuration.IsSendingEmailEnabled())
            {
                try
                {
                    if (donationId > 0)
                    {
                        Donation donation = this.context.Donation.GetFullDonationById(donationId);
                        if (donation == null)
                        {
                            EventTelemetry donationNotFound = new EventTelemetry("DonationNotFound");
                            donationNotFound.Properties.Add("DonationId", donationId.ToString());
                            donationNotFound.Properties.Add("Method", string.Concat(GetType().Name, ".", nameof(SendInvoiceEmail)));
                            this.telemetryClient.TrackEvent(donationNotFound);
                        }
                        else if (donation.PaymentStatus == PaymentStatus.Payed &&
                                donation.ConfirmedPayment != null)
                        {
                            if (!this.context.PaymentNotificationRepository.EmailNotificationExits(paymentId))
                            {
                                await this.mail.GenerateInvoiceAndSendByEmail(donation, Request, this.context, this.HttpContext.GetTenant());
                                this.telemetryClient.TrackEvent(
                                    "SendInvoiceEmail",
                                    new Dictionary<string, string>
                                    {
                                    { "DonationId", donationId.ToString() },
                                    { "PublicId", donation.PublicId.ToString() },
                                    { "ConfirmedPayment.Status", donation.ConfirmedPayment.Status },
                                    });
                                this.context.PaymentNotificationRepository.AddEmailNotification(donation.User, donation.ConfirmedPayment);
                            }
                            else
                            {
                                this.telemetryClient.TrackEvent(
                                    "EmailAlreadySent",
                                    new Dictionary<string, string>
                                    {
                                        { "DonationId", donationId.ToString() },
                                        { "PaymentId", donation.ConfirmedPayment.Id.ToString() },
                                    });
                            }
                        }
                        else
                        {
                            this.telemetryClient.TrackEvent(
                                "DonationWrongStatus",
                                new Dictionary<string, string>()
                                {
                                    { "DonationId", donationId.ToString() },
                                    { "DonationPaymentStatus", donation.PaymentStatus.ToString() },
                                    { "ConfirmedPayment.Status", donation.ConfirmedPayment?.Status },
                                });
                        }
                    }
                    else
                    {
                        EventTelemetry donationNotFound = new EventTelemetry("DonationNotFound");
                        donationNotFound.Properties.Add("DonationId", donationId.ToString());
                        donationNotFound.Properties.Add("Method", string.Concat(GetType().Name, ".", nameof(SendInvoiceEmail)));
                        this.telemetryClient.TrackEvent(donationNotFound);
                    }
                }
                catch (Exception exc)
                {
                    this.telemetryClient.TrackException(exc);
                }
            }
            else
            {
                this.telemetryClient.TrackEvent("EmailIsNotEanbled");
            }
        }
    }
}
