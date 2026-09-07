// -----------------------------------------------------------------------
// <copyright file="Details.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.FunctionExecutionReports
{
    using System;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports;
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Displays one immutable Azure Function execution report.
    /// </summary>
    public class DetailsModel : PageModel
    {
        private readonly FunctionExecutionReportQueryService queryService;

        /// <summary>
        /// Initializes a new instance of the <see cref="DetailsModel"/> class.
        /// </summary>
        /// <param name="queryService">Report query service.</param>
        /// <param name="localizer">Admin localizer.</param>
        public DetailsModel(
            FunctionExecutionReportQueryService queryService,
            IStringLocalizer<AdminSharedResources> localizer)
        {
            this.queryService = queryService;
            this.Localizer = localizer;
        }

        /// <summary>
        /// Gets the localizer.
        /// </summary>
        public IStringLocalizer<AdminSharedResources> Localizer { get; }

        /// <summary>
        /// Gets or sets the catalog function key.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string FunctionKey { get; set; }

        /// <summary>
        /// Gets or sets the execution identifier.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string ExecutionId { get; set; }

        /// <summary>
        /// Gets the validated function descriptor.
        /// </summary>
        public FunctionExecutionFunctionDescriptor Descriptor { get; private set; }

        /// <summary>
        /// Gets the storage result.
        /// </summary>
        public FunctionExecutionReportStorageResult Result { get; private set; }

        /// <summary>
        /// Gets the loaded report, if available.
        /// </summary>
        public FunctionExecutionReport Report => this.Result?.Report;

        /// <summary>
        /// Loads the requested report after validating both identifiers.
        /// </summary>
        /// <returns>The page or a not-found response for an out-of-scope identifier.</returns>
        public async Task<IActionResult> OnGetAsync()
        {
            this.Descriptor = FunctionExecutionReportCatalog.Find(this.FunctionKey);
            if (this.Descriptor == null
                || !FunctionExecutionReportQueryService.IsSafeExecutionId(this.ExecutionId))
            {
                return this.NotFound();
            }

            this.Result = await this.queryService.GetExecutionAsync(this.Descriptor, this.ExecutionId).ConfigureAwait(false);
            return this.Page();
        }

        /// <summary>
        /// Gets a localized label for a storage state.
        /// </summary>
        /// <param name="state">Storage state.</param>
        /// <returns>Localized state label.</returns>
        public string GetStateLabel(FunctionExecutionReportStorageState state)
        {
            return this.Localizer[$"FunctionExecutionReportState_{state}"].Value;
        }

        /// <summary>
        /// Gets a localized label for an outcome.
        /// </summary>
        /// <param name="outcome">Execution outcome.</param>
        /// <returns>Localized outcome label.</returns>
        public string GetOutcomeLabel(FunctionExecutionReportOutcome outcome)
        {
            return this.Localizer[$"FunctionExecutionReportOutcome_{outcome}"].Value;
        }

        /// <summary>
        /// Gets a localized label for an activity severity.
        /// </summary>
        /// <param name="severity">Activity severity.</param>
        /// <returns>Localized severity label.</returns>
        public string GetSeverityLabel(FunctionExecutionReportActivitySeverity severity)
        {
            return this.Localizer[$"FunctionExecutionReportSeverity_{severity}"].Value;
        }

        /// <summary>
        /// Gets a safe localized display value for optional report metadata.
        /// </summary>
        /// <param name="value">Metadata value.</param>
        /// <returns>The value or a localized empty-value label.</returns>
        public string GetMetadataValue(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? this.Localizer["FunctionExecutionReportsNotAvailable"].Value
                : value;
        }

        /// <summary>
        /// Gets a localized label for a Boolean report value.
        /// </summary>
        /// <param name="value">Boolean value.</param>
        /// <returns>Localized yes/no label.</returns>
        public string GetBooleanLabel(bool value)
        {
            return this.Localizer[value
                ? "FunctionExecutionReportsYes"
                : "FunctionExecutionReportsNo"].Value;
        }

        /// <summary>
        /// Formats a UTC timestamp with both UTC and local time, including the local offset.
        /// </summary>
        /// <param name="timestampUtc">Timestamp stored in the report.</param>
        /// <returns>A clear UTC/local representation.</returns>
        public string FormatTimestamp(DateTime timestampUtc)
        {
            DateTime utc = timestampUtc.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(timestampUtc, DateTimeKind.Utc)
                : timestampUtc.ToUniversalTime();
            DateTime local = utc.ToLocalTime();
            return $"{this.Localizer["FunctionExecutionReportsUtc"].Value}: {utc:yyyy-MM-dd HH:mm:ss} UTC / "
                + $"{this.Localizer["FunctionExecutionReportsLocal"].Value}: {local:yyyy-MM-dd HH:mm:ss zzz}";
        }
    }
}
