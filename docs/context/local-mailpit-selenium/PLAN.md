# Local email catcher and Selenium target plan

Status: self-contained Development browser-host follow-up complete, including live local browser verification and independent review. Local runs no longer require SQL database provisioning. Original SQL-backed LocalMailpit and Developer workflows remain available.

## Approved follow-up: self-contained Development browser host

The user approved implementing the proposed self-contained browser host and application-model compatibility.

- [x] Write failing regression tests for SQLite model compatibility, real loopback host/Identity behavior, fixture isolation, and provider selection.
- [x] Implement the test-only Kestrel/SQLite host, safe local configuration, synthetic tenant and automatic fixtures; keep production model/provider settings unchanged.
- [x] Adapt Selenium Local lifecycle, direct verification, cleanup guards, generated credentials, and categories. Retain explicit external-local and Developer modes.
- [x] Document one-command local browser execution with Mailpit and SQL Server coverage limitations.
- [x] Run affected suites, local browser cases where Chrome/Mailpit are available, and independent review. Do not commit, push, provision SQL/cloud resources, or run shared-slot tests.

### Self-contained verification results

- The initial host-contract test failed before implementation. Real HTTP/SQLite regressions then exposed the Development tenant registration and unavailable OAuth schemes; both were corrected in the test host, preserving role requirements.
- Full integration suite: 288 passed, including 22 real loopback host, relational constraint, Identity, authorization, ownership/isolation, disposal, and local-asset cases.
- Offline Selenium helpers: 95 passed (44 configuration and 51 Mailpit-reader cases).
- All 20 local Chrome cases passed twice after completing registration fixtures and fixing subpixel visibility assertions. The final documented runner invocation was `SeleniumTest__Headless=true bash scripts/test-selenium.sh local --no-restore --verbosity minimal -m:1 -nr:false` (20 passed in 64 seconds). Total distinct passing cases: 403.
- Test-only static routing serves bundled Identity validation scripts and returns missing assets as 404 before tenant Azure fallback. No production static routing, model, migrations, or OAuth configuration was changed for this follow-up.
- NuGet connectivity warnings from the initial sandbox restore cleared after a successful network-enabled forced metadata refresh. Final builds completed without compiler/analyzer warnings. Scoped whitespace, Bash syntax, configuration parsing, and independent review passed.
- A temporary installed native Mailpit instance was started on loopback for verification and stopped afterward. No shared SQL database, Azure slot, external mailbox, or payment provider was used. User development settings remained byte-for-byte unchanged; no commits or pushes were performed.

## Approved follow-up: in-memory infrastructure

The user explicitly approved using the existing Development in-memory infrastructure database for `LocalMailpit`.

- [x] Add regression tests and confirm the missing-infrastructure case fails before implementation, then passes after the change.
- [x] Add the root-only capture switch, enable it in the launch profile, and reuse the existing SQLite registration despite inherited SQL settings.
- [x] Preserve application SQL isolation, Development-only capture, and existing non-capture behavior.
- [x] Update the setup guide for native Mailpit and one dedicated SQL application database.
- [x] Run affected tests and independently review the follow-up. Do not provision a database, run shared-slot browser tests, or modify user development settings.

### Follow-up verification

- Missing-infrastructure regression: failed before implementation and passed afterward.
- Capture regression suite: 74 passed, including actual SQLite in-memory registration/tenant seeding despite inherited SQL settings, preserved non-capture SQL registration, application SQL isolation, root-only switches, environment restrictions, and SMTP capture.
- Full web integration suite: 266 passed. Offline Selenium helper suite: 76 passed. Total distinct tests executed for this follow-up: 342.
- Release web build: 0 warnings and 0 errors. Launch settings JSON/profile switch and scoped whitespace checks passed.
- Independent implementation review: no findings. Standalone Startup test fixture was corrected to use the web assembly, avoid telemetry-host dependencies, and inspect SQLite's configured data source.
- User development settings remain byte-for-byte unchanged. No SQL database was provisioned or migrated; no live browser/staging tests, commits, pushes, or deployments were performed.

## Task dependencies

Approval -> target/capture configuration tests -> Local/Developer selection and Mailpit setup -> password-reset browser cases -> validation and independent review.

## Tasks

- [x] Inspect current Selenium targeting, referral database guards, SMTP implementation, Identity reset flow, and stub-based integration tests.
- [x] Record the requirement to capture all real local emails without replacing deliberate unit/integration stubs or changing Azure slot delivery.
- [x] Approve this plan.
- [x] Add regression tests for target validation, isolated database checks, effective local SMTP configuration, and Mailpit message/link selection. Delegate test work to an SDET with disjoint file ownership.
- [x] Introduce validated Local/Developer targets in the existing Selenium setup. Preserve developer defaults and make local selection explicit. Update referral fixture guards for the selected environment.
- [x] Add a loopback-only Mailpit Compose definition and local web capture profile. Preserve user-edited development settings and avoid deployment configuration changes.
- [x] Verify local capture applies to the real `IMail` SMTP path used by Identity and donation emails, even with tenant configuration. Keep tracked stubs for ordinary integration tests.
- [x] Add an HTTP Mailpit reader with bounded polling, recipient correlation, local-origin link checks, and scoped message cleanup.
- [x] Add local Selenium cases for forgot-password validation, unknown users, actual reset-email links, weak/mismatched passwords, successful reset/login, old-password rejection, invalid tokens, and consumed-token reuse. Use only unique test accounts.
- [x] Add explicit local/developer test categories or run scripts. Local email cases must not be included in developer commands. Existing payment cases remain developer-oriented by default.
- [x] Document setup, separate run commands, test database requirements, captured-email scope, and what each test selection does and does not verify.
- [x] Build, discover/filter tests, run affected non-live tests, and check formatting. Run local browser cases only if an isolated app/database and Mailpit are available; never fall back to shared staging.
- [x] Independently review target isolation, SMTP override safety, token handling, data cleanup, and preservation of existing tests. Resolve findings before handoff.
- [x] Address review findings: suppress server and browser telemetry during capture, test inherited telemetry credentials and all three layouts, and reject pending local migrations without creating a schema.

## Expected file scope

- Selenium test project: target configuration, existing setup/referral guards, new password-reset cases, and Mailpit helpers.
- Web project: local launch profile and the minimum guarded SMTP configuration change needed to guarantee capture locally.
- Tenant initialization: a local-capture-only pending-migration rejection; normal deployment behavior is unchanged.
- Shared/tenant layouts: omit browser telemetry in capture mode, preserving ordinary behavior otherwise.
- Test projects: non-live configuration/parser regression tests using existing helpers.
- Local tooling: dedicated Mailpit Compose file and optional local/developer test-run scripts.
- `Documentation/TESTS.md`: setup and environment-selection instructions.

## Local execution prerequisites

Self-contained Local runs create owned SQLite memory databases and generated fixtures automatically. Explicit external-local SQL mode still requires a separate initialized test database and matching app configuration. No connection string is assumed from user-edited development settings. A running developer slot does not satisfy the Local prerequisite. Mailpit on the developer machine cannot capture an Azure app's email unless that app is separately configured to reach it; this task deliberately does not make that remote change.

## Original verification results

- Release builds of the Selenium and web integration projects: succeeded, 0 warnings and 0 errors.
- Offline Selenium helper tests: 76 passed (25 target/configuration cases and 51 mailbox-reader cases).
- Full web integration suite: 237 passed, including 45 capture policy/SMTP/telemetry/schema/HTML cases.
- Tenant-core tests: 13 passed. Total distinct non-live tests passed: 326.
- Real mail transport regression: failed against the old disabled-email shortcut, then passed after local capture was wired into `Mail.SendMail`.
- Discovery: `local-email` selects 8 cases; `local` selects 20 cases (8 email + 12 referral); `developer` selects the original 19 referral/payment cases and no local email cases.
- Bash syntax, JSON parsing, YAML parsing, and scoped `git diff --check`: passed. Docker Compose runtime validation was not available.
- Independent review: no remaining findings after the capture privacy and migration guard fixes.
- At the original handoff, no browser tests were executed, no Azure slot settings changed, and no staging/production database was used. Docker/Mailpit were not installed then, and no isolated local app/database was started. The user has since installed and started native Mailpit. Setup is documented in `Documentation/Local-Email-Tests.md`.
- User-edited `appsettings.Development.json` remained byte-for-byte unchanged. No commits, pushes, or deployments were performed.
