// -----------------------------------------------------------------------
// <copyright file="FunctionTestHelper.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function.Tests
{
    using System.Net.Http;
    using System.Reflection;
    using BancoAlimentar.AlimentaEstaIdeia.Function;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Helpers for invoking Azure Functions with test configuration.
    /// </summary>
    internal static class FunctionTestHelper
    {
        /// <summary>
        /// Assigns tenant configuration to a multi-tenant function instance.
        /// </summary>
        /// <param name="function">Function instance.</param>
        /// <param name="configuration">Configuration.</param>
        public static void SetConfiguration(MultiTenantFunction function, IConfiguration configuration)
        {
            FieldInfo field = typeof(MultiTenantFunction).GetField(
                "configuration",
                BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(function, configuration);
        }

        /// <summary>
        /// Replaces the HTTP client used by the multibanco reminder function.
        /// </summary>
        /// <param name="function">Function instance.</param>
        /// <param name="client">HTTP client.</param>
        public static void SetHttpClient(MultiBancoPaymentNotificationFunction function, HttpClient client)
        {
            FieldInfo field = typeof(MultiBancoPaymentNotificationFunction).GetField(
                "client",
                BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(function, client);
        }
    }
}
