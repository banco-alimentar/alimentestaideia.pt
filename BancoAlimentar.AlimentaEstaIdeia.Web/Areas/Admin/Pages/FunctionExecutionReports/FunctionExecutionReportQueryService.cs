// -----------------------------------------------------------------------
// <copyright file="FunctionExecutionReportQueryService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.FunctionExecutionReports
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.SiteHealth;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Reads function execution reports for the current tenant and deployment slot.
    /// </summary>
    public sealed class FunctionExecutionReportQueryService
    {
        private readonly IFunctionExecutionReportReader reader;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionExecutionReportQueryService"/> class.
        /// </summary>
        /// <param name="reader">Report reader.</param>
        /// <param name="httpContextAccessor">HTTP context accessor.</param>
        /// <param name="configuration">Tenant-resolved configuration.</param>
        public FunctionExecutionReportQueryService(
            IFunctionExecutionReportReader reader,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration)
        {
            this.reader = reader;
            this.httpContextAccessor = httpContextAccessor;
            this.configuration = configuration;
        }

        /// <summary>
        /// Validates an execution identifier before it reaches the storage path builder.
        /// </summary>
        /// <param name="executionId">Execution identifier.</param>
        /// <returns><see langword="true"/> when the identifier is a single safe segment.</returns>
        public static bool IsSafeExecutionId(string executionId)
        {
            if (string.IsNullOrWhiteSpace(executionId))
            {
                return false;
            }

            try
            {
                return string.Equals(
                    FunctionExecutionReportPaths.NormalizeSegment(executionId, nameof(executionId)),
                    executionId,
                    StringComparison.Ordinal);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /// <summary>
        /// Reads the latest report state for every catalog function.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Rows in catalog order.</returns>
        public async Task<IReadOnlyList<FunctionExecutionReportRow>> GetOverviewAsync(CancellationToken cancellationToken = default)
        {
            var rows = new List<FunctionExecutionReportRow>();
            foreach (FunctionExecutionFunctionDescriptor descriptor in FunctionExecutionReportCatalog.All)
            {
                FunctionExecutionReportStorageResult result = await this.GetLatestSafeAsync(descriptor, cancellationToken).ConfigureAwait(false);
                FunctionExecutionReportStorageResult infrastructureResult = descriptor.ScopeKind == FunctionExecutionReportScopeKind.Tenant
                    ? await this.GetLatestSafeAsync(
                        descriptor,
                        this.CreateGlobalScope(),
                        cancellationToken).ConfigureAwait(false)
                    : null;
                rows.Add(new FunctionExecutionReportRow(descriptor, result, infrastructureResult));
            }

            return rows;
        }

        /// <summary>
        /// Reads one exact report after validating the catalog key and scope.
        /// </summary>
        /// <param name="descriptor">Validated function descriptor.</param>
        /// <param name="executionId">Validated execution identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Report storage result.</returns>
        public Task<FunctionExecutionReportStorageResult> GetExecutionAsync(
            FunctionExecutionFunctionDescriptor descriptor,
            string executionId,
            CancellationToken cancellationToken = default)
        {
            return this.GetExecutionSafeAsync(descriptor, executionId, cancellationToken);
        }

        /// <summary>
        /// Creates the current slot and tenant/global scope for a catalog descriptor.
        /// </summary>
        /// <param name="descriptor">Function descriptor.</param>
        /// <returns>Safe report scope.</returns>
        public FunctionExecutionReportScope CreateScope(FunctionExecutionFunctionDescriptor descriptor)
        {
            ArgumentNullException.ThrowIfNull(descriptor);

            string slotKey = SiteHealthAppRoleResolver.ResolveCurrentSlot(
                SiteHealthReportConfiguration.ReadOptions(this.configuration)).SlotKey;
            string scopeKey = "global";
            if (descriptor.ScopeKind == FunctionExecutionReportScopeKind.Tenant)
            {
                Tenant tenant = this.httpContextAccessor.HttpContext?.GetTenant();
                scopeKey = tenant != null && !string.IsNullOrWhiteSpace(tenant.Name)
                    ? tenant.NormalizedName
                    : "global";
            }

            return new FunctionExecutionReportScope(slotKey, scopeKey);
        }

        /// <summary>
        /// Creates the global scope for infrastructure status metadata. This method never selects a tenant.
        /// </summary>
        /// <returns>Safe global report scope.</returns>
        public FunctionExecutionReportScope CreateGlobalScope()
        {
            string slotKey = SiteHealthAppRoleResolver.ResolveCurrentSlot(
                SiteHealthReportConfiguration.ReadOptions(this.configuration)).SlotKey;
            return new FunctionExecutionReportScope(slotKey, "global");
        }

        private static FunctionExecutionReportStorageResult UnavailableResult(Exception exception)
        {
            return new FunctionExecutionReportStorageResult
            {
                State = FunctionExecutionReportStorageState.Unavailable,
                Diagnostic = "Execution report storage is unavailable.",
                Exception = exception,
            };
        }

        private async Task<FunctionExecutionReportStorageResult> GetLatestSafeAsync(
            FunctionExecutionFunctionDescriptor descriptor,
            CancellationToken cancellationToken)
        {
            return await this.GetLatestSafeAsync(descriptor, this.CreateScope(descriptor), cancellationToken).ConfigureAwait(false);
        }

        private async Task<FunctionExecutionReportStorageResult> GetLatestSafeAsync(
            FunctionExecutionFunctionDescriptor descriptor,
            FunctionExecutionReportScope scope,
            CancellationToken cancellationToken)
        {
            try
            {
                return await this.reader.GetLatestAsync(
                    scope,
                    descriptor.FunctionKey,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                return UnavailableResult(exception);
            }
        }

        private async Task<FunctionExecutionReportStorageResult> GetExecutionSafeAsync(
            FunctionExecutionFunctionDescriptor descriptor,
            string executionId,
            CancellationToken cancellationToken)
        {
            try
            {
                return await this.reader.GetExecutionAsync(
                    this.CreateScope(descriptor),
                    descriptor.FunctionKey,
                    executionId,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                return UnavailableResult(exception);
            }
        }
    }
}
