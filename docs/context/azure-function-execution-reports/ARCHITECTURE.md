# Architecture: Azure Function Execution Reports

## 1. Architectural decisions

### 1.1 Shared operational-reporting component

Execution reporting is an operational concern shared by the Azure Functions host and the web Admin area. The contracts and storage implementation belong in the existing `BancoAlimentar.AlimentaEstaIdeia.Repository` project, which is already referenced by both applications and already owns the Blob Storage reporting implementations.

The component must be independent of ASP.NET Core, Razor Pages, Entity Framework entities, and Azure Functions trigger types. The Functions project adapts timer execution to the component; the Web project adapts authenticated Admin requests to its read API.

The shared component should provide:

- Immutable report contracts and enums for function name, scope, outcome, severity, activity, summary, and latest-execution metadata.
- A bounded in-memory report builder/writer for recording activities and counters.
- An execution coordinator that creates the execution ID, records start/completion, calculates duration, and finalizes the outcome.
- A Blob-backed store with separate write and read interfaces.
- Configuration/options and path helpers.
- A catalog of functions displayed by the Admin page.

The write path must not depend on the Admin application. The read path must not depend on the Functions worker runtime.

### 1.2 One logical report per execution scope

The top-level invocation receives one execution ID. Multi-tenant functions create one report for each tenant scope using that invocation ID, because the Admin area must never expose another tenant’s data. The reports are logically part of the same invocation and include the same invocation ID plus the tenant scope.

For a non-tenant execution, such as the standalone site-health function, the report uses the `global` scope. A non-production slot skip is also recorded at the global scope because tenant processing has not started.

The Admin page resolves the latest report for the current tenant and deployment slot. It must not aggregate or display reports from other tenant scopes.

### 1.3 Reports complement existing telemetry and artifacts

Execution reports are durable, human-readable operational records. They do not replace Application Insights, the donation analytics report, or the site-health report. Existing telemetry and business outputs remain in place.

Reports should include safe correlation metadata such as function name, execution ID, invocation ID, slot, tenant scope, and timestamps. They must not include full exception stack traces; those remain in Application Insights.

## 2. Contracts and service boundaries

The implementation should use explicit interfaces so business code can be tested without Azure Storage:

### 2.1 Report contracts

The report schema should be versioned from its first release. It should contain at least:

- `SchemaVersion`.
- `FunctionName` and a stable catalog key.
- `ExecutionId` and `InvocationId`.
- `Environment`/deployment slot and scope key.
- `StartedAtUtc`, `CompletedAtUtc`, and duration.
- `Outcome`: `Succeeded`, `Partial`, `Failed`, `Skipped`, or `Disabled`.
- `BusinessDataChanged`.
- A bounded ordered list of activity entries.
- A final summary with common counters and function-specific metrics.
- Safe error and warning summaries, including correlation IDs where available.

Use machine-readable enum values in storage and localize their display labels in the Admin UI. Do not store localized status values as the canonical report state.

### 2.2 Writer/coordinator

The writer/coordinator should expose operations equivalent to:

- Begin an execution with a stable ID and target scope.
- Record an activity with severity, activity key, optional tenant/scope, safe message, and counters.
- Increment or set summary metrics.
- Mark a tenant result as succeeded, failed, or skipped.
- Complete with an explicit outcome and final summary.
- Finalize/persist the immutable report and latest pointer using best effort.

The writer must cap activity count and message length. A report is an audit summary, not a debug log. When limits are reached, add a single truncation warning and retain the final summary.

The writer must not accept raw secrets or arbitrary payloads. Callers should pass identifiers and counts, not request URLs containing credentials, authorization headers, connection strings, API keys, email addresses, or payment/card details.

### 2.3 Storage interfaces

Separate the storage abstraction into write and read responsibilities:

- `IFunctionExecutionReportStore` for immutable report persistence and latest-pointer publication.
- `IFunctionExecutionReportReader` for latest lookup and exact execution lookup.

The Admin query layer should consume the reader, not `BlobServiceClient` directly. The Functions code should consume the writer/coordinator, not construct Blob clients in each function.

Storage exceptions must be translated into a result/diagnostic that preserves the original business exception. The storage abstraction must not cause a successful business operation to be reported as a business failure solely because the operational report could not be written.

### 2.4 Time and ID seams

Inject a UTC clock and execution-ID generator through the coordinator or its factory. Tests must be able to produce deterministic timestamps and IDs without changing production behavior.

## 3. Azure Blob Storage design

### 3.1 Configuration

Use a dedicated `FunctionExecutionReports` configuration section:

- `ConnectionString`: preferred storage connection for execution reports.
- `ContainerName`: default `function-execution-reports`.
- `RetentionDays`: default `90`, with a minimum enforced by configuration validation.
- Optional `Enabled` flag for local/test control only.

If the dedicated connection is absent, the existing tenant `AzureStorage:ConnectionString` may be used as the compatibility fallback. The fallback must be resolved from the current tenant configuration for multi-tenant execution and from host configuration for global execution. Configuration values themselves must never be written into reports or telemetry.

The container must remain private. Do not use public blob access, SAS URLs, or direct blob URLs in the Admin page.

### 3.2 Path layout

Use stable, normalized path segments and keep tenant and slot boundaries explicit:

```text
v1/{slotKey}/{scopeKey}/{functionKey}/executions/{yyyy}/{MM}/{dd}/{executionId}.json
v1/{slotKey}/{scopeKey}/{functionKey}/latest.json
```

`slotKey` is `production` when `WEBSITE_SLOT_NAME` is empty, otherwise the normalized slot name. `scopeKey` is the normalized tenant name for tenant reports or `global` for global reports. `functionKey` is the stable catalog key, not a display label.

The immutable execution blob is uploaded first. `latest.json` is a small pointer containing the execution ID, completed timestamp, outcome, duration, and report path. The pointer is updated only after the immutable report is successfully written.

The store must prevent an older, slow execution from replacing a newer latest pointer. Use a compare-and-update operation based on the existing pointer’s completion timestamp/ETag, or re-read and conditionally replace the pointer.

### 3.3 Retention and discovery

Historical execution blobs are never overwritten. Latest discovery must read one deterministic pointer blob and must not list or download the complete execution history for the overview page.

Retention is an Azure Storage lifecycle concern where possible: apply a lifecycle policy to the execution path using the configured retention period. If deployment configuration cannot guarantee a lifecycle policy, the store may perform bounded best-effort pruning after a successful write, but pruning must never block or repeat business work. A pruning failure is telemetry only.

The latest pointer is not itself a report. If it points to a missing or corrupt execution blob, the Admin UI must show an unavailable/corrupt state rather than silently presenting an older report as current.

## 4. Function integration

### 4.1 `MultiTenantFunction`

`MultiTenantFunction.RunFunctionCore` is the shared integration point for:

- `DeleteOldSubscriptionFunction`.
- `GenerateDonationReportFunction`.
- `MultiBancoPaymentNotificationFunction`.
- `UpdateSubscriptions`.

The outer timer entry point starts the invocation context before slot/configuration work. The shared flow must:

1. Start a global invocation ID.
2. Record trigger and slot metadata.
3. Record a `Skipped` report before returning when the slot guard blocks execution.
4. Discover tenants and record the count.
5. Create a tenant-scoped report for each tenant before loading tenant configuration.
6. Record configuration loading, database setup, business activity, and tenant completion.
7. Continue according to the existing per-tenant failure semantics.
8. Mark the tenant result and persist its report in a `finally` path.
9. Aggregate safe counts in the invocation telemetry without exposing tenant data across Admin boundaries.

The existing `ExecuteFunction` delegate currently has no report parameter. The integration should expose a report context through an explicit invocation/tenant context or a scoped reporter rather than a static mutable property. Existing functions should log their main activities through that context.

The current outer catches that record Application Insights exceptions must remain. They should additionally complete the report with `Failed` or `Partial` and preserve the current exception-swallowing/rethrow behavior unless a separate product decision changes it.

### 4.2 Function-specific activity boundaries

The report records summaries at meaningful boundaries, not every low-level operation:

- `DeleteOldSubscriptionFunction`: candidates found, active-sibling decisions, donations/subscriptions deleted, transaction outcome, and failures.
- `GenerateDonationReportFunction`: enabled/skipped state, report generation result, pages uploaded, paid count/amount, and publication outcome.
- `MultiBancoPaymentNotificationFunction`: pending records found, eligible records, reminder attempts, successful/failed responses, and records without a user. Never record the API certificate or a URL containing it.
- `UpdateSubscriptions`: transaction lifecycle and an explicit no-op summary stating that no subscription synchronization was performed.

### 4.3 Standalone site-health function

`GenerateSiteHealthReportFunction` does not use `MultiTenantFunction` and must be wrapped directly with the same coordinator. Its existing slot guard, enabled check, site-health report generation, `SiteHealthReportPublished` event, and exception rethrow behavior remain intact.

The execution report must distinguish:

- `Skipped` for a non-production slot.
- `Disabled` when `SiteHealthReport:Enabled` is false.
- `Succeeded` only after the site-health artifact is stored.
- `Failed` when generation or storage fails.

Do not merge the function execution report with the existing site-health JSON or generation-status blobs. They have different schemas and consumers.

### 4.4 Host-disabled functions

Azure Functions disabled through `AzureWebJobs.<FunctionName>.Disabled` never enter user code and therefore cannot write a report. The Admin overview must represent this as “no report available” unless an external deployment/monitoring source is added. In-process disabled outcomes are supported by the report schema; host-level disabling is an operational limitation that must be documented and must not be falsely reported as a completed execution.

## 5. Admin read architecture

### 5.1 Pages and query service

Add a dedicated Admin page and report viewer under the existing `Areas/Admin/Pages` tree. The page model must call a tenant-aware query service, not Azure SDKs directly.

The query service should:

- Read the function catalog to produce the complete list, including functions with no report.
- Resolve one latest pointer per function for the current slot and tenant scope.
- Fetch a single report only when the viewer is opened.
- Return typed unavailable states for no report, expired/missing blob, corrupt JSON, and storage access failure.
- Avoid throwing one function’s storage failure through the entire overview page.

The report viewer must accept only a catalog function key and a validated execution ID. The store constructs the blob path; user input must never be treated as a raw blob path.

### 5.2 Authorization and tenant isolation

The new page and viewer use the existing `AdminArea` policy through the Admin area conventions. They should not be added to the `RoleArea` super-admin-only list unless the product requirements change.

Tenant isolation is enforced in two layers:

1. The current `TenantConfigurationRoot` resolves the storage configuration for the current request.
2. The query service derives the current normalized tenant and slot scope and passes those values to the path builder.

The page must not accept tenant or storage-container overrides through query-string parameters. A copied report link remains scoped to the current tenant and environment.

### 5.3 Overview semantics

Use the catalog’s display metadata for function names, descriptions, schedules, and localization keys. Use the latest pointer for the date, status, duration, and summary preview. Render timestamps as UTC or with an explicit localized timezone indicator; the stored canonical value remains UTC.

Status must be conveyed by text and accessible labels, not by color alone. Missing and unavailable reports must be distinct from a stale successful report.

## 6. Function catalog

Define a shared stable catalog with the five current function keys and localization keys. The catalog is metadata only and must not become a second source of timer behavior.

To prevent silent omissions:

- The Admin page is generated from the catalog, not a hand-maintained Razor table.
- A Functions test reflects over Azure Functions `FunctionAttribute` declarations and asserts that every timer function is registered in the catalog.
- The catalog test also asserts that each display/localization key exists in the supported Admin resource sets or has a documented fallback.

When a new timer function is added, the build/test guard fails until its catalog metadata and execution-report integration are added.

## 7. Dependency injection and configuration boundaries

### Azure Functions

Register the shared report store/coordinator in `BancoAlimentar.AlimentaEstaIdeia.Function/Program.cs`. The storage client/store may be singleton because it is stateless; per-execution target configuration and scope are passed to operations. Do not capture a tenant configuration in a singleton.

`MultiTenantFunction` receives the coordinator through DI or a factory. Test construction must be able to provide an in-memory/fake store and deterministic clock. The standalone site-health function receives the same abstraction directly.

### Web application

Register the reader/store and the Admin query service in `Startup.ConfigureServices`. The reader must resolve the scoped tenant configuration at request time. Do not inject the scoped `TenantConfigurationRoot` into a singleton.

Reuse the existing `SiteHealthReportBlobStore` patterns for JSON serialization, private Blob access, and `AzureStorage:ConnectionString` fallback, but keep the execution-report schema and paths separate.

## 8. Localization

Use the existing `AdminSharedResources` localizer and `Resources/AdminSharedResources*.resx` files for:

- Page title, headings, descriptions, and column labels.
- Function display names and descriptions.
- Outcome/status labels.
- No-report, expired, corrupt, and storage-unavailable messages.
- Report section headings and accessibility text.

At minimum update Portuguese, English, Spanish, and French resources consistently with the existing application support. Stored activity messages remain neutral operational text; the viewer localizes structural labels and status values.

## 9. Failure handling and observability

Report persistence is best effort and must be isolated from business semantics:

- Business exceptions are captured first and remain the authoritative function failure.
- The coordinator attempts finalization in `finally`.
- A storage failure emits Application Insights exception/trace telemetry with function name, execution ID, slot, and scope, but never with a connection string or secret.
- If business work succeeded and report persistence failed, the function remains a business success; telemetry and the Admin unavailable state expose the reporting problem.
- If business work failed and report persistence also failed, the original business exception remains the primary diagnostic.

Continue emitting existing Application Insights events and traces. Add report-specific events such as report persisted, report persistence failed, and latest-pointer update failed, with low-cardinality properties. Do not depend on telemetry delivery to make the Blob report durable.

## 10. Test seams and coverage expectations

### Unit tests

Cover the shared component with fakes for storage, clock, ID generation, and configuration:

- Report schema serialization/deserialization and schema version.
- Activity truncation and secret-safe message handling.
- Outcome calculation for success, partial, failed, skipped, disabled, and no-work executions.
- Latest-pointer monotonic update when executions complete out of order.
- Storage failure preserving business outcome and telemetry diagnostic.
- Path normalization and tenant/slot isolation.
- Retention option validation.
- Catalog completeness and localization-key coverage.

### Function tests

Extend the existing `BancoAlimentar.AlimentaEstaIdeia.Function.Tests` seams rather than requiring live Azure Storage. Provide a fake execution-report store through the existing test service provider or function helper. Assert activity/summary data for each function’s main path, no-work path, slot skip, partial tenant result, and exception path.

For `GenerateSiteHealthReportFunction`, fake the site-health service or its storage boundary and assert that disabled, skipped, successful, and failed paths complete the execution report without changing existing site-health telemetry behavior.

### Web/Admin integration tests

Use the existing `CustomWebApplicationFactory`, seeded Admin role, and test localization setup. Replace the report reader/store with a fake containing:

- No reports.
- A latest report for every function.
- Missing/corrupt report data.
- A storage exception for one function.
- A report from another tenant/slot that must not be returned.

Assert Admin authorization, catalog rendering, latest-link generation, viewer access, unavailable states, localized labels, and that one storage failure does not suppress the remaining overview rows.

## 11. Repository guardrails

- Do not add a database table or migration for execution reports in this release.
- Do not alter function schedules or business side effects.
- Do not call payment, subscription, notification, cleanup, or external reporting APIs solely because a report is being written.
- Do not expose Blob Storage URLs, SAS tokens, connection strings, or secrets in the Admin UI or report JSON.
- Do not let the Admin request accept arbitrary blob names.
- Keep storage code in the shared Repository layer; keep Azure Functions trigger orchestration in the Function project and presentation/authorization in the Web project.
- Preserve existing StyleCop, nullable, XML documentation, and warnings-as-errors conventions.
- Do not overwrite existing user changes; limit changes to the feature’s shared reporting, Function integration, Admin pages/resources, tests, and documentation.
- Treat `UpdateSubscriptions` as a no-op in the report until its business implementation changes; never imply synchronization occurred.

## 12. Existing repository references

- Function host and shared execution flow: `BancoAlimentar.AlimentaEstaIdeia.Function/Program.cs`, `MultiTenantFunction.cs`, and `FunctionSlotExecution.cs`.
- Current timer functions: `DeleteOldSubscriptionFunction.cs`, `GenerateDonationReportFunction.cs`, `GenerateSiteHealthReportFunction.cs`, `MultiBancoPaymentNotificationFunction.cs`, and `UpdateSubscriptions.cs`.
- Existing Blob reporting patterns: `BancoAlimentar.AlimentaEstaIdeia.Repository/Reporting` and `Repository/SiteHealth`.
- Web DI, localization, and Admin authorization: `BancoAlimentar.AlimentaEstaIdeia.Web/Startup.cs`, `AdminSharedResources.cs`, and `Areas/Admin/Pages`.
- Function tests: `BancoAlimentar.AlimentaEstaIdeia.Function.Tests`.
- Web test host and Admin integration patterns: `BancoAlimentar.AlimentaEstaIdeia.Web.TestHost` and `BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests`.
