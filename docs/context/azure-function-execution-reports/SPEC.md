# Product Specification: Azure Function Execution Reports

## 1. Overview

Create a consistent execution-reporting capability for every Azure Function in the platform and expose the most recent execution information through the Admin area.

At the end of each function execution, the system must write a report to Azure Storage. The report must record the main activities performed during the run and a final summary that makes the outcome clear, including partial failures. An authorized administrator must be able to open an Admin page that lists every existing function, its latest execution date and status, and a link to view the latest execution report.

The first release covers the five functions currently declared in `BancoAlimentar.AlimentaEstaIdeia.Function`:

- `GenerateDonationReportFunction`
- `GenerateSiteHealthReportFunction`
- `DeleteOldSubscriptionFunction`
- `MultiBancoPaymentNotificationFunction`
- `UpdateSubscriptions`

The list must remain data-driven or otherwise maintainable so that a newly added Azure Function cannot silently remain absent from the Admin overview.

## 2. Goals

- Make every function execution auditable without requiring direct access to Application Insights.
- Preserve a durable, human-readable record of what each run attempted, completed, skipped, or failed to do.
- Provide a single Admin entry point for checking whether scheduled functions are running.
- Make multi-tenant and partial execution outcomes explicit.
- Avoid exposing credentials, tokens, connection strings, or unnecessary personal data in stored reports or the Admin UI.

## 3. Non-goals

- Replacing Application Insights for diagnostics, alerting, or exception telemetry.
- Changing the business behavior or schedule of any existing function.
- Adding a manual “run function” capability to the Admin page.
- Changing the existing donation, subscription, site-health, or notification report formats.
- Reprocessing failed work automatically from the report page.
- Creating a database table or other permanent domain record solely for function execution history in the first release.

## 4. Existing behavior and context

The Functions project uses .NET 10 Azure Functions isolated worker. Four functions use the shared multi-tenant execution flow; `GenerateSiteHealthReportFunction` has its own execution flow. Timer functions are expected to run only in the production slot, while non-production slots are skipped by the existing slot guard.

Existing outputs and telemetry remain in place:

- Donation analytics are published as a public report under `/report/`.
- Site-health data is stored and displayed through the existing site-health reporting capability.
- Application Insights receives function events, traces, requests, and exceptions.
- Cleanup and notification functions currently expose their activity primarily through telemetry.
- `UpdateSubscriptions` is currently a no-op placeholder and must still produce an execution report that accurately says no subscription work was performed.

The execution report is an additional operational record, not a replacement for these outputs.

## 5. Users and permissions

The feature is for authenticated administrators who already have access to the Admin area. It must not be publicly accessible, linked from the public site, or accessible to ordinary donors.

The report viewer must enforce the same tenant and environment boundaries as the Admin area. Reports must not allow an administrator to infer or download secrets belonging to another tenant or environment.

## 6. Use cases

### UC-1: Verify that scheduled functions are running

An administrator opens the new Admin page and sees every existing Azure Function, its latest execution date/time, current outcome, and a link to the latest report. If a function has never completed, the page clearly says that no execution report is available.

### UC-2: Understand a successful execution

An administrator opens a report and sees when the function started and finished, which slot/environment executed it, the main activities performed, and a summary of the work completed.

### UC-3: Investigate a partial multi-tenant execution

An administrator opens a report for a multi-tenant function and sees which tenants succeeded, failed, or were skipped, together with counts and actionable error details that do not contain secrets.

### UC-4: Distinguish skipped, failed, and disabled runs

An administrator can tell whether a function was skipped because it ran in a non-production slot, disabled by configuration, failed before processing, or completed normally.

### UC-5: Review a maintenance function with no work

An administrator opens a report for a function that found no records to process, or for the current `UpdateSubscriptions` placeholder, and sees a successful execution with an explicit “no work performed” explanation rather than an empty report.

### UC-6: Open an older execution report

When a latest report is available, the administrator can open it from the overview page. The stored report must identify its execution and function clearly so that it remains understandable after later runs occur.

## 7. User journeys

### Journey A: Daily health check

1. The administrator navigates to the new Admin function-execution page.
2. The page lists all five current functions without requiring the administrator to know their Azure names.
3. The administrator scans the latest execution date and status.
4. The administrator opens the report for a function whose latest run is old, partial, or failed.
5. The report shows the final summary and directs the administrator to Application Insights for deeper diagnostics when appropriate.

### Journey B: Multi-tenant failure investigation

1. The administrator opens the latest report for a multi-tenant function.
2. The report shows total tenants discovered, succeeded, failed, and skipped.
3. The administrator expands or reads the activity entries for the affected tenant.
4. The administrator can identify whether the failure occurred during configuration loading, database access, external service access, or business processing.
5. Other tenant results remain visible even when one tenant failed.

### Journey C: Non-production execution check

1. The administrator opens a report generated for a non-production slot or a disabled function.
2. The report is marked `Skipped` or `Disabled`, not `Succeeded`.
3. The report explains the reason and confirms whether any tenant or business data was touched.

## 8. Functional requirements

### FR-1: Report every function execution

Every invocation of each existing Azure Function must produce one execution report when the function reaches its completion or failure handling path. This includes successful, no-work, skipped, disabled, partial, and failed outcomes.

Each report must have a unique execution identifier and include:

- Function name.
- Execution identifier.
- Trigger source or trigger type when available.
- Environment and deployment slot.
- Start time and completion time in UTC.
- Duration.
- Final outcome: `Succeeded`, `Partial`, `Failed`, `Skipped`, or `Disabled`.
- Whether any business data was changed.

### FR-2: Record main activities

The report must contain timestamped activity entries at an appropriate level of detail to explain the execution without becoming an unbounded debug log. Each entry should identify, when applicable:

- Activity name.
- Tenant or scope.
- Informational, warning, or error severity.
- Records discovered, processed, skipped, or changed.
- External operation attempted and its result, without credentials or sensitive payloads.
- A concise, human-readable message.

The report must not store passwords, API keys, access tokens, connection strings, secret values, full authorization headers, or payment card data.

### FR-3: Produce a final summary

Every report must end with a summary containing totals relevant to the function and the final outcome. At minimum, the summary must distinguish:

- Tenants discovered, processed, succeeded, failed, and skipped for multi-tenant functions.
- Activities attempted and completed.
- Warnings and errors.
- Records changed, when applicable.
- The reason for a skipped, disabled, partial, or failed outcome.

Function-specific metrics should be included when available, including donation-report publication counts, site-health report periods, deleted subscriptions, Multibanco notifications attempted/sent/failed, and subscription work performed. The `UpdateSubscriptions` report must explicitly identify that the current implementation performed no subscription synchronization.

### FR-4: Persist reports in Azure Storage

Reports must be stored in Azure Storage associated with the relevant application/tenant environment.

Storage behavior must meet these expectations:

- Each execution has a stable, unique path or identifier.
- A new execution does not overwrite the historical report for an earlier execution.
- The latest report can be resolved efficiently for the Admin overview.
- Reports are private and are not exposed through an unauthenticated public blob URL.
- The retention period is configurable and must retain at least 90 days by default.
- Storage failures are themselves recorded through existing operational telemetry and surfaced as a report/storage error where possible.

The exact serialization format may be chosen during architecture and implementation, but it must be readable by the Admin report viewer and suitable for future machine querying.

### FR-5: Add an Admin overview page

Add a localized Admin page for function execution reports. The page must list every registered/existing Azure Function and show:

- Display name and function name.
- Latest execution date/time, clearly identified as UTC or rendered in the current application locale with an unambiguous timezone.
- Latest execution status.
- Duration when available.
- Short outcome summary or warning indicator.
- Link to view the latest report when one exists.
- Clear “no report available” state when none exists.

The page must be usable on normal desktop and narrow screens and must not require direct Azure Portal access.

### FR-6: Add a report viewer

The report link must open a localized, authenticated Admin view of the selected execution report. The viewer must show the metadata, activities, final summary, warnings, errors, and any affected tenant/scope information in a readable order.

The viewer must preserve the execution status even when the report is partial or failed and must show a clear message when the report has expired, is missing, or cannot be read.

### FR-7: Keep existing telemetry and outputs

Adding execution reports must not remove or weaken existing Application Insights events, traces, exceptions, donation reports, site-health reports, reminder behavior, or cleanup behavior. The execution report should include correlation identifiers or links where available so an administrator can continue the investigation in Application Insights.

## 9. Failure and partial-execution behavior

- If one tenant fails in a multi-tenant function, processing must continue for other tenants where the existing function semantics allow it. The final report must be `Partial` and include per-tenant results.
- If all meaningful work fails, the final report must be `Failed`.
- If a function is skipped because of deployment-slot protection, the report must be `Skipped`, explain the slot reason, and confirm that tenant business work was not started.
- If a function is disabled by configuration, the report must be `Disabled` and include the configuration-controlled reason without exposing configuration secrets.
- If no records require processing, the report must be `Succeeded` with a no-work summary.
- If an exception occurs, the report must include a safe exception summary and correlation identifier; full stack traces remain in Application Insights.
- Report finalization must happen even when business work fails, using a best-effort finalization path.
- If Azure Storage is unavailable, the function must not hide the original business failure. It must emit an Application Insights exception/trace stating that the execution report could not be persisted. The Admin page must show the absence or unavailable state rather than displaying stale data as current.
- Report-writing failures must not cause a successful business operation to be incorrectly reported as a business failure unless the product explicitly chooses a fail-closed policy during architecture review.
- Reports must not trigger a retry of payments, subscriptions, notifications, deletions, or other business operations.

## 10. Localization and accessibility

- Admin page labels, statuses, empty states, errors, and report headings must use the application’s existing localization system.
- At minimum, Portuguese must be supported consistently with the current Admin experience; existing supported Admin languages should receive equivalent translations.
- Status must not be conveyed by color alone. Use text and accessible labels in addition to any success/warning/error colors.
- Tables and report sections must be keyboard accessible and provide meaningful headings and link text.

## 11. Non-functional requirements

### Security and privacy

- Only authorized Admin users may list or view execution reports.
- Reports must be scoped to the current tenant/environment and must not expose another tenant’s report.
- Secrets and sensitive credentials must never be written to report storage, rendered in the UI, or included in links.
- Report links must remain protected after copying or bookmarking.

### Reliability and durability

- Report creation must be attempted for every execution path.
- Historical reports must remain available for the configured retention period.
- A single corrupt or missing report must not prevent the overview page from displaying other functions.

### Performance

- The overview page should load without downloading full historical reports for every function.
- Opening one report should load only the selected execution report and should remain responsive for a normal multi-tenant execution.
- Report generation must avoid materially delaying existing function work or causing duplicate external calls.

### Operability

- Reports must contain enough context to correlate with Application Insights using function name, execution identifier, timestamps, slot, and tenant where applicable.
- Storage and report-read failures must be observable in Application Insights.
- The feature must work for scheduled executions and local/test execution paths where the repository’s test infrastructure supports them.

## 12. Acceptance criteria

1. The five current Azure Functions each produce one stored execution report for a successful execution.
2. A report is produced, or a clearly observable persistence failure is recorded, for skipped, disabled, no-work, partial, and exception paths.
3. Each report contains function name, unique execution ID, slot/environment, UTC start/end timestamps, duration, status, activity entries, and an end summary.
4. Multi-tenant reports show tenant-level success, failure, and skipped results and do not hide successful tenant results when another tenant fails.
5. Reports contain no secrets, credentials, access tokens, payment card details, or full sensitive request payloads.
6. Reports are stored as historical executions without overwriting earlier reports, are private, and follow the configured retention policy of at least 90 days by default.
7. The Admin overview lists all five current functions, even when a function has no report yet.
8. The overview shows the latest execution date/time, status, duration where available, a concise result, and a working link to the latest report when available.
9. The report viewer is protected by Admin authorization, is localized, and clearly handles missing, expired, corrupt, or unavailable reports.
10. The overview and report viewer preserve the existing tenant/environment access boundaries.
11. Existing Application Insights telemetry and existing business outputs continue to work.
12. Existing functions do not make duplicate payment, subscription, notification, or deletion calls because of report generation.
13. Automated tests cover successful reporting, no-work reporting, skipped/disabled reporting, partial multi-tenant reporting, storage failure handling, Admin authorization, overview rendering, and report-link/viewer behavior.
14. Documentation identifies the five functions, report storage/retention behavior, Admin page location, report statuses, and the relationship between execution reports and Application Insights.

## 13. Product decisions to preserve during design

- A report is an operational audit record, not a replacement for Application Insights.
- The final summary is mandatory even when there is no business work to perform.
- Partial execution must remain visible; it must not be flattened into a generic success.
- The current `UpdateSubscriptions` no-op behavior must be reported accurately rather than implying that subscriptions were synchronized.
- Report access must be secure and application-mediated rather than relying on public storage URLs.
