// -----------------------------------------------------------------------
// <copyright file="FunctionExecutionReportCoordinatorFactory.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports;
    using Microsoft.Extensions.Configuration;

    /// <summary>Creates execution report coordinators for host or tenant configuration.</summary>
    public sealed class FunctionExecutionReportCoordinatorFactory
    {
        private readonly IFunctionExecutionReportClock clock;
        private readonly IFunctionExecutionReportIdGenerator idGenerator;

        /// <summary>Initializes a new instance of the <see cref="FunctionExecutionReportCoordinatorFactory"/> class.</summary>
        /// <param name="clock">UTC clock.</param>
        /// <param name="idGenerator">Execution identifier generator.</param>
        public FunctionExecutionReportCoordinatorFactory(
            IFunctionExecutionReportClock clock = null,
            IFunctionExecutionReportIdGenerator idGenerator = null)
        {
            this.clock = clock ?? new SystemUtcClock();
            this.idGenerator = idGenerator ?? new GuidFunctionExecutionReportIdGenerator();
        }

        /// <summary>Creates a coordinator using the supplied configuration.</summary>
        /// <param name="configuration">Host or tenant configuration.</param>
        /// <returns>A best-effort coordinator.</returns>
        public IFunctionExecutionReportCoordinator Create(IConfiguration configuration)
        {
            FunctionExecutionReportOptions options;
            try
            {
                options = FunctionExecutionReportConfiguration.Create(configuration);
            }
            catch (Exception)
            {
                options = new FunctionExecutionReportOptions { Enabled = false };
            }

            if (!options.Enabled || string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                options.Enabled = false;
                return new FunctionExecutionReportCoordinator(
                    new DisabledFunctionExecutionReportStore(),
                    options,
                    this.clock,
                    this.idGenerator);
            }

            try
            {
                return new FunctionExecutionReportCoordinator(
                    new BlobFunctionExecutionReportStore(options.ConnectionString, options, this.clock),
                    options,
                    this.clock,
                    this.idGenerator);
            }
            catch (Exception)
            {
                options.Enabled = false;
                return new FunctionExecutionReportCoordinator(
                    new DisabledFunctionExecutionReportStore(),
                    options,
                    this.clock,
                    this.idGenerator);
            }
        }

        private sealed class DisabledFunctionExecutionReportStore : IFunctionExecutionReportStore
        {
            public Task WriteExecutionAsync(FunctionExecutionReport report, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task PublishLatestAsync(FunctionExecutionReportLatestPointer pointer, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }
    }
}
