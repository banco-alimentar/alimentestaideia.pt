// -----------------------------------------------------------------------
// <copyright file="IntegrationTestMailConfiguration.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.TestHost
{
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

    /// <summary>
    /// Registers tracked stub mail for integration tests.
    /// </summary>
    public static class IntegrationTestMailConfiguration
    {
        /// <summary>
        /// Replaces <see cref="IMail"/> with a scoped <see cref="StubMail"/> backed by a singleton tracker.
        /// </summary>
        /// <param name="services">Application services.</param>
        public static void AddTrackedStubMail(IServiceCollection services)
        {
            services.RemoveAll(typeof(IMail));
            services.RemoveAll(typeof(StubMailTracker));
            services.AddSingleton<StubMailTracker>();
            services.AddScoped<IMail, StubMail>();
        }
    }
}
