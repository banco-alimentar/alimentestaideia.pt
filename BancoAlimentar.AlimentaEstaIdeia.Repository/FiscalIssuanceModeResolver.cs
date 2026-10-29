// -----------------------------------------------------------------------
// <copyright file="FiscalIssuanceModeResolver.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Resolves the development-only simulation mode without accepting request, tenant or user input.
    /// </summary>
    public sealed class FiscalIssuanceModeResolver : IFiscalIssuanceModeResolver
    {
        private const string DevelopmentEnvironmentName = "Development";

        private readonly IOptions<FiscalSimulationOptions> options;
        private readonly IWebHostEnvironment webHostEnvironment;

        /// <summary>
        /// Initializes a new instance of the <see cref="FiscalIssuanceModeResolver"/> class.
        /// </summary>
        /// <param name="options">The process-local simulation configuration.</param>
        /// <param name="webHostEnvironment">The current web hosting environment.</param>
        public FiscalIssuanceModeResolver(
            IOptions<FiscalSimulationOptions> options,
            IWebHostEnvironment webHostEnvironment)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
        }

        /// <inheritdoc />
        public FiscalIssuanceMode Resolve()
        {
            if (!this.options.Value.Enabled)
            {
                return FiscalIssuanceMode.Real;
            }

            if (string.Equals(
                    this.webHostEnvironment.EnvironmentName,
                    DevelopmentEnvironmentName,
                    StringComparison.Ordinal))
            {
                return FiscalIssuanceMode.Simulation;
            }

            throw new InvalidOperationException(
                "Fiscal simulation is enabled, but it is permitted only in the exact Development environment. " +
                "Disable Fiscal:Simulation:Enabled before starting this host.");
        }
    }
}
