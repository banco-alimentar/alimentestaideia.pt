// -----------------------------------------------------------------------
// <copyright file="EasyPayWebhookPayloadBuilder.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.TestHost
{
    using System;
    using System.Collections.ObjectModel;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Api.Model;
    using Easypay.Rest.Client.Model;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// Builds Easypay webhook JSON payloads for integration tests.
    /// </summary>
    public static class EasyPayWebhookPayloadBuilder
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters =
            {
                new Web.JsonConverter.GuidConverter(),
                new StringEnumConverter(new CamelCaseNamingStrategy()),
            },
        };

        /// <summary>
        /// Builds a credit-card payment completion notification payload.
        /// </summary>
        /// <param name="publicId">Donation public identifier (Easypay key).</param>
        /// <param name="transactionKey">Merchant transaction key.</param>
        /// <param name="easyPayId">Easypay payment id.</param>
        /// <param name="amount">Paid amount.</param>
        /// <returns>JSON body.</returns>
        public static string BuildCreditCardPaymentNotification(
            Guid publicId,
            string transactionKey,
            string easyPayId,
            double amount = 5.0)
        {
            return BuildPaymentNotification(publicId, transactionKey, "CC", amount);
        }

        /// <summary>
        /// Builds a multibanco payment completion notification payload.
        /// </summary>
        /// <param name="publicId">Donation public identifier (Easypay key).</param>
        /// <param name="transactionKey">Merchant transaction key.</param>
        /// <param name="easyPayId">Easypay payment id.</param>
        /// <param name="amount">Paid amount.</param>
        /// <returns>JSON body.</returns>
        public static string BuildMultiBankPaymentNotification(
            Guid publicId,
            string transactionKey,
            string easyPayId,
            double amount = 5.0)
        {
            return BuildPaymentNotification(publicId, transactionKey, "MB", amount);
        }

        /// <summary>
        /// Builds a generic multibanco status notification payload.
        /// </summary>
        /// <param name="easyPayId">Easypay payment id.</param>
        /// <param name="transactionKey">Merchant transaction key.</param>
        /// <returns>JSON body.</returns>
        public static string BuildGenericPaymentNotification(
            string easyPayId,
            string transactionKey)
        {
            var notification = new NotificationGeneric(
                Guid.Parse(easyPayId),
                transactionKey,
                NotificationGeneric.TypeEnum.Capture,
                NotificationGeneric.StatusEnum.Success,
                new Collection<string> { "integration-test" },
                DateTime.UtcNow.ToString("o"));

            return JsonConvert.SerializeObject(notification, SerializerSettings);
        }

        private static string BuildPaymentNotification(
            Guid publicId,
            string transactionKey,
            string method,
            double amount)
        {
            var request = new TransactionNotificationRequest(
                account: new AuthorisationNotificationRequestAuthorisation(Guid.NewGuid()),
                id: publicId,
                value: amount,
                currency: "EUR",
                key: publicId.ToString(),
                expirationTime: DateTime.UtcNow.AddDays(1).ToString("o"),
                customer: new Customer(
                    id: Guid.NewGuid().ToString(),
                    name: "Integration Test",
                    email: "webhook@integration.test"),
                method: method,
                transaction: new TransactionNotificationRequestTransaction(
                    id: Guid.NewGuid(),
                    key: transactionKey,
                    date: DateTime.UtcNow.ToString("o"),
                    values: new PaymentSingleTransactionValues(
                        requested: amount,
                        paid: amount,
                        fixedFee: 0,
                        variableFee: 0,
                        tax: 0,
                        transfer: amount),
                    documentNumber: "INT-TEST-1"));

            return JsonConvert.SerializeObject(request, SerializerSettings);
        }
    }
}
