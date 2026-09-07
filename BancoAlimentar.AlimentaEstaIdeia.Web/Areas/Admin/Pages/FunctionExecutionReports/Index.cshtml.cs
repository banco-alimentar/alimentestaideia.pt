// -----------------------------------------------------------------------
// <copyright file="Index.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services.FunctionExecutionReports;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Shows the latest execution report for every scheduled Azure Function.
    /// </summary>
    [ValidateAntiForgeryToken]
    public class IndexModel : PageModel
    {
        private readonly FunctionExecutionReportQueryService queryService;
        private readonly IFunctionExecutionTriggerClient triggerClient;
        private readonly IAuthorizationService authorizationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexModel"/> class.
        /// </summary>
        /// <param name="queryService">Report query service.</param>
        /// <param name="triggerClient">Manual execution trigger client.</param>
        /// <param name="authorizationService">Authorization service.</param>
        /// <param name="localizer">Admin localizer.</param>
        public IndexModel(
            FunctionExecutionReportQueryService queryService,
            IFunctionExecutionTriggerClient triggerClient,
            IAuthorizationService authorizationService,
            IStringLocalizer<AdminSharedResources> localizer)
        {
            this.queryService = queryService;
            this.triggerClient = triggerClient;
            this.authorizationService = authorizationService;
            this.Localizer = localizer;
        }

        /// <summary>
        /// Gets the page localizer.
        /// </summary>
        public IStringLocalizer<AdminSharedResources> Localizer { get; }

        /// <summary>
        /// Gets the report rows.
        /// </summary>
        public IReadOnlyList<FunctionExecutionReportRow> Rows { get; private set; } =
            new List<FunctionExecutionReportRow>();

        /// <summary>
        /// Gets or sets a success message from a completed queue request.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        /// Gets or sets an error message from a failed queue request.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Loads the report overview.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task OnGetAsync()
        {
            this.Rows = await this.queryService.GetOverviewAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Queues a manual execution request for one catalog function.
        /// </summary>
        /// <param name="functionKey">Stable catalog function key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Redirects to the report overview.</returns>
        public async Task<IActionResult> OnPostRunAsync(
            string functionKey,
            CancellationToken cancellationToken = default)
        {
            AuthorizationResult authorization = await this.authorizationService
                .AuthorizeAsync(this.User, null, "RoleArea")
                .ConfigureAwait(false);
            if (!authorization.Succeeded)
            {
                return this.Forbid();
            }

            if (FunctionExecutionReportCatalog.Find(functionKey) == null)
            {
                return this.BadRequest(this.Localizer["FunctionExecutionReportsInvalidFunction"].Value);
            }

            FunctionExecutionTriggerResult result = await this.triggerClient
                .TriggerAsync(functionKey, cancellationToken)
                .ConfigureAwait(false);
            if (result.IsSucceeded)
            {
                this.StatusMessage = string.IsNullOrWhiteSpace(result.Message)
                    ? this.Localizer["FunctionExecutionReportsQueued"].Value
                    : result.Message;
            }
            else
            {
                this.ErrorMessage = string.IsNullOrWhiteSpace(result.Message)
                    ? this.Localizer["FunctionExecutionReportsQueueFailed"].Value
                    : result.Message;
            }

            return this.RedirectToPage();
        }

        /// <summary>
        /// Gets a localized display name for a catalog function.
        /// </summary>
        /// <param name="descriptor">Function descriptor.</param>
        /// <returns>Localized function name.</returns>
        public string GetDisplayName(FunctionExecutionFunctionDescriptor descriptor)
        {
            return this.Localizer[descriptor.DisplayNameResourceKey].Value;
        }

        /// <summary>
        /// Gets a localized description for a catalog function.
        /// </summary>
        /// <param name="descriptor">Function descriptor.</param>
        /// <returns>Localized function description.</returns>
        public string GetDescription(FunctionExecutionFunctionDescriptor descriptor)
        {
            return this.Localizer[descriptor.DescriptionResourceKey].Value;
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
