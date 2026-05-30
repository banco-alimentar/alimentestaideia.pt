// -----------------------------------------------------------------------
// <copyright file="StubPayPalHttpClient.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace PayPalCheckoutSdk.Core
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using PayPalHttp;

    /// <summary>
    /// PayPal HTTP client that returns canned responses for integration tests.
    /// </summary>
    public class StubPayPalHttpClient : PayPalHttpClient
    {
        private readonly Func<PayPalHttp.HttpRequest, Task<PayPalHttp.HttpResponse>> executeHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="StubPayPalHttpClient"/> class.
        /// </summary>
        /// <param name="executeHandler">Handler invoked for each PayPal API request.</param>
        public StubPayPalHttpClient(Func<PayPalHttp.HttpRequest, Task<PayPalHttp.HttpResponse>> executeHandler)
            : base(new SandboxEnvironment("integration-stub-client-id", "integration-stub-secret"))
        {
            this.executeHandler = executeHandler ?? throw new ArgumentNullException(nameof(executeHandler));
        }

        /// <inheritdoc/>
        public override Task<PayPalHttp.HttpResponse> Execute<T>(T request)
        {
            return this.executeHandler(request);
        }
    }
}
