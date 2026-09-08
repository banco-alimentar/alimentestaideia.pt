// -----------------------------------------------------------------------
// <copyright file="BlobFunctionExecutionReportStore.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports
{
    using System;
    using System.IO;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;

    /// <summary>Persists execution reports in a private Azure Blob container.</summary>
    public sealed class BlobFunctionExecutionReportStore : IFunctionExecutionReportStore, IFunctionExecutionReportReader
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };

        private readonly BlobContainerClient container;
        private readonly FunctionExecutionReportOptions options;
        private readonly IFunctionExecutionReportClock clock;
        private readonly string storageAccountName;

        /// <summary>Initializes the store from a connection string.</summary>
        public BlobFunctionExecutionReportStore(string connectionString, FunctionExecutionReportOptions options, IFunctionExecutionReportClock clock = null)
            : this(CreateServiceClient(connectionString), options, clock)
        {
        }

        /// <summary>Initializes the store with an existing client.</summary>
        public BlobFunctionExecutionReportStore(BlobServiceClient serviceClient, FunctionExecutionReportOptions options, IFunctionExecutionReportClock clock = null)
        {
            ArgumentNullException.ThrowIfNull(serviceClient);
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            FunctionExecutionReportOptions.Validate(this.options);
            this.clock = clock ?? new SystemUtcClock();
            this.container = serviceClient.GetBlobContainerClient(this.options.ContainerName);
            this.storageAccountName = serviceClient.AccountName;
        }

        /// <inheritdoc />
        public async Task WriteExecutionAsync(FunctionExecutionReport report, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(report);
            await this.container.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            string path = FunctionExecutionReportPaths.BuildExecutionPath(report.SlotKey, report.ScopeKey, report.FunctionKey, report.CompletedAtUtc, report.ExecutionId);
            BlobClient blob = this.container.GetBlobClient(path);
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(report, JsonOptions);
            using var stream = new MemoryStream(bytes, writable: false);
            await blob.UploadAsync(
                stream,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = "application/json; charset=utf-8" },
                    Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All },
                },
                cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task PublishLatestAsync(FunctionExecutionReportLatestPointer pointer, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(pointer);
            await this.container.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            string path = FunctionExecutionReportPaths.BuildLatestPathFromReportPath(pointer.ReportPath);
            BlobClient blob = this.container.GetBlobClient(path);
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(pointer, JsonOptions);

            for (int attempt = 0; attempt < 3; attempt++)
            {
                BlobProperties existingProperties = null;
                FunctionExecutionReportLatestPointer existing = null;
                try
                {
                    Response<BlobDownloadResult> response = await blob.DownloadContentAsync(cancellationToken).ConfigureAwait(false);
                    existing = JsonSerializer.Deserialize<FunctionExecutionReportLatestPointer>(response.Value.Content.ToString(), JsonOptions);
                    existingProperties = await blob.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                catch (RequestFailedException exception) when (exception.Status == 404)
                {
                }
                catch (JsonException)
                {
                    existing = null;
                }

                if (existing != null && FunctionExecutionReportLatestPointer.IsAtLeastAsNew(existing, pointer))
                {
                    return;
                }

                using var stream = new MemoryStream(bytes, writable: false);
                try
                {
                    var options = new BlobUploadOptions
                    {
                        HttpHeaders = new BlobHttpHeaders { ContentType = "application/json; charset=utf-8" },
                    };
                    if (existingProperties != null)
                    {
                        options.Conditions = new BlobRequestConditions { IfMatch = existingProperties.ETag };
                    }
                    else
                    {
                        options.Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All };
                    }

                    await blob.UploadAsync(stream, options, cancellationToken).ConfigureAwait(false);
                    return;
                }
                catch (RequestFailedException exception) when (exception.Status == 409 || exception.Status == 412)
                {
                    if (attempt == 2)
                    {
                        throw;
                    }
                }
            }
        }

        /// <inheritdoc />
        public async Task<FunctionExecutionReportStorageResult> GetLatestAsync(FunctionExecutionReportScope scope, string functionKey, CancellationToken cancellationToken = default)
        {
            string path = FunctionExecutionReportPaths.BuildLatestPath(scope.SlotKey, scope.ScopeKey, functionKey);
            FunctionExecutionReportStorageResult pointerResult = await ReadJsonAsync<FunctionExecutionReportLatestPointer>(path, cancellationToken).ConfigureAwait(false);
            if (pointerResult.State != FunctionExecutionReportStorageState.Available || pointerResult.LatestPointer == null)
            {
                return pointerResult;
            }

            FunctionExecutionReportStorageResult reportResult = await GetExecutionAsync(scope, functionKey, pointerResult.LatestPointer.ExecutionId, cancellationToken).ConfigureAwait(false);
            if (reportResult.State == FunctionExecutionReportStorageState.Available)
            {
                reportResult.LatestPointer = pointerResult.LatestPointer;
            }

            return reportResult;
        }

        /// <inheritdoc />
        public Task<FunctionExecutionReportStorageResult> GetExecutionAsync(FunctionExecutionReportScope scope, string functionKey, string executionId, CancellationToken cancellationToken = default)
        {
            return this.GetExecutionByIdAsync(scope, functionKey, executionId, cancellationToken);
        }

        private static BlobServiceClient CreateServiceClient(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Function execution report storage is not configured.");
            }

            return new BlobServiceClient(connectionString);
        }

        private async Task<FunctionExecutionReportStorageResult> GetExecutionByIdAsync(
            FunctionExecutionReportScope scope,
            string functionKey,
            string executionId,
            CancellationToken cancellationToken)
        {
            string prefix = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "v1/{0}/{1}/{2}/executions/",
                scope.SlotKey,
                scope.ScopeKey,
                FunctionExecutionReportPaths.NormalizeSegment(functionKey, nameof(functionKey)));
            string suffix = "/" + FunctionExecutionReportPaths.NormalizeSegment(executionId, nameof(executionId)) + ".json";
            try
            {
                await foreach (BlobItem item in this.container.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken))
                {
                    if (item.Name.EndsWith(suffix, StringComparison.Ordinal))
                    {
                        return await ReadJsonAsync<FunctionExecutionReport>(item.Name, cancellationToken).ConfigureAwait(false);
                    }
                }

                return new FunctionExecutionReportStorageResult { State = FunctionExecutionReportStorageState.Missing };
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return new FunctionExecutionReportStorageResult
                {
                    State = FunctionExecutionReportStorageState.Unavailable,
                    Diagnostic = "Execution report storage is unavailable.",
                    Exception = exception,
                };
            }
        }

        private async Task<FunctionExecutionReportStorageResult> ReadJsonAsync<T>(string path, CancellationToken cancellationToken)
        {
            BlobClient blob = this.container.GetBlobClient(path);
            try
            {
                Response<BlobDownloadResult> response = await blob.DownloadContentAsync(cancellationToken).ConfigureAwait(false);
                string content = response.Value.Content.ToString();
                T document = JsonSerializer.Deserialize<T>(content, JsonOptions);
                if (document == null)
                {
                    return new FunctionExecutionReportStorageResult
                    {
                        State = FunctionExecutionReportStorageState.Corrupt,
                        Diagnostic = "The execution report is empty.",
                    };
                }

                if (document is FunctionExecutionReport report
                    && report.CompletedAtUtc < this.clock.UtcNow.AddDays(-this.options.RetentionDays))
                {
                    return new FunctionExecutionReportStorageResult { State = FunctionExecutionReportStorageState.Expired };
                }

                var result = new FunctionExecutionReportStorageResult
                {
                    State = FunctionExecutionReportStorageState.Available,
                    ReportPersisted = true,
                    StorageAccountName = this.storageAccountName,
                    StorageContainerName = this.container.Name,
                    StorageBlobPath = path,
                    StorageBlobUri = blob.Uri.AbsoluteUri,
                };
                if (document is FunctionExecutionReport typedReport)
                {
                    result.Report = typedReport;
                }
                else if (document is FunctionExecutionReportLatestPointer pointer)
                {
                    result.LatestPointer = pointer;
                }

                return result;
            }
            catch (RequestFailedException exception) when (exception.Status == 404)
            {
                return new FunctionExecutionReportStorageResult { State = FunctionExecutionReportStorageState.Missing };
            }
            catch (JsonException exception)
            {
                return new FunctionExecutionReportStorageResult
                {
                    State = FunctionExecutionReportStorageState.Corrupt,
                    Diagnostic = "The execution report is not valid JSON.",
                    Exception = exception,
                };
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return new FunctionExecutionReportStorageResult
                {
                    State = FunctionExecutionReportStorageState.Unavailable,
                    Diagnostic = "Execution report storage is unavailable.",
                    Exception = exception,
                };
            }
        }
    }
}
