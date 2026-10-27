# Local email capture and Selenium targets

Selenium runs against one explicitly selected target at a time. Local runs start a self-contained Development application on a temporary localhost port with SQLite in-memory data and Mailpit. Developer runs use the deployed developer slot. They do not share a database or inbox.

| Selection | Target | Included cases |
|-----------|--------|----------------|
| `local-email` | Local application | 8 shared password-reset cases using real captured emails |
| `local` | Local application | 60 cases: the 20 shared cases plus 40 self-contained regression cases |
| `developer` | Developer slot | 19 cases: 12 shared referral cases and 7 live sandbox payment cases |
| `offline` | No running application | 109 configuration, runner-selection, and Mailpit-reader cases; no Chrome, SQL connection, or real inbox |

Ordinary unit and in-process integration tests keep their existing email stubs. They do not require Mailpit. Every real email sent by the application under the `LocalMailpit` profile, including Identity and donation emails, goes to the local catcher instead of a real recipient.

## Start the email catcher

On macOS, Mailpit can run directly without Docker:

```bash
brew install mailpit
mailpit --listen 127.0.0.1:8025 --smtp 127.0.0.1:1025
```

Keep this terminal open while the tests run. If Mailpit is already running on these ports, do not start a second instance. See the [Mailpit installation instructions](https://github.com/axllent/mailpit#installation).

Alternatively, with Docker and Compose installed, run from the repository root:

```bash
docker compose -f compose.mailpit.yml up -d
```

Open the [local inbox](http://127.0.0.1:8025). SMTP listens on loopback port 1025; the inbox and API listen on loopback port 8025. The Compose definition pins [Mailpit v1.32.0](https://github.com/axllent/mailpit/releases/tag/v1.32.0), exposes no public ports, and configures no forwarding or relay. See the [Mailpit Docker documentation](https://mailpit.axllent.org/docs/install/docker/).

Captured messages contain live test-account security links. Do not expose the inbox to a public network or use real users for these tests.

## Run self-contained local browser tests

With .NET 10, Chrome and a compatible ChromeDriver available, keep Mailpit running and run from the repository root:

```bash
bash scripts/test-selenium.sh local-email
```

This selection runs the password-reset browser cases. For password-reset and referral cases together:

```bash
bash scripts/test-selenium.sh local
```

Local defaults to `SeleniumTest:Local:SelfContained=true`. The test fixture starts the website through Kestrel on an ephemeral loopback HTTP port, creates application and infrastructure SQLite databases in memory, and seeds synthetic site/admin accounts plus a campaign with paid and pending donations. No SQL Server, connection strings, manual accounts, Azure credentials, separately running website, or HTTPS development certificate is needed. Each test-owned host is disposed after use and its memory databases disappear.

All generated Identity emails still use the real application SMTP transport and Mailpit. The host does not replace them with an email stub. It does not use inherited local secrets or cloud database settings. Production models and SQL migrations are unchanged; SQLite compatibility adjustments exist only in the test host. These tests exercise browser/Identity/referral behavior, not SQL Server-specific collation, query translation, or migration deployment.

The test host uses real Identity cookies and role checks, without external OAuth credentials. It serves bundled static assets locally, including Identity validation-library aliases, and does not consult Azure blob storage for missing assets. Image uploads use an owned temporary directory outside the repository; disposing the host removes these files.

Two local payment cases exercise the donation UI, real Easypay SDK, production webhook verification, payment completion, confirmation-email composition, and the admin referral link. Only the provider is simulated, through endpoints registered by the test assembly on the same temporary loopback host. A test changes the simulated provider's payment status; it does not directly mark the application's donation paid. PayPal transport calls are rejected locally. These cases do not prove real-provider acceptance, settlement, or invoice PDF generation. External OAuth and Azure asset delivery also remain outside this local selection.

For headless Chrome, set `SeleniumTest__Headless=true`. Keep the Mailpit UI/API on loopback; the default is `http://127.0.0.1:8025`.

Run one browser suite at a time. Overlapping runs can compete for ChromeDriver ports, even though their application databases and files are isolated.

If Mailpit uses different ports, set `SeleniumTest__Local__MailpitBaseUrl` for its UI/API origin and `SeleniumTest__Local__MailpitSmtpPort` for SMTP. The SMTP default is `1025`. The self-contained host reads both settings; do not configure a public SMTP relay.

## Local browser coverage

The counts below include parameterized cases. The default `local` selection runs all 60:

| Group | Cases | Assertions |
|-------|-------|------------|
| Shared referral behavior | 12 | Creation and loaded QR image; exact/case-insensitive duplicates and automatic scrolling; empty/short/symbol codes; active/inactive/unknown links; anonymous access; paid-only admin metrics |
| Shared password reset | 8 | Empty/malformed email, unknown recipient, successful emailed reset, weak/mismatched passwords, invalid/reused tokens, and old/new password login |
| Campaign authorization | 4 | Another user's detail/edit URLs and forged edit POST are denied; ordinary users cannot access admin referrals |
| Campaign editing | 4 | Name/tagline, active/inactive/reactivated links, and public/private statistics visibility persist correctly |
| Campaign images | 3 | Upload/replace/remove; unsupported and oversized replacements preserve the existing usable image |
| Admin edge cases | 19 | Seven sort columns in both directions, ID ties, next/previous pagination, empty lists, and no-paid-donation/zero-opening metrics |
| Reset-token security | 4 | Expired, missing, malformed, and cross-account tokens cannot change the wrong password |
| Mobile and keyboard | 4 | Campaign creation/editing/duplicate feedback and password-reset validation/completion through narrow viewports or keyboard navigation |
| Referral payment confirmation | 2 | Campaign attribution survives local MBWay confirmation; real captured email includes the campaign and escapes HTML; admin donation details link to its referral |

The 40 additional cases have `Category=LocalBrowser` and require the test-owned host. Expiry tests shorten that host's built-in Identity token lifetime only after receiving an authentic email; they do not wait for wall-clock expiry or replace token validation. The `local-email` selection remains the eight shared cases so it also works in external-local mode. Use `local` for the expanded reset security and mobile/keyboard cases.

```bash
SeleniumTest__Headless=true bash scripts/test-selenium.sh local
bash scripts/test-selenium.sh local --list-tests
```

## Run on an Azure build agent

[`azure-pipeline-selenium-ui-tests.yml`](../azure-pipeline-selenium-ui-tests.yml) runs the `offline` checks followed by all self-contained `local` browser cases with headless Chrome. It installs .NET 10, uses the hosted Ubuntu agent's Chrome and ChromeDriver, starts the pinned Mailpit container on loopback, publishes TRX results, and removes the container and temporary inbox afterward. Docker is needed only on the hosted agent, not on your Mac.

Here, `local` means local to the Azure agent. The application, SQLite memory databases, simulated payment provider, and catcher run there; development, preproduction, and production slots are not contacted. No Azure credentials, shared SQL connection, or real email recipients are required. The captured mailbox is not uploaded as an artifact.

The standalone YAML validates PRs targeting `developer` and `master`, and supports manual runs.
It does not trigger on branch pushes: Core/developer pipelines reuse the same job as a required
stage before trusted packaging/deployment. PR and feature-branch contexts omit credentialed stages;
the standalone check blocks merges only after it is made required in GitHub. See
[the Azure pipeline setup](CI-Azure-DevOps.md#self-contained-selenium-pipeline).

The shared job verifies that both TRXs contain complete passing results with consistent counters
and unique display names. In hosted run 2412, all 169 cases executed, but Azure displayed 162 because
eight long-URL theory arguments had identical truncated names. Distinct scenario labels fix the
collision; a read-only report check prevents silent result loss. No captured inbox is published.

## Optional: separately running SQL-backed local application

Set `SeleniumTest__Local__SelfContained=false` in the test terminal to retain the external-local workflow below. It requires a separately started application and matching SQL connection. Do not use this setting for the self-contained commands above.

With this setting, `local` runs only the original 20 shared cases. The runner excludes all 40 host-dependent cases; selecting them directly fails an ownership guard before fixture mutations.

Provision one dedicated application test database and initialize its schema through the repository's normal `ApplicationDbContext` migrations. Its name must end in `_selenium`. Do not use the shared staging or production database, and do not copy real donors or credentials into a test fixture. See the context/migration information in [CI-Azure-DevOps.md](CI-Azure-DevOps.md). If using `scripts/apply-database-migrations.ps1`, pass the test connection explicitly and use `-SkipInfrastructure`.

`LocalMailpit` enables `LocalEmailCapture:UseInMemoryInfrastructure` to reuse the application's existing Development SQLite `:memory:` infrastructure database. No infrastructure SQL database or migrations are required for this profile, and inherited infrastructure SQL connection strings are ignored. The existing `Tenant-Override` name, domain, invoicing strategy, and payment strategy settings seed the local tenant. Infrastructure data is temporary; the application SQL database remains persistent.

Set these values in the application terminal. Replace the connection-string placeholder with credentials for the dedicated application test database; do not commit its value:

```bash
export ConnectionStrings__DefaultConnection='<connection string for alimentestaideia_selenium>'
export LocalEmailCapture__ExpectedDatabase=alimentestaideia_selenium

dotnet run --project BancoAlimentar.AlimentaEstaIdeia.Web/BancoAlimentar.AlimentaEstaIdeia.Web.csproj --launch-profile LocalMailpit
```

The profile binds to `https://localhost:5051` and `http://localhost:5050`. Trust the normal .NET development certificate before using Chrome over HTTPS. Configure any other required application settings for this isolated environment; do not start with a shared database as a fallback.

Startup rejects capture outside Development, non-loopback application URLs, and database names that do not match the explicit isolated names. Tenant database access is checked again before opening a SQL connection. SMTP settings supplied by a tenant cannot replace the local catcher transport. Capture never falls back to a real SMTP provider if Mailpit is unavailable.

Capture mode disables Application Insights telemetry, even if credentials are inherited, so emailed reset tokens are not exported in request URLs. The launch profile also suppresses informational hosting request logs. Local capture refuses pending tenant migrations instead of applying them on the first request; prepare the schema explicitly before running tests.

Other launch profiles and Azure slot settings are unchanged. Choose `LocalMailpit` explicitly when running local email tests.

If you explicitly disable `LocalEmailCapture:UseInMemoryInfrastructure` while capture remains enabled, you must configure a dedicated infrastructure SQL connection and matching `LocalEmailCapture:ExpectedInfrastructureDatabase`, also ending in `_selenium`. Shared infrastructure databases are still rejected in that mode.

## Run password-reset tests against the external local application

In the test terminal, configure the matching local verification database and site:

```bash
export SeleniumTest__Local__BaseUrl=https://localhost:5051
export SeleniumTest__Local__SelfContained=false
export SeleniumTest__Local__VerificationConnectionString='<same isolated tenant database connection>'
export SeleniumTest__Local__ExpectedVerificationDatabase=alimentestaideia_selenium
export SeleniumTest__Local__MailpitBaseUrl=http://127.0.0.1:8025

bash scripts/test-selenium.sh local-email
```

Password-reset cases register unique test users, read their confirmation/reset messages through the Mailpit API, follow only links matching the configured local origin and expected Identity page, and verify login. They never reset the shared site-test or admin account password. Cleanup is scoped to the created users and their captured messages; it never clears the entire inbox or removes financial records. An interrupted run can leave test fixtures for inspection.

This external-local mode requires Chrome, the running isolated app, its initialized SQL schema, and Mailpit. Missing prerequisites fail clearly rather than switching to the developer slot. Both local modes verify generated emails and the reset journey, not delivery to Outlook, Gmail, or another external provider.

## Run shared referral cases locally

Self-contained mode seeds these prerequisites automatically. Only for external-local mode, configure `SeleniumTest:Local:Site:Username` and `Password` for an existing test user in the local database. The admin case also needs `SeleniumTest:Local:Admin:Username`, `Password`, and `SeleniumTest:Local:ReferralMetricsCode`. Keep the distinguishing sample requirements in [TESTS.md](TESTS.md#referral-test-configuration).

Use environment variables with `__` instead of `:`, or the Selenium project's user secrets, then run:

```bash
bash scripts/test-selenium.sh local
```

Local credentials and connection strings never fall back to Developer or legacy shared settings.

## Run against the developer slot

Developer remains the default target for backward compatibility. Prefer explicit profile settings:

- `SeleniumTest:Developer:BaseUrl`: `https://dev.alimentestaideia.pt`.
- `SeleniumTest:Developer:VerificationConnectionString`: the matching staging SQL connection.
- `SeleniumTest:Developer:ExpectedVerificationDatabase`: `alimentestaideia.core_staging`.
- `SeleniumTest:Developer:Site:*`, `Admin:*`, and `ReferralMetricsCode`: the existing staging fixtures.

Legacy unscoped Selenium settings remain accepted for Developer only. The expected staging catalog and approved dev host are validated before Chrome starts.

```bash
bash scripts/test-selenium.sh developer
```

This selection excludes local email-dependent tests. The developer slot continues to use its existing SMTP configuration, and its sandbox payment cases can create real staging donation/payment/invoice records. A Mailpit instance on your computer does not capture emails sent by Azure.

## Build and discover without running browser cases

```bash
bash scripts/test-selenium.sh offline
bash scripts/test-selenium.sh local-email --list-tests
bash scripts/test-selenium.sh developer --list-tests
```

Discovery does not construct browser fixtures or connect to SQL/Mailpit. Do not run the entire Selenium project without a category filter: local email cases and developer payment cases intentionally require different targets.

## Stop local capture

For native Mailpit, press Ctrl+C in its terminal. For Docker:

```bash
docker compose -f compose.mailpit.yml down
```

This removes the local catcher container and its temporary captured messages. It does not remove your test SQL databases. Message deletion is not reversible, so inspect any needed messages first.
