# Implementation plan

Status: implementation, independent review, and final validation complete. Changes are uncommitted.

## Dependency order

Approve scope -> failing regression tests and host controls -> scenario groups ->
full local validation -> independent review -> documentation and handoff.

## Tasks

- [x] Inspect existing browser cases, local host, runner, image-edit behavior,
  password-reset handling, and testing/payment documentation.
- [x] Record specification and architecture guardrails.
- [x] Obtain approval for this plan.
- [x] Delegate bounded regression-test work to an SDET; preserve disjoint file
  ownership and keep host integration on the main implementation path.
- [x] Add owned image storage and deterministic token-expiry support to the test
  host, with integration tests for isolation and disposal.
- [x] Add local provider simulation using the existing payment boundary and the
  actual payment-confirmation/email pipeline. Prove it cannot contact providers.
- [x] Add ownership, forged-edit, and ordinary-user admin-denial Selenium cases.
- [x] Add campaign edit, activation lifecycle, and public-setting Selenium cases.
- [x] Add real upload, replacement, removal, unsupported-type, and oversized-image
  Selenium cases. First demonstrate failures before fixing image preservation.
- [x] Add admin sorting in both directions, deterministic ties, pagination, empty
  results, pending-only donations, no donations, and zero-opening cases.
- [x] Add expired/missing/malformed/cross-account reset-token cases. First
  demonstrate failures before any required safe-error handling fix.
- [x] Add referral donation-to-email and admin-details referral-link scenarios.
- [x] Add narrow-viewport and keyboard-only creation, editing, duplicate-warning,
  and password-reset scenarios.
- [x] Update local runner filtering and verify host-only cases are excluded from
  external developer runs. Preserve existing 20 local browser cases and 7 live
  sandbox payment cases.
- [x] Run targeted new cases, all local browser cases, offline helpers, and the web
  integration suite. Start loopback Mailpit only if not already running, and stop
  only the instance started for this work.
- [x] Request independent review against the specification; address findings and
  repeat affected tests.
- [x] Update `Documentation/Local-Email-Tests.md` and `Documentation/TESTS.md` with
  actual discovered counts, the scenario inventory, commands, SMTP-port override,
  and limits of local simulated payment coverage.
- [x] Record measured results and any remaining limitations. Leave all work
  uncommitted; do not push or create a PR.

## Expected file scope

- Selenium project: new focused partial test/support files and minimal shared-helper
  changes, plus runner-selection helper tests where practical.
- Web.TestHost: isolated file storage, token expiry, and payment simulation support.
- Web.IntegrationTests: host isolation/expiry/payment-boundary regression checks.
- Web: narrow image-edit preservation, malformed reset-token handling, and
  pagination accessibility-attribute fixes demonstrated by the new tests.
- Documentation and `scripts/test-selenium.sh`.

Do not modify `Web/appsettings.Development.json`, deployment settings, migrations,
or unrelated existing edits.

## Validation

Run from the repository root:

```bash
dotnet build BancoAlimentar.AlimentaEstaIdeia.Web.SeleniumUITest/BancoAlimentar.AlimentaEstaIdeia.Web.Selenium.UITest.csproj --configuration Release
bash scripts/test-selenium.sh offline
SeleniumTest__Headless=true bash scripts/test-selenium.sh local
dotnet test BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests/BancoAlimentar.AlimentaEstaldeia.Web.Integration.Tests.csproj --configuration Release
bash scripts/test-selenium.sh developer --list-tests
git diff --check
```

Do not execute live developer payment cases as part of this task. Report failures,
environmental prerequisites, and any checks that could not be run explicitly.

## Regression evidence and implementation notes

- Before the image fix, both browser rejection cases failed because the previously
  uploaded file was deleted. The replacement now uploads successfully before the
  old image is removed.
- Before the reset fix, the malformed-base64 browser case returned HTTP 500. It now
  returns a safe HTTP 400; missing/empty codes are also rejected.
- Pagination tests exposed Razor rendering boolean `aria-disabled` values as the
  attribute name. The page now emits explicit `true`/`false` strings.
- Initial targeted validation: 38 new lifecycle/admin/reset cases passed after
  those fixes; 109 offline helpers passed; the first full integration run passed
  296 cases before adding the final real-SDK contract case.
- Review added missing anonymous public/private statistics assertions and a
  rejecting PayPal transport, including a direct callback isolation test.
- The payment simulator retains the actual generated SDK, webhook verifier,
  donation completion, and SMTP mail composition. Its contract test caught
  missing required response fields before final validation.
- The local host enables the existing subscription-form feature so its required
  frequency field is rendered. Simulated donation cases remain one-time payments;
  no production donation model or feature settings were changed.
- Reused the already running loopback Mailpit process. Do not stop it at handoff.
- Existing `appsettings.Development.json` edits remain untouched. A full-tree
  whitespace check reports its pre-existing CRLF/trailing whitespace; validate
  this task's files separately without rewriting the user's configuration.
- Independent final review: no remaining actionable findings. Final integration
  validation: 297 passed, including all nine new host-contract cases. Final
  offline validation: 109 passed. Discovery: 60 local / 19 developer cases.
- Repeated full browser runs exposed one narrow detached-node response while
  polling a replaced form. The shared helper now treats that specific Chrome
  response as staleness and still verifies destination and persisted state.
- Overlapping browser runs produced a ChromeDriver port collision. Final browser
  validation must run alone; the setup guide documents serial execution.

## Final results — October 27, 2026

- Release Selenium build: zero warnings and zero errors.
- Standalone local Chrome suite: **60 passed, 0 failed, 0 skipped**, approximately
  191 seconds. This includes the original 20 cases and all 40 new cases.
- Offline helper suite: **109 passed**, including 14 new mail-reader and runner
  selection cases.
- Full web integration suite: **297 passed**, including nine new host-contract
  cases for storage, Identity expiry, invalid links, and payment boundaries.
- Final independent review and the subsequent navigation/authorization audit:
  no remaining actionable findings.
- Developer discovery: **19 cases**, unchanged. Developer browser cases were not
  executed, and none of the 40 host-only cases are selected for that target.
- Scoped tracked-file and new-file whitespace checks: no whitespace errors in
  this task's files. The user's development configuration remains untouched.
- Final successful Chrome report:
  `BancoAlimentar.AlimentaEstaIdeia.Web.SeleniumUITest/TestResults/local-selenium-expanded-serial.trx`.
- Final integration report:
  `BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests/TestResults/local-browser-expansion-integration-final.trx`.

No commits, pushes, PRs, Azure slot changes, SQL migrations, or live payment-provider
transactions were performed. The existing Mailpit process remains running.

## Developer branch sync — 2026-10-27

- Fast-forwarded `codex/selenium-test-infrastructure` from `1c822533` to
  `origin/developer` at `51a60523`, incorporating five upstream commits.
- Retained the original work in recovery stash
  `5d0f20f63a9719a88433fe17102e58d05da8bedd` and the original HEAD in
  `codex/selenium-before-developer-sync`. The reconciled work remains unstaged
  and uncommitted; all 36 originally untracked files are present.
- Resolved the Startup conflict by moving local database safeguards into
  `PersistenceServiceCollectionExtensions`. Register telemetry suppression before
  Application Insights 3 builds its SDK, and disable Live Metrics after the root
  options setup. Host assertions verify both settings and `OTEL_SDK_DISABLED`.
- Adapted the telemetry test to OpenTelemetry with an in-memory exporter and
  an HTTP transport that cannot access the network. Selenium 4.51 nullable
  results now use explicit type and required-attribute assertions.
- Original `appsettings.Development.json` contents are byte-for-byte unchanged
  and already match upstream, so that file has no pending diff.
- Final Release solution build: zero warnings and zero errors.

Final regression results, with overlapping targeted runs excluded:

| Suite | Passed |
|-------|--------|
| Local headless Selenium | 60 |
| Offline Selenium helpers | 109 |
| Web integration | 301 |
| Repository | 272 |
| Tenant core | 13 |
| Functions | 49 |
| Total | 804 |

All final suites completed without failures or skips. The final browser report is
`BancoAlimentar.AlimentaEstaIdeia.Web.SeleniumUITest/TestResults/developer-sync-local-final.trx`;
the final integration report is
`BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests/TestResults/developer-sync-integration-final.trx`.
Developer discovery still selects 19 cases; those deployed-site cases were not run.
The incoming migration files were checked out but no migrations were applied.
No commit, push, Azure pipeline run, or deployment was performed. The existing
native Mailpit instance remains running.
