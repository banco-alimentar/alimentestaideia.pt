// -----------------------------------------------------------------------
// <copyright file="SubscriptionReconciliationReportTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.EasyPay;
    using Easypay.Rest.Client.Api;
    using Easypay.Rest.Client.Client;
    using Easypay.Rest.Client.Model;
    using Microsoft.Extensions.Configuration;
    using Moq;
    using Xunit;
    using Subscription = BancoAlimentar.AlimentaEstaIdeia.Model.Subscription;

    public sealed class SubscriptionReconciliationReportTests
    {
        [Theory]
        [InlineData(false, "Failed")]
        [InlineData(true, "Partial")]
        public void ProviderFailuresPreserveRowsAndIndependentLocalExpirationTotals(
            bool includeSuccess,
            string expectedStatus)
        {
            var subscriptions = new List<Subscription>
            {
                CreateSubscription(1, false),
                CreateSubscription(2, false),
                CreateSubscription(3, true),
            };
            subscriptions[0].EasyPaySubscriptionId = null;
            subscriptions[1].InitialDonation = null;
            var api = new Mock<ISubscriptionPaymentApi>();
            api.Setup(value => value.SubscriptionIdGet(It.IsAny<Guid>(), It.IsAny<int>()))
                .Throws(new ApiException(503, "Provider unavailable"));
            if (includeSuccess)
            {
                Subscription successful = CreateSubscription(4, false);
                subscriptions.Add(successful);
                api.Setup(value => value.SubscriptionIdGet(
                        Guid.Parse(successful.EasyPaySubscriptionId),
                        It.IsAny<int>()))
                    .Returns(new SubscriptionIdGet200Response(
                        id: Guid.Parse(successful.EasyPaySubscriptionId),
                        key: successful.TransactionKey,
                        value: 5));
            }

            IConfiguration configuration = new ConfigurationBuilder().Build();
            var tool = new SubscriptionReconciliationReportTool(configuration, (_, _) => api.Object);
            var report = CreateReport();

            tool.PopulateSubscriptionReport(report, subscriptions, Array.Empty<PaymentNotifications>(), configuration);

            Assert.Equal(expectedStatus, report.Status);
            Assert.Equal(subscriptions.Count, report.Subscriptions.Count);
            Assert.Equal(3, report.Summary.ProviderLookupFailures);
            Assert.Equal(includeSuccess ? 1 : 0, report.Summary.ProviderSubscriptionsRead);
            Assert.Equal(includeSuccess ? 3 : 2, report.Summary.NonExpiredSubscriptions);
            Assert.Equal(1, report.Summary.LocalExpiredSubscriptions);
            Assert.Equal(
                report.Summary.LocalSubscriptions,
                report.Summary.NonExpiredSubscriptions + report.Summary.LocalExpiredSubscriptions);
            Assert.Equal(
                new[] { "InvalidSubscriptionId", "MissingInitialDonation", "Failed" },
                report.Subscriptions.Take(3).Select(row => row.ProviderLookupStatus));
        }

        [Fact]
        public void MissingCredentialsFailTheAuditWithoutLosingLocalRows()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Tenant:PaymentStrategy"] = "SharedPaymentProcessor",
                })
                .Build();
            var tool = new SubscriptionReconciliationReportTool(configuration);
            var report = CreateReport();

            tool.PopulateSubscriptionReport(
                report,
                new[] { CreateSubscription(1, false) },
                Array.Empty<PaymentNotifications>(),
                configuration);

            Assert.Equal("Failed", report.Status);
            Assert.Equal("Failed", Assert.Single(report.Subscriptions).ProviderLookupStatus);
            Assert.Equal(1, report.Summary.ProviderLookupFailures);
            Assert.Equal(1, report.Summary.NonExpiredSubscriptions);
            Assert.Equal(0, report.Summary.ProviderSubscriptionsRead);
        }

        private static SubscriptionEnvironmentReport CreateReport()
        {
            return new SubscriptionEnvironmentReport
            {
                Summary = new SubscriptionEnvironmentSummary(),
                Subscriptions = new List<SubscriptionReconciliationRow>(),
            };
        }

        private static Subscription CreateSubscription(int id, bool expired)
        {
            return new Subscription
            {
                Id = id,
                EasyPaySubscriptionId = Guid.NewGuid().ToString(),
                TransactionKey = Guid.NewGuid().ToString(),
                ExpirationTime = DateTime.UtcNow.AddDays(expired ? -1 : 1),
                Status = SubscriptionStatus.Active,
                InitialDonation = new Donation { Id = id, DonationAmount = 5 },
            };
        }
    }
}
