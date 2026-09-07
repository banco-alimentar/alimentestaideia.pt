# Implementation Plan: Azure Function Execution Reports

## 1. Plan status and scope

**Status:** P0/P1 shared Repository implementation, P2-P4 Function integration, Web/Admin P5-P6 integration, review fixes, and focused validation completed.

**Test-first status:** Repository contract/coordinator/storage tests, Function integration/catalog tests, and Admin overview/viewer tests were added against the planned seams. Repository P0/P1, Function P2-P4, and Web/Admin P5-P6 production infrastructure are implemented. The shared read contract now exposes `FunctionExecutionReportStorageState.Available` and `FunctionExecutionReportLatestPointer.Summary`.

**P0/P1 completion notes (2026-09-07):** Added the versioned report contracts, five-function catalog metadata, validated options and safe path helpers, UTC/ID seams, bounded activity writer, best-effort coordinator, typed storage states, and private Azure Blob implementation. Immutable execution blobs are written before a timestamp/ETag-guarded latest pointer. Read operations return `Available`; execution finalization returns `Succeeded` when persistence completes. The activity-limit diagnostic explicitly reports truncation.

**Review fixes (2026-09-07):** Added an explicit outcome-marking seam with a safe fallback that converts recorded errors into `Failed` reports when a delegate completes optimistically. All five functions now mark skipped, failed, and partial work explicitly. Reports now include trigger type, environment, and safe Application Insights correlation metadata. Multi-tenant global reports expose discovered, processed, succeeded, failed, and skipped tenant counters. Warning, error, activity-counter, and summary-counter collections are bounded, and credential/token/connection-string patterns are rejected. Latest-pointer ordering is deterministic for equal timestamps using the execution ID. Retention is represented by a validated Azure Blob lifecycle-policy descriptor; the application never deletes report blobs during normal execution, so the descriptor is intended for external storage-management deployment.

**Focused validation (2026-09-07):**

- `dotnet test BancoAlimentar.AlimentaEstaIdeia.Repository.Tests/BancoAlimentar.AlimentaEstaIdeia.Repository.Tests.csproj --no-restore -p:BuildInParallel=false -p:UseSharedCompilation=false -p:MSBuildNodeReuse=false --filter FullyQualifiedName~FunctionExecutionReportTests`: passed, 30/30.
- `dotnet test BancoAlimentar.AlimentaEstaIdeia.Function.Tests/BancoAlimentar.AlimentaEstaIdeia.Function.Tests.csproj --no-restore -p:BuildInParallel=false -p:UseSharedCompilation=false -p:MSBuildNodeReuse=false --filter FullyQualifiedName~FunctionExecutionReport --logger "console;verbosity=minimal"`: passed, 7/7.
- `dotnet test BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests/BancoAlimentar.AlimentaEstaldeia.Web.Integration.Tests.csproj --no-restore -p:BuildInParallel=false -p:UseSharedCompilation=false -p:MSBuildNodeReuse=false --filter FullyQualifiedName~AdminFunctionExecutionReportsTests --logger "console;verbosity=minimal"`: passed, 12/12.

The focused commands required elevated local test-host socket access in this environment. No focused suite remains blocked.

**Review-fix focused validation (2026-09-07):** Repository report tests passed, 30/30, including outcome marking, metadata, tenant counters, bounded collections, expanded sensitive-text rejection, deterministic pointer ordering, and lifecycle-policy descriptor coverage. Function report tests passed, 7/7. Repository and Function builds passed with 0 warnings/errors. Test execution used local test-host socket access only; no Azure Storage, Key Vault, or production database operations were performed.

**Web/Admin review fixes (2026-09-07):**

- The Admin overview now shows the latest report completion time with both UTC and local offset, duration, summary message, tenant status, and a separate metadata-only global infrastructure status for tenant-scoped functions.
- The details page now renders the summary message, all bounded function-specific counters, localized activity severities, localized `Available`/missing/corrupt/expired/unavailable storage states, and explicit UTC/local-offset timestamps.
- Global infrastructure status is queried only from the fixed `global` scope for the current slot. The overview does not link to or load global report details from a tenant row, preventing cross-tenant report disclosure.
- The catalog continues to render all five functions in stable catalog order, and the existing Admin authorization, navigation, catalog-key validation, execution-ID validation, and private reader path boundaries remain in place.
- Added focused Admin coverage for duration/summary, global metadata-only status, localized report content, all-five-function representation, authorization, storage states, and traversal-like identifiers.
- Documented the required per-slot shared private Blob Storage configuration, managed-identity access, versioned report paths, and the 90-day lifecycle policy. Added the explicit local `AzureStorage__ConnectionString` fallback placeholder.

Validation for the review fixes:

- `dotnet build BancoAlimentar.AlimentaEstaIdeia.Web/BancoAlimentar.AlimentaEstaIdeia.Web.csproj --no-restore -p:RunAnalyzers=false`: passed, 0 warnings/errors. The normal build remains blocked by three existing StyleCop errors in Repository report production files, which were outside this Web/Admin-only scope.
- `dotnet test BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests/BancoAlimentar.AlimentaEstaldeia.Web.Integration.Tests.csproj --no-restore -p:RunAnalyzers=false --filter FullyQualifiedName~AdminFunctionExecutionReportsTests`: passed, 13/13. The test host required elevated local socket permission.
- Resource XML and `git diff --check`: passed.

This plan implements SPEC.md and ARCHITECTURE.md. It covers the five current timer functions:

- GenerateDonationReportFunction
- GenerateSiteHealthReportFunction
- DeleteOldSubscriptionFunction
- MultiBancoPaymentNotificationFunction
- UpdateSubscriptions

The persisted “log file” is a private, human-readable JSON blob. The Admin UI renders that JSON and never exposes a public blob URL. Timer schedules and business side effects must not change.

### Worktree guardrail

Before implementation, run git status --short --branch and preserve all unrelated changes. The current worktree contains the untracked feature-context directory with SPEC.md and ARCHITECTURE.md; do not rewrite either file. Only feature implementation, tests, and directly related documentation may be changed.

There is no AGENTS.md inside this repository. The only discovered AGENTS.md belongs to a different sibling repository and is not applicable. Existing repository rules remain: .NET 10, nullable annotations, StyleCop, warnings as errors, no destructive Git operations, and no production database migrations.

## 2. Dependency graph

    P0 Repository contracts/options/catalog/path helpers
     ├── P1 Blob store, reader, writer, and coordinator
     │    ├── P2 Function DI and MultiTenantFunction integration
     │    │    ├── P3 Four multi-tenant function adapters
     │    │    └── P4 Standalone site-health adapter
     │    └── P5 Repository/function unit tests
     ├── P6 Web DI and tenant/global read adapter
     │    └── P7 Admin query service and overview/viewer
     │         ├── P8 Localization and navigation
     │         └── P9 Admin integration tests
     └── P10 Documentation and configuration updates

P5 and P9 can be developed against fakes after their interfaces exist. Final validation depends on P0 through P10.

Recommended order:

1. P0, then P1 with repository tests.
2. P2, then P3 and P4.
3. P6, P7, and P8.
4. P9 and P10.
5. Full validation and review.

## 3. Exact project and file impact

### Shared Repository project

Add a new BancoAlimentar.AlimentaEstaIdeia.Repository/FunctionExecutionReports folder:

- FunctionExecutionReport.cs: versioned immutable report DTO.
- FunctionExecutionReportActivity.cs: timestamped activity DTO.
- FunctionExecutionReportSummary.cs: common and function-specific counters.
- FunctionExecutionReportOutcome.cs: Succeeded, Partial, Failed, Skipped, Disabled.
- FunctionExecutionReportActivitySeverity.cs: Information, Warning, Error.
- FunctionExecutionReportScope.cs: normalized tenant/global and slot target.
- FunctionExecutionReportLatestPointer.cs: latest metadata document.
- FunctionExecutionFunctionDescriptor.cs: catalog metadata.
- FunctionExecutionReportCatalog.cs: the five stable function keys, names, descriptions, schedules, localization keys, and scope kind.
- FunctionExecutionReportOptions.cs: configuration model and defaults.
- FunctionExecutionReportConfiguration.cs: binding, validation, connection fallback, and scope resolution helpers.
- FunctionExecutionReportPaths.cs: normalized blob paths.
- IFunctionExecutionReportStore.cs: immutable write and latest-pointer write contract.
- IFunctionExecutionReportReader.cs: latest and exact-report read contract.
- IFunctionExecutionReportCoordinator.cs: execution lifecycle contract.
- FunctionExecutionReportWriter.cs: bounded activity/counter writer.
- FunctionExecutionReportCoordinator.cs: IDs, UTC timestamps, outcomes, and best-effort finalization.
- BlobFunctionExecutionReportStore.cs: private Blob Storage JSON implementation.
- FunctionExecutionReportStorageResult.cs, or an equivalent typed result: success, missing, expired, corrupt, and unavailable states.

The Repository project already references Azure.Storage.Blobs and the required Microsoft.Extensions packages, so no new package is expected.

### Functions project

Modify:

- Function/Program.cs: register store, reader/coordinator dependencies, UTC clock, ID generator, and options.
- Function/MultiTenantFunction.cs: invocation ID, slot/infrastructure reports, tenant-scoped writers, continuation, and finalization.
- Function/FunctionSlotExecution.cs: add only a normalized slot-key helper if needed; preserve the current production/local decision.
- Function/DeleteOldSubscriptionFunction.cs: deletion activities and counters.
- Function/GenerateDonationReportFunction.cs: report result metrics while retaining DonationReportPublished telemetry.
- Function/MultiBancoPaymentNotificationFunction.cs: safe notification counts and outcomes.
- Function/UpdateSubscriptions.cs: explicit no-op summary.
- Function/GenerateSiteHealthReportFunction.cs: direct standalone coordinator wrapper.
- Function/FunctionInitializer.cs: only if a reporter/context must be passed to delegates; keep unit-of-work behavior unchanged.
- Function/local.settings.json.example: safe local configuration names/defaults only.

### Web/Admin project

Add:

- Web/Areas/Admin/Pages/FunctionExecutionReports/Index.cshtml
- Web/Areas/Admin/Pages/FunctionExecutionReports/Index.cshtml.cs
- Web/Areas/Admin/Pages/FunctionExecutionReports/Details.cshtml
- Web/Areas/Admin/Pages/FunctionExecutionReports/Details.cshtml.cs
- A tenant-aware query service and row model, preferably under Web/Areas/Admin/Pages/FunctionExecutionReports/FunctionExecutionReportQueryService.cs and FunctionExecutionReportRow.cs. If repository conventions require reusable services outside pages, use Web/Services/FunctionExecutionReports instead.

Modify:

- Web/Startup.cs: register reader, storage configuration adapter, query service, and options.
- Web/Areas/Admin/Pages/Index.cshtml: add a localized navigation item.
- Web/Resources/AdminSharedResources.resx
- Web/Resources/AdminSharedResources.en.resx
- Web/Resources/AdminSharedResources.es.resx
- Web/Resources/AdminSharedResources.fr.resx

Use the inherited AdminArea policy. Do not add this page to the SuperAdmin-only RoleArea list unless requirements change.

### Tests

Add or modify:

- Repository.Tests/FunctionExecutionReportTests.cs: contracts, serialization, bounds, outcomes, paths, pointer ordering, options, and storage failures.
- Function.Tests/FunctionExecutionReportIntegrationTests.cs: all five functions and shared paths.
- Function.Tests/FunctionCatalogCompletenessTests.cs: reflection over Function attributes and resource coverage.
- Function.Tests/FunctionTestHelper.cs: explicit fake reporter/store seams, preserving existing helpers as needed.
- Web.IntegrationTests/IntegrationTests/AdminFunctionExecutionReportsTests.cs: authorization, overview, viewer, localization, unavailable data, and isolation.
- Web.TestHost/CustomWebApplicationFactory.cs only if a fake reader must be registered centrally.

Do not overwrite unrelated dirty-worktree changes or alter unrelated fixtures.

### Documentation

Modify:

- Documentation/Azure-Functions.md
- Documentation/Application-Insights.md
- Documentation/TESTS.md
- Function/local.settings.json.example

Review scripts/configure-function-slot-timer-settings.ps1 and deployment YAML only if new non-secret configuration needs deployment. Do not alter timer schedules or slot disabling.

## 4. Shared report model and lifecycle

### Canonical report

FunctionExecutionReport must contain:

- SchemaVersion.
- FunctionKey and FunctionName.
- ExecutionId and InvocationId.
- TriggerType, Environment, and SlotKey.
- ScopeKind and ScopeKey: tenant name or global.
- StartedAtUtc, CompletedAtUtc, and DurationMilliseconds.
- Outcome.
- BusinessDataChanged.
- Ordered, bounded Activities.
- Summary.
- Safe Warnings and Errors.
- Application Insights correlation identifier when available.

Each activity contains ordinal, UTC timestamp, activity key, severity, scope/tenant key, safe message, and optional numeric counters. The report is an audit summary, not a debug dump. Never store passwords, API keys, access tokens, connection strings, authorization headers, payment-card data, raw response bodies, or unbounded provider payloads.

### Coordinator and writer

The coordinator must:

1. Create execution and invocation IDs.
2. Record trigger, slot, environment, and scope metadata.
3. Provide a tenant/global writer.
4. Enforce activity and message limits and add one truncation warning.
5. Accept explicit outcomes: success, partial, failed, skipped, disabled.
6. Attempt finalization in a finally path.
7. Preserve a business exception when report persistence also fails.
8. Emit low-cardinality telemetry for persisted, persistence-failed, and latest-pointer-failed events.
9. Never cause a payment, subscription, notification, deletion, or report-generation retry.

Business success plus report-storage failure remains a business success with telemetry indicating the missing report. Business failure plus storage failure retains the business exception.

### Multi-tenant integration

MultiTenantFunction.RunFunctionCore is the shared integration point for DeleteOldSubscriptionFunction, GenerateDonationReportFunction, MultiBancoPaymentNotificationFunction, and UpdateSubscriptions.

Flow:

1. Start one invocation ID.
2. On non-production slot, write a global Skipped report and return before infrastructure or tenant DB work.
3. Discover tenants and record the count.
4. Create one tenant-scoped report per tenant using the same invocation ID.
5. Record Key Vault/configuration loading, database setup, delegate start/end, and tenant result.
6. Continue to later tenants after an allowed per-tenant exception.
7. Finalize each tenant report in finally.
8. Finalize global infrastructure failure where applicable without exposing cross-tenant details in a tenant view.

Change the current ExecuteFunction delegate to an explicit reporter-aware form, for example:

    Func<IUnitOfWork, ApplicationDbContext, IFunctionExecutionReportWriter, Task>

This avoids static mutable state and lets each function write to the correct tenant report. Update direct function tests to provide a fake writer.

### Standalone site-health integration

GenerateSiteHealthReportFunction.Run is wrapped directly because it does not inherit MultiTenantFunction:

- Slot guard rejection: Skipped.
- SiteHealthReport:Enabled false: Disabled.
- Successful GenerateAndStoreAsync: Succeeded with period count and timestamp.
- Exception: Failed with safe error/correlation data; preserve existing rethrow.

Do not change site-health report JSON, generation-status blobs, or SiteHealthReportService behavior. The execution report is separate.

## 5. Blob Storage paths, retention, and security

### Configuration

Add FunctionExecutionReports options:

    Enabled: true
    ConnectionString: empty
    ContainerName: function-execution-reports
    RetentionDays: 90
    MaxActivities: 500
    MaxActivityMessageLength: 1000

A dedicated connection is preferred. If absent, use AzureStorage:ConnectionString for the current tenant scope or host/global scope. Enforce retention of at least 90 days and validate container/path values. Never log option values.

For multi-tenant functions resolve storage after tenant configuration is loaded. For standalone site-health/global reports resolve host/global storage. The Web reader needs a scoped storage configuration adapter so a tenant request cannot read another tenant or slot.

### Path layout

Use this private-container layout:

    v1/{slotKey}/{scopeKey}/{functionKey}/executions/{yyyy}/{MM}/{dd}/{executionId}.json
    v1/{slotKey}/{scopeKey}/{functionKey}/latest.json

Examples:

    v1/production/alimentestaideia/DeleteOldSubscriptionFunction/executions/2026/09/07/{executionId}.json
    v1/production/alimentestaideia/DeleteOldSubscriptionFunction/latest.json
    v1/production/global/GenerateSiteHealthReportFunction/executions/2026/09/07/{executionId}.json
    v1/production/global/GenerateSiteHealthReportFunction/latest.json

Normalize slot, scope, and function segments to restricted lowercase-safe values. User input must never be treated as a raw blob path.

Write the immutable execution blob first, then update latest.json. The pointer contains execution ID, completion time, outcome, duration, and report path. Use ETag/conditional replacement or a timestamp comparison so a slower old run cannot replace a newer latest pointer.

Historical blobs are never overwritten. Configure an Azure Storage lifecycle rule on the execution prefix for RetentionDays, default 90. Runtime pruning, if needed, is bounded and best effort. A pruning failure must not fail business work.

The overview reads one latest pointer per catalog function. The details page reads only the selected report. Missing pointer, missing report, corrupt JSON, expired report, and storage access failure are distinct typed states; do not silently fall back to an older report.

## 6. Function-specific logging

### DeleteOldSubscriptionFunction

Record candidate count, transaction start/commit/rollback, candidate inspection, active-sibling decisions, initial donation retained/deleted, subscription/donation deletion counts, and safe candidate/tenant failures.

Summary metrics: candidates, subscriptions deleted, donations deleted, retained initial donations, tenant failures.

### GenerateDonationReportFunction

Record enabled/skipped state, snapshot/build boundaries, pages generated/uploaded/local counts, paid count/amount, and publication result. Retain DonationReportPublished and existing traces. Do not record storage/database credentials or raw data.

### GenerateSiteHealthReportFunction

Record slot result, enabled/disabled state, 24-hour and 7-day query phases, period count, generated timestamp, persistence result, and safe failure. Retain SiteHealthReportPublished, generation state, and rethrow behavior.

### MultiBancoPaymentNotificationFunction

Record pending count, records with/without a user, reminder attempts, success/failure counts, status-code counts, and final summary. Never persist ApiCertificateV3, credential-bearing URLs, email addresses, or raw response bodies. Keep existing request telemetry.

### UpdateSubscriptions

Record transaction begin/commit/rollback and explicitly state that no subscription synchronization was performed. The summary must not claim subscriptions were updated. Preserve the current no-op.

## 7. Catalog and Admin read architecture

### Catalog

Create a shared catalog with stable keys and metadata:

| Key | Scope | Schedule | Description |
|---|---|---|---|
| GenerateDonationReportFunction | Tenant | 0 0 6 * * * | Donation analytics publication |
| GenerateSiteHealthReportFunction | Global | 0 0 7 * * * | Site-health snapshot |
| DeleteOldSubscriptionFunction | Tenant | * * */24 * * * | Abandoned subscription cleanup |
| MultiBancoPaymentNotificationFunction | Tenant | 0 59 11 * * * | Pending Multibanco reminders |
| UpdateSubscriptions | Tenant | * * */24 * * * | Current no-op maintenance |

The catalog is metadata only and must not alter TimerTrigger attributes. Reflection tests must fail when a new Function attribute has no catalog entry. Catalog localization keys must exist in Portuguese, English, Spanish, and French resources.

### Web DI and query service

Register the reader and scoped tenant/global storage resolver in Startup.cs. The query service:

- Builds all overview rows from the catalog, including functions with no report.
- Reads one latest pointer per function.
- Fetches a full report only for the details page.
- Returns typed no-report, missing, expired, corrupt, and unavailable states.
- Isolates one function’s storage failure from other rows.
- Derives tenant and slot server-side.
- Never accepts a tenant, container, or arbitrary blob-name override from the query string.

### Pages

Use routes:

- /Admin/FunctionExecutionReports
- /Admin/FunctionExecutionReports/Details?functionKey=...&executionId=...

The overview shows function name/key, schedule/description, latest timestamp with UTC indication, status text, duration, safe summary, and a details link or explicit unavailable/no-report state.

The details page validates functionKey against the catalog, derives current scope, loads the selected report through the reader, and displays metadata, activity timeline, final summary, warnings/errors, and business-data-changed state. It handles missing, expired, corrupt, and unavailable reports clearly and links back to the overview.

Use existing Admin page layout/imports and _AdminNavItem. The page inherits AdminArea authorization; add no manual execution action.

## 8. DI boundaries

### Functions

Register in Function/Program.cs:

- Validated FunctionExecutionReportOptions.
- Singleton stateless Blob store/reader.
- Coordinator or coordinator factory that holds no per-tenant state.
- UTC clock and execution-ID generator.
- Safe telemetry adapter, if needed.

Pass resolved scope/connection to each operation; never capture tenant configuration in a singleton. Update descendant constructors to pass catalog key and coordinator. Inject the coordinator directly into GenerateSiteHealthReportFunction.

### Web

Register in Startup.cs:

- Blob reader/store implementation.
- Scoped storage configuration resolver.
- Scoped Admin query service.
- Validated options.

Do not make Razor page models call BlobServiceClient directly. Do not inject scoped TenantConfigurationRoot into a singleton.

## 9. Localization and accessibility

Add AdminSharedResources keys in all four resource files for page title/intro, function names/descriptions, schedule, latest execution, outcome, duration, summary, details link, report metadata, activity/severity, final summary, warnings/errors, UTC marker, no-report/missing/expired/corrupt/unavailable states, and all five outcomes.

Store canonical enum values and localize them at render time. Status must include text and accessible labels in addition to Bootstrap colors. Use heading hierarchy, keyboard-accessible links, responsive tables/sections, and meaningful link text.

## 10. Test plan

### Repository.Tests

Add tests for:

1. Serialization/deserialization of schema version, UTC dates, outcomes, activities, and summary.
2. Activity/message bounds and one truncation warning.
3. Success/no-work, partial, failed, skipped, and disabled outcomes.
4. BusinessDataChanged independent of report persistence result.
5. Path normalization and traversal/invalid-key rejection.
6. Latest-pointer monotonic update for out-of-order completion.
7. Missing/corrupt/unavailable read results.
8. Storage failure preserving the original business outcome/exception.
9. Retention default 90 days and invalid lower values.
10. Secret-safe message contract.

Use fake storage, deterministic clock, and deterministic ID generation. No live Azure Storage.

### Function.Tests

Add tests for:

- Successful multi-tenant execution creates tenant report with activities/summary.
- One tenant failure does not hide later tenant results.
- Non-production slot creates global Skipped report before tenant/database work.
- Infrastructure exception finalizes Failed.
- DeleteOld reports candidate/deletion/retention counts without changing deletion semantics.
- Donation report reports skipped/success/failure metrics and retains existing telemetry.
- Multibanco reports counts without persisting the certificate.
- UpdateSubscriptions accurately reports its no-op.
- Site-health standalone skip/disabled/success/failure.
- Report-store failure is observable and does not duplicate business work.

Preserve existing DeleteOldSubscriptionFunctionTests, MultiBancoPaymentNotificationFunctionTests, and UpdateSubscriptionsFunctionTests; change only setup required by the reporter-aware delegate.

### Catalog tests

Reflect all Function attributes, assert exactly the five expected current functions, and verify all display/status localization keys exist in all four Admin resource sets.

### Admin integration tests

Add AdminFunctionExecutionReportsTests using CustomWebApplicationFactory, IntegrationTestDataSeeder, and WebTestAuthHelper:

- Anonymous GET redirects to login.
- Admin/Manager can view; ordinary authenticated users cannot.
- All five rows render with no reports.
- Latest metadata/status/duration and details links render.
- Details renders activities, summary, warnings/errors, and status text.
- Missing/corrupt/unavailable data is explicit and does not hide other rows.
- Invalid function/execution identifiers cannot escape catalog/path scope.
- Other tenant/slot reports are not returned.
- Portuguese and English labels are localized.
- Status is represented by text/accessible markup, not color alone.

Replace IFunctionExecutionReportReader with a fake in the test host; do not call Azure Storage.

## 11. Documentation and deployment deliverables

Update Documentation/Azure-Functions.md to include GenerateSiteHealthReportFunction, report status meanings, invocation versus tenant/global scope, storage configuration/path/retention, Admin route, privacy exclusions, and the host-disabled limitation.

Update Documentation/Application-Insights.md with report persistence/read failure events, function/execution/slot/scope correlation properties, and KQL examples. Retain all existing function telemetry documentation.

Update Documentation/TESTS.md with Repository, Function, catalog, and Admin coverage.

Update Function/local.settings.json.example with non-secret placeholders/defaults. Verify that the Azure Storage container is private and lifecycle retention is configured; document any policy that is managed outside this repository.

## 12. Task checklist

### P0 — Contracts, catalog, options, paths — complete

- [x] Add report DTOs/enums/scope/summary/activity contracts.
- [x] Add five-function catalog.
- [x] Add options/configuration validation and path builder.
- [x] Add store/reader/coordinator interfaces.
- [x] Add P0 repository tests.

Done when contracts compile, paths match Section 5, retention minimum is enforced, and catalog/resource metadata is testable.

### P1 — Storage and coordinator — complete

- [x] Implement immutable private Blob writes.
- [x] Implement latest pointer with monotonic update.
- [x] Implement typed reader states.
- [x] Implement bounded writer/coordinator and finally finalization.
- [ ] Emit safe report telemetry (Function integration phase).
- [x] Add failure and ordering tests.

Done when fake-store tests prove every outcome and persistence failures do not change business semantics.

### P2 — Shared Functions integration

- [x] Register Function DI.
- [x] Refactor MultiTenantFunction to explicit scoped reporter/context.
- [x] Add global slot/infrastructure finalization.
- [x] Add tenant finalization.
- [x] Preserve existing telemetry and per-tenant continuation.

Done when existing Function tests compile and shared success/partial/skip/failure tests pass.

### P3 — Multi-tenant adapters

- [x] Add DeleteOld activity/counters.
- [x] Add donation report metrics.
- [x] Add Multibanco safe counts.
- [x] Add UpdateSubscriptions no-op summary.
- [x] Preserve the direct-test delegate while adding the reporter-aware delegate.

Done when each function produces correct tenant reports without duplicate business calls.

### P4 — Standalone site health

- [x] Inject coordinator.
- [x] Report skip/disabled/success/failure.
- [x] Preserve site-health artifacts and telemetry.
- [ ] Add standalone tests.

Done when execution and site-health reports remain separate.

### P5 — Web read path/Admin pages — complete

- [x] Register reader/resolver/query service.
- [x] Add overview/details pages.
- [x] Add catalog-driven rows and unavailable states.
- [x] Enforce AdminArea and current scope.
- [x] Add navigation link.

Done when all five rows render, latest links work, details are secure, and one storage failure does not break the overview.

### P6 — Localization/accessibility — complete

- [x] Add four resource sets.
- [x] Add accessible status text and responsive sections.
- [x] Add resource/catalog completeness test (covered by the parallel catalog test).

### P7 — Documentation/deployment verification

- [x] Update Azure Functions, Application Insights, and test documentation.
- [x] Update local settings example.
- [ ] Verify private container/lifecycle requirements.
- [x] Add only required non-secret deployment configuration.

### P8 — Final validation — focused validation complete

- [x] Run status and diff checks.
- [x] Run targeted Repository, Function, catalog, and Admin tests.
- [ ] Run full solution build/test as practical.
- [x] Review secrets/privacy, tenant isolation, disabled-host limitation, and duplicate-side-effect risks.
- [x] Update this checklist after focused verification.

### P9 — Admin Run Now command path — implemented

- [x] Add a versioned, catalog-validated command contract in Repository.
- [x] Add dedicated queue options and local configuration placeholders.
- [x] Add a private QueueTrigger dispatcher with malformed-message validation.
- [x] Add allow-listed executor and deployment-slot guard interfaces.
- [x] Route timer and manual executions through the same five function runners.
- [x] Preserve `FunctionSlotExecution` so developer and preprod commands produce skipped reports without tenant work.
- [x] Preserve manual trigger metadata in execution reports.
- [x] Add the Azure Storage Queue worker extension package.
- [x] Add command-contract and dispatcher tests.

The queue trigger is intentionally not an HTTP endpoint. The Admin Web application must enqueue
`FunctionExecutionCommand` messages using the same private queue, and the Function App must have
the corresponding `FunctionExecutionCommands__ConnectionString` and
`FunctionExecutionCommands__QueueName` settings. The command is accepted only when its function
key is in `FunctionExecutionReportCatalog`; arbitrary type or method names are never resolved.

Focused validation for this phase:

- Repository tests: 246 passed.
- Function tests: 47 passed.
- Function project build: passed.
- Web project build: passed in isolation; the full solution build/test was not run for this scoped change.

## 13. Validation commands

Run from /Users/tiaandra/repos/alimentestaideia.pt with the SDK selected by global.json:

    git status --short --branch
    git diff --check

    dotnet build BancoAlimentar.AlimentaEstaIdeia.Repository/BancoAlimentar.AlimentaEstaIdeia.Repository.csproj --configuration Debug
    dotnet build BancoAlimentar.AlimentaEstaIdeia.Function/BancoAlimentar.AlimentaEstaIdeia.Function.csproj --configuration Debug
    dotnet build BancoAlimentar.AlimentaEstaIdeia.Web/BancoAlimentar.AlimentaEstaIdeia.Web.csproj --configuration Debug

    dotnet test BancoAlimentar.AlimentaEstaIdeia.Repository.Tests/BancoAlimentar.AlimentaEstaIdeia.Repository.Tests.csproj --configuration Debug --no-restore
    dotnet test BancoAlimentar.AlimentaEstaIdeia.Function.Tests/BancoAlimentar.AlimentaEstaIdeia.Function.Tests.csproj --configuration Debug --no-restore
    dotnet test BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests/BancoAlimentar.AlimentaEstaldeia.Web.Integration.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~AdminFunctionExecutionReportsTests

    dotnet build BancoAlimentar.AlimentaEstaIdeia.Web.sln --configuration Debug
    dotnet test BancoAlimentar.AlimentaEstaIdeia.Web.sln --configuration Debug --no-build

Do not run Azure Storage, Key Vault, or production database operations during local validation. If full-solution tests fail for environment reasons, report the exact test/project and retain successful targeted results.

## 14. Risks and mitigations

| Risk | Mitigation |
|---|---|
| Report writer changes business outcome | Business result/exception remains authoritative; persistence is best effort. |
| Tenant data leaks | Slot/scope path segments; server-derived scope; no raw blob paths. |
| Old run overwrites latest | Immutable blobs and conditional/timestamp-checked pointer. |
| Unbounded log or secret payload | Bounded writer and safe message/counter contract. |
| Global site-health storage differs from tenant storage | Explicit global/tenant resolver and tests for both scopes. |
| Host-disabled function cannot write a report | Document limitation and show no report available. |
| Existing tests use old delegate signature | Update explicit test seam, not hidden global state. |
| Localization drift | Catalog/resource completeness test. |
| Dirty worktree overwritten | Status before batches and final name-only diff review. |

## 15. Definition of done

Complete only when all five functions produce durable private reports for application-reached success, no-work, skipped, disabled, partial, and failure paths; tenant/global and slot boundaries are enforced; existing schedules, telemetry, outputs, and side effects remain intact; Admin overview/details are authorized, localized, accessible, and resilient; retention is configured/documented at 90 days or more; tests cover the acceptance criteria; documentation is updated; validation passes or environmental failures are recorded; and unrelated worktree changes remain untouched.
