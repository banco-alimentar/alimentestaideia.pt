// -----------------------------------------------------------------------
// <copyright file="SubscriptionReconciliationEndpointPolicyTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.Tests
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.EasyPay;
    using Xunit;

    /// <summary>
    /// Tests that reconciliation targets cannot cross provider environments.
    /// </summary>
    public sealed class SubscriptionReconciliationEndpointPolicyTests
    {
        [Theory]
        [InlineData("Developer", "https://api.test.easypay.pt")]
        [InlineData("Developer", "https://api.test.easypay.pt/")]
        [InlineData("Production", "https://api.prod.easypay.pt")]
        public void Validate_AcceptsTheMatchingHttpsEndpoint(string environment, string baseUrl)
        {
            SubscriptionReconciliationEndpointPolicy.Validate(environment, baseUrl);
        }

        [Theory]
        [InlineData("Developer", "https://api.prod.easypay.pt")]
        [InlineData("Production", "https://api.test.easypay.pt")]
        [InlineData("Developer", "http://api.test.easypay.pt")]
        [InlineData("Developer", "https://user:secret@api.test.easypay.pt")]
        [InlineData("Developer", "https://api.test.easypay.pt:444")]
        [InlineData("Developer", "https://api.test.easypay.pt/?key=secret")]
        [InlineData("Production", "https://api.prod.easypay.pt/2.0")]
        public void Validate_RejectsWrongSchemeHostOrPath(string environment, string baseUrl)
        {
            Assert.Throws<InvalidOperationException>(
                () => SubscriptionReconciliationEndpointPolicy.Validate(environment, baseUrl));
        }

        [Fact]
        public void Validate_RejectsUnknownEnvironment()
        {
            Assert.Throws<ArgumentException>(
                () => SubscriptionReconciliationEndpointPolicy.Validate("Staging", "https://api.test.easypay.pt"));
        }
    }
}
