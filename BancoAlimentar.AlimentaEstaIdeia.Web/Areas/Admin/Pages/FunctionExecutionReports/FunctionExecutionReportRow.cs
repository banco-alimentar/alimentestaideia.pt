// -----------------------------------------------------------------------
// <copyright file="FunctionExecutionReportRow.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.FunctionExecutionReports
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports;

    /// <summary>
    /// A catalog function and the latest report state for the current scope.
    /// </summary>
    public sealed class FunctionExecutionReportRow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionExecutionReportRow"/> class.
        /// </summary>
        /// <param name="descriptor">Function descriptor.</param>
        /// <param name="result">Latest report result for the current tenant or global scope.</param>
        /// <param name="infrastructureResult">Latest global infrastructure result, when applicable.</param>
        public FunctionExecutionReportRow(
            FunctionExecutionFunctionDescriptor descriptor,
            FunctionExecutionReportStorageResult result,
            FunctionExecutionReportStorageResult infrastructureResult = null)
        {
            this.Descriptor = descriptor;
            this.Result = result;
            this.InfrastructureResult = infrastructureResult;
        }

        /// <summary>
        /// Gets the catalog descriptor.
        /// </summary>
        public FunctionExecutionFunctionDescriptor Descriptor { get; }

        /// <summary>
        /// Gets the storage result.
        /// </summary>
        public FunctionExecutionReportStorageResult Result { get; }

        /// <summary>
        /// Gets the global infrastructure result. It contains status metadata only in the overview;
        /// tenant report details are never loaded from this result.
        /// </summary>
        public FunctionExecutionReportStorageResult InfrastructureResult { get; }

        /// <summary>
        /// Gets the latest pointer, if one was found.
        /// </summary>
        public FunctionExecutionReportLatestPointer LatestPointer => this.Result?.LatestPointer;

        /// <summary>
        /// Gets a value indicating whether a report can be opened from this row.
        /// </summary>
        public bool HasReport => this.LatestPointer != null && !string.IsNullOrWhiteSpace(this.LatestPointer.ExecutionId);
    }
}
