// -----------------------------------------------------------------------
// <copyright file="IntegrationTestEasyPayConfiguration.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.TestHost
{
    using System;
    using System.Collections.ObjectModel;
    using System.Net;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Services;
    using Easypay.Rest.Client.Api;
    using Easypay.Rest.Client.Client;
    using Easypay.Rest.Client.Model;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Moq;

    /// <summary>
    /// Registers a stub Easypay subscription checkout API for integration tests.
    /// </summary>
    public static class IntegrationTestEasyPayConfiguration
    {
        /// <summary>
        /// Replaces <see cref="EasyPayBuilder"/> with one that returns a stub subscription API.
        /// </summary>
        /// <param name="services">Application services.</param>
        /// <param name="checkoutUrl">Redirect URL returned by the stub checkout.</param>
        /// <param name="subscriptionId">Easypay subscription identifier returned by the stub.</param>
        public static void AddStubSubscriptionCheckout(
            IServiceCollection services,
            string checkoutUrl = "https://checkout.integration.test/subscription",
            string subscriptionId = null)
        {
            services.RemoveAll(typeof(EasyPayBuilder));
            services.AddScoped<EasyPayBuilder>(serviceProvider => CreateBuilderWithSubscriptionStub(
                serviceProvider,
                checkoutUrl,
                subscriptionId));
        }

        /// <summary>
        /// Replaces <see cref="EasyPayBuilder"/> with one that simulates a subscription API failure.
        /// </summary>
        /// <param name="services">Application services.</param>
        public static void AddStubSubscriptionCheckoutFailure(IServiceCollection services)
        {
            services.RemoveAll(typeof(EasyPayBuilder));
            services.AddScoped<EasyPayBuilder>(serviceProvider =>
            {
                var builder = new EasyPayBuilder(
                    serviceProvider.GetRequiredService<IConfiguration>(),
                    serviceProvider.GetRequiredService<IHttpContextAccessor>());
                var apiMock = new Mock<ISubscriptionPaymentApi>();
                apiMock
                    .Setup(api => api.SubscriptionPost(
                        It.IsAny<SubscriptionPostRequest>(),
                        It.IsAny<int>()))
                    .Throws(new ApiException(503, "Subscription provider unavailable"));
                builder.SetSubscriptionPaymentApiOverride(apiMock.Object);
                return builder;
            });
        }

        /// <summary>
        /// Replaces <see cref="EasyPayBuilder"/> with one that returns a stub single-payment API.
        /// </summary>
        /// <param name="services">Application services.</param>
        /// <param name="paymentId">Easypay payment identifier returned by the stub.</param>
        /// <param name="paymentMethodType">Easypay method type (cc, mb, mbw).</param>
        /// <param name="checkoutUrl">Credit-card checkout URL returned by the stub.</param>
        /// <param name="mbWayAlias">MBWay alias returned by the stub.</param>
        /// <param name="multibancoEntity">Multibanco entity returned by the stub.</param>
        /// <param name="multibancoReference">Multibanco reference returned by the stub.</param>
        /// <param name="singlePaymentLookupMethodStatus">Status returned by Easypay single-payment lookup.</param>
        /// <param name="singlePaymentLookupException">Exception thrown by Easypay single-payment lookup.</param>
        public static void AddStubSinglePaymentCheckout(
            IServiceCollection services,
            string paymentId = null,
            string paymentMethodType = "mbw",
            string checkoutUrl = "https://checkout.integration.test/credit-card",
            string mbWayAlias = "integration-mbway-alias",
            string multibancoEntity = "12345",
            string multibancoReference = "987654321",
            string singlePaymentLookupMethodStatus = "pending",
            ApiException singlePaymentLookupException = null)
        {
            services.RemoveAll(typeof(EasyPayBuilder));
            services.AddScoped<EasyPayBuilder>(serviceProvider =>
            {
                var builder = new EasyPayBuilder(
                    serviceProvider.GetRequiredService<IConfiguration>(),
                    serviceProvider.GetRequiredService<IHttpContextAccessor>());
                var easyPayPaymentId = paymentId ?? System.Guid.NewGuid().ToString();
                Method method = paymentMethodType switch
                {
                    "cc" => new Method(type: "cc", status: "pending", url: checkoutUrl),
                    "mb" => new Method(type: "mb", status: "pending", entity: multibancoEntity, reference: multibancoReference),
                    _ => new Method(type: "mbw", status: "pending", alias: mbWayAlias),
                };
                var apiMock = new Mock<ISinglePaymentApi>();
                apiMock
                    .Setup(api => api.SinglePostWithHttpInfoAsync(
                        It.IsAny<SinglePostRequest>(),
                        It.IsAny<int>(),
                        It.IsAny<System.Threading.CancellationToken>()))
                    .ReturnsAsync(new ApiResponse<InlineObject5>(
                        HttpStatusCode.Created,
                        new InlineObject5(
                            status: ResponseStatus.Ok,
                            message: new Collection<string> { "Your request was successfully created" },
                            id: easyPayPaymentId,
                            method: method,
                            customer: new InlineObject5Customer(id: System.Guid.NewGuid().ToString())),
                        string.Empty));
                apiMock
                    .Setup(api => api.SingleGetAsync(
                        It.IsAny<int?>(),
                        It.IsAny<int?>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<System.Guid?>(),
                        It.IsAny<string>(),
                        It.IsAny<double?>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<System.Threading.CancellationToken>()))
                    .ReturnsAsync(new InlineObject8(data: new Collection<Easypay.Rest.Client.Model.Single>()));
                var singlePaymentLookup = apiMock
                    .Setup(api => api.SingleIdGetAsync(
                        It.IsAny<System.Guid>(),
                        It.IsAny<int>(),
                        It.IsAny<System.Threading.CancellationToken>()));
                if (singlePaymentLookupException != null)
                {
                    singlePaymentLookup.ThrowsAsync(singlePaymentLookupException);
                }
                else
                {
                    singlePaymentLookup.ReturnsAsync(new InlineObject9(
                        method: new Method(type: paymentMethodType, status: singlePaymentLookupMethodStatus)));
                }

                builder.SetSinglePaymentApiOverride(apiMock.Object);
                return builder;
            });
        }

        /// <summary>
        /// Replaces the Easypay APIs with read-only subscription and payment detail stubs.
        /// </summary>
        /// <param name="services">Application services.</param>
        /// <param name="subscriptionId">Easypay subscription identifier.</param>
        /// <param name="paymentId">Easypay payment identifier.</param>
        public static void AddStubSubscriptionDetails(
            IServiceCollection services,
            string subscriptionId,
            string paymentId)
        {
            services.RemoveAll(typeof(EasyPayBuilder));
            services.AddScoped<EasyPayBuilder>(serviceProvider =>
            {
                var builder = new EasyPayBuilder(
                    serviceProvider.GetRequiredService<IConfiguration>(),
                    serviceProvider.GetRequiredService<IHttpContextAccessor>());
                string transactionKey = "subscription-provider-key";
                var subscriptionApi = new Mock<ISubscriptionPaymentApi>();
                subscriptionApi
                    .Setup(api => api.SubscriptionIdGetAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<int>(),
                        It.IsAny<System.Threading.CancellationToken>()))
                    .ReturnsAsync(new SubscriptionIdGet200Response(
                        id: Guid.Parse(subscriptionId),
                        key: transactionKey,
                        frequency: "1M",
                        currency: "EUR",
                        value: 5,
                        createdAt: "2026-01-01 10:00:00",
                        startTime: "2026-01-01 10:00:00",
                        expirationTime: "2027-01-01 10:00:00",
                        method: new SubscriptionIdGet200ResponseMethod(
                            type: "cc",
                            status: SubscriptionIdGet200ResponseMethod.StatusEnum.Active),
                        customer: new SubscriptionGet200ResponseDataInnerCustomer(
                            id: "customer-1",
                            name: "Integration Customer",
                            email: "customer@integration.test"),
                        transactions: new Collection<SubscriptionIdGet200ResponseTransactionsInner>
                        {
                            new SubscriptionIdGet200ResponseTransactionsInner(
                                id: paymentId,
                                key: transactionKey,
                                createdAt: "2026-01-02 10:00:00",
                                date: "2026-01-02 10:00:00",
                                values: new SubscriptionIdGet200ResponseTransactionsInnerValues(
                                    requested: 5,
                                    paid: 5,
                                    fixedFee: 0.09m,
                                    variableFee: 0.1m,
                                    transfer: 5),
                                method: "cc"),
                        }));

                var paymentApi = new Mock<ISinglePaymentApi>();
                var paymentSummary = new Easypay.Rest.Client.Model.Single(
                    id: paymentId,
                    key: transactionKey,
                    descriptive: "Integration payment",
                    customer: new Customer(id: "customer-1"),
                    method: new Method(type: "cc", status: "paid"),
                    value: 5,
                    createdAt: "2026-01-02 10:00:00",
                    paymentStatus: SinglePaymentStatus.Paid);
                paymentApi
                    .Setup(api => api.SingleGetWithHttpInfoAsync(
                        It.IsAny<int?>(),
                        It.IsAny<int?>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Guid?>(),
                        It.IsAny<string>(),
                        It.IsAny<double?>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<System.Threading.CancellationToken>()))
                    .ReturnsAsync(new ApiResponse<InlineObject8>(
                        HttpStatusCode.OK,
                        new InlineObject8(
                            meta: new ResponseMeta(new ResponseMetaPage(1, 1)),
                            data: new Collection<Easypay.Rest.Client.Model.Single> { paymentSummary }),
                        string.Empty));
                paymentApi
                    .Setup(api => api.SingleIdGetAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<int>(),
                        It.IsAny<System.Threading.CancellationToken>()))
                    .ThrowsAsync(new ApiException(404, "Subscription transactions are not standalone payments."));

                builder.SetSubscriptionPaymentApiOverride(subscriptionApi.Object);
                builder.SetSinglePaymentApiOverride(paymentApi.Object);
                return builder;
            });
        }

        private static EasyPayBuilder CreateBuilderWithSubscriptionStub(
            IServiceProvider serviceProvider,
            string checkoutUrl = "https://checkout.integration.test/subscription",
            string subscriptionId = null)
        {
            var builder = new EasyPayBuilder(
                serviceProvider.GetRequiredService<IConfiguration>(),
                serviceProvider.GetRequiredService<IHttpContextAccessor>());
            var easyPaySubscriptionId = subscriptionId ?? System.Guid.NewGuid().ToString();
            var subscriptionGuid = System.Guid.Parse(easyPaySubscriptionId);
            var apiMock = new Mock<ISubscriptionPaymentApi>();
            apiMock
                .Setup(api => api.SubscriptionPost(
                    It.IsAny<SubscriptionPostRequest>(),
                    It.IsAny<int>()))
                .Returns(new SubscriptionPost201Response(
                    status: "ok",
                    id: easyPaySubscriptionId,
                    method: new SubscriptionPost201ResponseMethod(type: "cc", status: "pending")));
            apiMock
                .Setup(api => api.SubscriptionIdGet(
                    It.IsAny<System.Guid>(),
                    It.IsAny<int>()))
                .Returns(new SubscriptionIdGet200Response(
                    id: subscriptionGuid,
                    method: new SubscriptionIdGet200ResponseMethod(type: "cc", url: checkoutUrl)));
            apiMock
                .Setup(api => api.SubscriptionIdDeleteWithHttpInfo(
                    It.IsAny<Guid>(),
                    It.IsAny<int>()))
                .Returns(new ApiResponse<object>(
                    HttpStatusCode.NoContent,
                    (object)null,
                    string.Empty));
            builder.SetSubscriptionPaymentApiOverride(apiMock.Object);
            return builder;
        }
    }
}
