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

**Runtime:** .NET 9, Azure Functions v4 (isolated worker)

**Configuration (production):**

- `VaultUri` — tenant application secrets (connection strings, Easypay, storage, etc.)
- `SasVaultUri` — shared platform secrets
- `ConnectionStrings:Infrastructure` — multi-tenant registry database

Copy `local.settings.json.example` to `local.settings.json` for local development.

## Functions

| Function | Schedule (UTC) | Purpose |
|----------|----------------|---------|
| `GenerateDonationReportFunction` | Daily at 06:00 | Build static donation analytics pages and publish to blob storage |
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

## Observability

Functions log to **Application Insights**:

- `FunctionCoreExecuted` — successful run per tenant
- `DonationReportPublished` — report generation metrics (pages uploaded, paid amount, donation count)
- Exceptions and trace messages for failures and cleanup actions

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
- [Useful SQL queries](database.queries.md)
