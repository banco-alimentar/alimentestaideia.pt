# Local Mailpit and Selenium architecture

## Existing components

- `AutomatedUITests` currently hardcodes the developer URL and verifies SQL directly.
- `ReferralTestSupport` currently requires the exact shared staging catalog, so local targeting must update its guards deliberately rather than bypass them.
- Identity `EmailSender` delegates to `IMail.SendMail`, the same SMTP implementation used by payment confirmations.
- `CustomWebApplicationFactory` uses isolated EF InMemory contexts and tracked email stubs. Preserve this fast test architecture.
- Local web configuration can be tenant-aware. A local capture profile must verify the effective SMTP destination used by `Mail`, not merely set a JSON value that a tenant override can replace.

## Design boundaries

- Replace the hardcoded browser target with validated Local/Developer configuration and a configurable base URL. Require loopback for Local and the approved dev host for Developer. Reject production URLs and mismatched database configuration before Chrome launch or fixture writes.
- Require separate explicit verification connections and expected database names for Local and Developer. Reject production and shared staging catalogs in the Local profile. Do not provision or migrate an existing database implicitly.
- Add a loopback-only Mailpit Compose service, using a verified pinned image, SMTP port 1025, and UI/API port 8025. Do not start containers during build or discovery.
- Add an explicit local web launch profile or equivalent runner configuration. Route all real local SMTP through Mailpit with credentials and TLS disabled only for loopback capture. Guard the opt-in by the Development environment and a local application address. Keep deployment defaults unchanged.
- Add a small HTTP Mailpit reader in test code using the documented API. Poll with a bounded timeout, filter by unique recipient and request time/message baseline, and parse only expected reset/confirmation links.
- Before Selenium opens an emailed link, require the configured local origin and expected Identity path. Never follow arbitrary message links or emit tokens in failures.
- Use unique test users and scoped cleanup. Prefer the application's Identity lifecycle and existing helpers; do not generate reset tokens directly in SQL or bypass the email under test.
- Add explicit categories and documented local/developer selections. Do not mark skipped or unexecuted email scenarios as passed on Developer.
- Avoid new external dependencies unless existing .NET, Selenium, and HTML helpers cannot cover the need.
- The root-only `LocalEmailCapture:UseInMemoryInfrastructure` switch selects the existing SQLite `:memory:` infrastructure registration only when capture is enabled in Development. `LocalMailpit` enables this switch. Ignore inherited infrastructure SQL settings in this mode, keep effective application SQL validation, and retain infrastructure SQL validation when the switch is disabled. Tenant overrides cannot change the switch.
- Self-contained runs use a new test-only `BrowserTestApplication` in `Web.TestHost` and .NET 10 `WebApplicationFactory.UseKestrel` on a dynamic loopback port. The host owns open SQLite memory databases for its lifetime, registers the same application context for HTTP requests and scoped verification, and seeds synthetic fixtures. Configure an isolated in-process-only tenant manager, disable cloud integrations/telemetry/hosted synchronization, and keep real Identity and `IMail` delivery through Mailpit.
- Test-only SQLite model adjustments belong in `Web.TestHost`, not production models or migrations. The existing SQL capture guards remain unchanged; the browser host overrides SQL registrations with owned memory connections and validates those actual connections before verification/cleanup. Its synthetic root SQL catalog satisfies existing capture policy but is never opened.
- Selenium Local defaults to `SelfContained=true`; explicit `SelfContained=false` retains external-local SQL behavior and guards. Developer remains SQL-backed with its approved URL/catalog. Seed credentials are generated in memory and never inherited from shared/local secrets. Dispose browser, verification scope, and owned host/database even after constructor/test failures.
- In the browser host only, retain admin policy role requirements but authenticate using the real Identity application cookie; external OAuth handlers are deliberately not configured. Serve bundled assets before tenant blob fallback, alias Identity validation-library paths to repository files, and return 404 for missing static resources without Azure storage retries. Production startup and asset routing remain unchanged.

## Relevant references

- `AGENTS.md`
- `Documentation/TESTS.md`
- `Documentation/Contributing-MacOS-setup.md`
- `Documentation/SolutionReadme.md`
- `BancoAlimentar.AlimentaEstaIdeia.Web/Areas/Identity/Pages/Account/ForgotPassword.cshtml.cs`
- `BancoAlimentar.AlimentaEstaIdeia.Web/Areas/Identity/Pages/Account/ResetPassword.cshtml.cs`
- Mailpit Docker documentation: https://mailpit.axllent.org/docs/install/docker/
- Mailpit API documentation: https://mailpit.axllent.org/docs/api-v1/
