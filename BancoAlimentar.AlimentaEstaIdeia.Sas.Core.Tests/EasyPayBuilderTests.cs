// -----------------------------------------------------------------------
// <copyright file="EasyPayBuilderTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Services;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Moq;
    using Xunit;

    /// <summary>
    /// Unit tests for <see cref="EasyPayBuilder"/>.
    /// </summary>
    public class EasyPayBuilderTests
    {
        /// <summary>
        /// Shared payment strategy uses tenant-wide Easypay credentials.
        /// </summary>
        [Fact]
        public void CanBuildApisForSharedPaymentProcessor()
        {
            var configuration = BuildConfiguration(new Dictionary<string, string>
            {
                { "Easypay:BaseUrl", "https://api.test.easypay.pt" },
                { "Easypay:AccountId", "shared-account" },
                { "Easypay:ApiKey", "shared-key" },
            });
            var httpContextAccessor = CreateHttpContextAccessor(PaymentStrategy.SharedPaymentProcessor);

            var builder = new EasyPayBuilder(configuration, httpContextAccessor);

            Assert.NotNull(builder.GetSinglePaymentApi());
            Assert.NotNull(builder.GetSubscriptionPaymentApi());
        }

        /// <summary>
        /// Per-food-bank strategy requires a food bank id in session.
        /// </summary>
        [Fact]
        public void ThrowsWhenFoodBankMissingFromSession()
        {
            var configuration = BuildConfiguration(new Dictionary<string, string>
            {
                { "Easypay:BaseUrl", "https://api.test.easypay.pt" },
            });
            var httpContextAccessor = CreateHttpContextAccessor(PaymentStrategy.IndividualPaymentProcessorPerFoodBank);

            Assert.Throws<InvalidOperationException>(() => new EasyPayBuilder(configuration, httpContextAccessor));
        }

        /// <summary>
        /// Per-food-bank strategy reads food-bank-specific Easypay credentials from configuration.
        /// </summary>
        [Fact]
        public void CanBuildApisForIndividualPaymentProcessorWithFoodBankInSession()
        {
            const int foodBankId = 2;
            var configuration = BuildConfiguration(new Dictionary<string, string>
            {
                { "Easypay:BaseUrl", "https://api.test.easypay.pt" },
                { $"Easypay:AccountId-{foodBankId}", "fb-account" },
                { $"Easypay:ApiKey-{foodBankId}", "fb-key" },
            });
            var httpContextAccessor = CreateHttpContextAccessor(
                PaymentStrategy.IndividualPaymentProcessorPerFoodBank,
                foodBankId);

            var builder = new EasyPayBuilder(configuration, httpContextAccessor);

            Assert.NotNull(builder.GetSinglePaymentApi());
            Assert.NotNull(builder.GetSubscriptionPaymentApi());
        }

        private static IConfiguration BuildConfiguration(Dictionary<string, string> values)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();
        }

        private static IHttpContextAccessor CreateHttpContextAccessor(
            PaymentStrategy paymentStrategy,
            int? foodBankId = null)
        {
            var context = new DefaultHttpContext();
            context.SetTenant(new Tenant
            {
                PaymentStrategy = paymentStrategy,
                InvoicingStrategy = InvoicingStrategy.SingleInvoiceTable,
                Name = "test-tenant",
                PublicId = Guid.NewGuid(),
            });

            if (foodBankId.HasValue)
            {
                var sessionMock = new Mock<ISession>();
                byte[] serializedId = BitConverter.GetBytes(foodBankId.Value);
                sessionMock
                    .Setup(s => s.TryGetValue(typeof(FoodBank).Name, out It.Ref<byte[]>.IsAny))
                    .Returns((string key, out byte[] value) =>
                    {
                        value = serializedId;
                        return true;
                    });
                context.Session = sessionMock.Object;
            }

            var mock = new Mock<IHttpContextAccessor>();
            mock.Setup(a => a.HttpContext).Returns(context);
            return mock.Object;
        }
    }
}
