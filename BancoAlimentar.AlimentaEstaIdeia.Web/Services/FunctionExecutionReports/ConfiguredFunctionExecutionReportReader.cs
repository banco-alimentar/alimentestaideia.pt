// -----------------------------------------------------------------------
// <copyright file="ConfiguredFunctionExecutionReportReader.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services.FunctionExecutionReports
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Storage.Blobs;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports;

    /// <summary>
    /// Creates a tenant-scoped blob reader and converts construction failures into a safe unavailable state.
    /// </summary>
    public sealed class ConfiguredFunctionExecutionReportReader : IFunctionExecutionReportReader
    {
        private readonly IFunctionExecutionReportReader innerReader;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfiguredFunctionExecutionReportReader"/> class.
        /// </summary>
        /// <param name="options">Report options.</param>
        public ConfiguredFunctionExecutionReportReader(FunctionExecutionReportOptions options)
        {
            if (options == null || !options.Enabled || string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                this.innerReader = new UnavailableFunctionExecutionReportReader();
                return;
            }

            try
            {
                this.innerReader = new BlobFunctionExecutionReportStore(
                    new BlobServiceClient(options.ConnectionString),
                    options);
            }
            catch (Exception exception)
            {
                this.innerReader = new FailedConstructionReader(exception);
            }
        }

        /// <inheritdoc />
        public Task<FunctionExecutionReportStorageResult> GetLatestAsync(
            FunctionExecutionReportScope scope,
            string functionKey,
            CancellationToken cancellationToken = default)
        {
            return this.innerReader.GetLatestAsync(scope, functionKey, cancellationToken);
        }

        /// <inheritdoc />
        public Task<FunctionExecutionReportStorageResult> GetExecutionAsync(
            FunctionExecutionReportScope scope,
            string functionKey,
            string executionId,
            CancellationToken cancellationToken = default)
        {
            return this.innerReader.GetExecutionAsync(scope, functionKey, executionId, cancellationToken);
        }

        private sealed class FailedConstructionReader : IFunctionExecutionReportReader
        {
            private readonly Exception exception;

            public FailedConstructionReader(Exception exception)
            {
                this.exception = exception;
            }

            public Task<FunctionExecutionReportStorageResult> GetLatestAsync(
                FunctionExecutionReportScope scope,
                string functionKey,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(this.CreateResult());
            }

            public Task<FunctionExecutionReportStorageResult> GetExecutionAsync(
                FunctionExecutionReportScope scope,
                string functionKey,
                string executionId,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(this.CreateResult());
            }

            private FunctionExecutionReportStorageResult CreateResult()
            {
                return new FunctionExecutionReportStorageResult
                {
                    State = FunctionExecutionReportStorageState.Unavailable,
                    Diagnostic = "Execution report storage is unavailable.",
                    Exception = this.exception,
                };
            }
        }
    }
}
