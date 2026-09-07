# Azure Functions

The `BancoAlimentar.AlimentaEstaIdeia.Function` project hosts scheduled background jobs for the Alimente esta ideia platform. These functions run outside the web application and operate across **all tenants** registered in the shared infrastructure database.

## Architecture

All timer functions inherit from `MultiTenantFunction`, which:

1. Loads the tenant list from `InfrastructureDbContext`.
2. Fetches each tenant's configuration from Azure Key Vault via `IKeyVaultConfigurationManager`.
3. Opens a tenant-specific SQL connection (`DefaultConnection`) and `UnitOfWork`.
4. Runs the function logic for that tenant, logging success or failure to Application Insights.

This means a single function execution processes every tenant in sequence. Failures for one tenant are logged but do not stop processing for the others.

**Project:** `BancoAlimentar.AlimentaEstaIdeia.Function`

**Runtime:** .NET 10, Azure Functions v4 (isolated worker)

**Deployment slots:** Timer functions must run only in the **production** slot of `AlimentaEstaIdeia-tools`. Non-production slots (preprod, developer) may share production database connection strings for web validation, but must not execute scheduled jobs twice. See [Deployment slots](#deployment-slots) below.

**Configuration (production):**

- `VaultUri` — tenant application secrets (connection strings, Easypay, storage, etc.)
- `SasVaultUri` — shared platform secrets
- `ConnectionStrings:Infrastructure` — multi-tenant registry database
- `FunctionExecutionReports:ConnectionString` — private Azure Blob Storage connection string used for execution reports. Configure this explicitly per environment/slot; it must not be committed.
- `FunctionExecutionReports:ContainerName` — private container name, normally `function-execution-reports`.
- `FunctionExecutionReports:RetentionDays` — report retention in days; the application requires at least 90.

Copy `local.settings.json.example` to `local.settings.json` for local development.

## Functions

| Function | Schedule (UTC) | Purpose |
|----------|----------------|---------|
| `GenerateDonationReportFunction` | Daily at 06:00 | Build static donation analytics pages and publish to blob storage |
| `GenerateSiteHealthReportFunction` | Daily at 07:00 | Query Log Analytics and publish the site health report |
| `DeleteOldSubscriptionFunction` | Every 24 hours | Remove abandoned subscription drafts older than one day |
| `MultiBancoPaymentNotificationFunction` | Daily at 11:59 | Send reminder emails for pending Multibanco payments |
| `UpdateSubscriptions` | Every 24 hours | Reserved daily subscription maintenance hook (currently a no-op) |

### GenerateDonationReportFunction

Generates the public donation analytics report served at `/report/` on each tenant site.

For each tenant (when `DonationReport:Enabled` is true, or when forced via admin):

1. Queries donations, campaigns, food banks, products, payments, and subscriptions from the tenant database.
2. Builds an analytics snapshot (`DonationReportRepository`).
3. Renders static HTML pages, CSS, and client-side filter scripts (`DonationReportHtmlGenerator`).
4. Uploads files to the tenant's Azure Storage container under `wwwroot/report/`.

The report includes KPIs, charts, campaign and food-bank breakdowns, payment analysis, subscription statistics (including per-frequency counts and paid totals), and cross-tabulations. The header shows when the report was generated.

The same generation logic can also be triggered manually from the admin backoffice or locally via the Tools CLI (see below).

### DeleteOldSubscriptionFunction

Cleans up subscription records that were created but never activated.

For each tenant, it finds subscriptions where:

- `Status` is `Created`, and
- `Created` is more than one day ago.

For each match:

- If no other **active** subscription shares the same initial donation, the initial donation is deleted.
- Linked donation rows are cleared and the subscription record is removed.

This prevents incomplete checkout flows from leaving orphaned data in the database.

### MultiBancoPaymentNotificationFunction

Sends payment reminder emails for Multibanco donations that are still waiting for payment.

For each tenant:

1. Finds Multibanco payments from the last three days that have not yet received an email notification.
2. For each payment with an associated user, calls the tenant web application's reminder endpoint (`WebUrl` configuration, authenticated with `ApiCertificateV3`).
3. The web app handles the actual email send; the function only triggers it.

Runs daily at 11:59 UTC so reminders go out before reference expiry.

### UpdateSubscriptions

Scheduled daily subscription maintenance function. The current implementation opens and commits a database transaction without performing additional work — it is a placeholder kept for future subscription sync logic. Tests verify it completes without error and is idempotent.

## Deployment slots

The web app preprod slot may use production data for validation. The function app must **not** run timer side effects from non-production slots.

### Code guard (deployed with the function)

`MultiTenantFunction.RunFunctionCore` checks `WEBSITE_SLOT_NAME` before touching tenant databases:

| Slot | `WEBSITE_SLOT_NAME` | Timer side effects |
|------|---------------------|-------------------|
| Production | empty | Allowed |
| preprod, developer, … | slot name | Skipped (`FunctionTimerSkippedNonProductionSlot` in Application Insights) |

Local `func start` has no slot name set, so timers run normally.

### Azure configuration (preprod and developer slots)

Timer triggers are disabled automatically in CI/CD via [`scripts/configure-function-slot-timer-settings.ps1`](../scripts/configure-function-slot-timer-settings.ps1):

| Pipeline | When |
|----------|------|
| `azure-pipelines/preprod-release.yml` | After web deploy to **preprod** |
| `azure-pipelines/developer-debug.yml` | After function deploy to **developer** |

The script sets these **slot-sticky** app settings on `AlimentaEstaIdeia-tools` (not swapped to production):

| App setting | Value |
|-------------|-------|
| `AzureWebJobs.GenerateDonationReportFunction.Disabled` | `true` |
| `AzureWebJobs.DeleteOldSubscriptionFunction.Disabled` | `true` |
| `AzureWebJobs.MultiBancoPaymentNotificationFunction.Disabled` | `true` |
| `AzureWebJobs.UpdateSubscriptions.Disabled` | `true` |

**One-time manual apply** (after `az login`):

```powershell
.\scripts\configure-function-slot-timer-settings.ps1 -SlotName preprod
.\scripts\configure-function-slot-timer-settings.ps1 -SlotName developer
```

**Portal (alternative):** Function App → **Deployment slots** → slot → **Environment variables** → add each name above, check **Deployment slot setting**, Save.

This stops the timer scheduler from firing in the slot. The code guard above is a safety net if a setting is missing.

## Observability

Functions log to **Application Insights**. See [Application Insights telemetry](Application-Insights.md) for the full catalog of custom events, traces, and KQL examples.

Summary for functions:

- `FunctionCoreExecuted` — successful run per tenant
- `FunctionTimerSkippedNonProductionSlot` — timer fired in a non-production deployment slot; no database work performed
- `DonationReportPublished` — report generation metrics (pages uploaded, paid amount, donation count)
- Exceptions and trace messages for failures and cleanup actions

### Function execution report storage

Every one of the five functions listed above writes a private JSON execution report after it runs. The report contains bounded activities, a safe summary, counters, outcome, duration, and UTC timestamps. The Admin page at `/Admin/FunctionExecutionReports` reads the latest report for the current tenant and deployment slot. Tenant rows also show the metadata-only global infrastructure status; they never read another tenant's report.

Configure the same private storage account and container for the Function App and the Web App in each environment/slot. Use slot-specific configuration so a preproduction report cannot appear as a production report:

| Setting | Required value |
|---------|----------------|
| `FunctionExecutionReports:Enabled` | `true` |
| `FunctionExecutionReports:ConnectionString` | Secret connection string for the shared report storage account; use a Key Vault reference in Azure where possible |
| `FunctionExecutionReports:ContainerName` | `function-execution-reports` (private container) |
| `FunctionExecutionReports:RetentionDays` | `90` or greater |

If `FunctionExecutionReports:ConnectionString` is omitted, the current implementation may use the existing `AzureStorage:ConnectionString` fallback. Treat that fallback as a compatibility option only; configure the dedicated setting explicitly for deployed slots and local development. The Web App must receive the value through tenant/slot configuration before an Admin request is made.

The storage layout is versioned and slot/tenant scoped:

```text
v1/{slotKey}/{scopeKey}/{functionKey}/executions/{yyyy}/{MM}/{dd}/{executionId}.json
v1/{slotKey}/{scopeKey}/{functionKey}/latest.json
```

Apply an Azure Storage lifecycle management rule to delete blobs under `v1/` execution prefixes after the configured retention period (90 days by default). Keep `latest.json` long enough to show the last execution, or include it in a separate rule if the operational policy requires it. Lifecycle deletion is storage administration, not a function side effect, and must never delete a tenant database record. Verify the rule separately for production, preprod, and developer storage accounts.

The reports are not public files. Restrict the configured storage credential to the required container operations: the Function App needs write access and the Web App needs read access. If the deployment uses managed identities instead of a connection string, grant those equivalent data-plane roles and keep the container private. Do not log or store connection strings, tokens, authorization headers, payment data, or raw provider payloads in a report.

### Manual execution from Admin

SuperAdmins can use **Run now** on `/Admin/FunctionExecutionReports`. The Web App validates the selected key against the five-function catalog and places a small command on a private Azure Storage Queue. The browser never receives the queue connection string. The Function App must consume that queue and dispatch only catalog keys; it must continue applying the existing deployment-slot guard.

Configure these settings in both the Web App and the Function App for each
environment and slot. Azure App Service settings use double underscores because
`.NET` maps `__` to a configuration section separator:

| Setting | Required value |
|---------|----------------|
| `FunctionExecutionCommands__Enabled` | `true` when manual execution is enabled |
| `FunctionExecutionCommands__ConnectionString` | Private queue storage connection string; use a Key Vault reference in Azure |
| `FunctionExecutionCommands__QueueName` | Lowercase queue name, normally `function-execution-commands` |

For the local Function host, use the same double-underscore names under the
`Values` object in `local.settings.json`. For local Web configuration, the
equivalent keys are `FunctionExecutionCommands:Enabled`,
`FunctionExecutionCommands:ConnectionString`, and
`FunctionExecutionCommands:QueueName`. The example files contain the expected
local forms.

Keep the queue private and grant the Web App permission to create/send messages and the Function App permission to read/delete messages. A successful Admin response means that the command was queued, not that the Function has completed. The execution report is the source of truth for the result.

## Running locally

### Function host

1. Copy `BancoAlimentar.AlimentaEstaIdeia.Function/local.settings.json.example` to `local.settings.json`.
2. Configure Key Vault access and the infrastructure connection string (user secrets share the same `UserSecretsId` as the Web project).
3. Install [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local) and run:

```powershell
cd BancoAlimentar.AlimentaEstaIdeia.Function
func start
```

Timer triggers use `RunOnStartup = false` for the donation report (it only fires at 06:00 UTC). Other functions may run on startup. To trigger a function manually while the host is running, use the admin API with the master key printed in the console.

### Donation report without the function host

To generate report files directly to disk (for local browsing at `/report/`):

```powershell
dotnet run --project BancoAlimentar.AlimentaEstaIdeia.Tools -- generate-donation-report
```

This writes HTML to `BancoAlimentar.AlimentaEstaIdeia.Web/wwwroot/report/`. Start the Web app and open `/report/`.

You can also generate reports from **Admin → Generate donation report** while running the site in Development mode.

## Related documentation

- [Test projects overview](TESTS.md) — includes `BancoAlimentar.AlimentaEstaIdeia.Function.Tests`
