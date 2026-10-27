# Referral Selenium test plan

Status: implementation and review complete. Release build and discovery verified; live tests were not executed.

## Dependencies

Specification and architecture approval -> test helpers and scenarios -> build and discovery -> review and handoff.

## Tasks

- [x] Inspect repository instructions, test documentation, existing Selenium helpers, referral pages, and integration coverage.
- [x] Record specification and architecture.
- [x] Obtain user approval for the test scope below.
- [x] Add referral test helpers with explicit configuration checks, staging guards, unique campaign fixtures, and narrowly scoped cleanup. Reuse the existing Selenium class rather than introducing another browser framework.
- [x] Add campaign creation and QR-image loading coverage.
- [x] Add duplicate-code cases for original and uppercase codes, asserting unchanged SQL identity/ownership and automatic warning visibility after reload.
- [x] Add invalid-code cases for empty, too-short, and non-alphanumeric values, including visible client-side feedback and no campaign insertion.
- [x] Add active, inactive, and unknown referral cases, checking redirects, displayed campaign information, and opening-event persistence as applicable.
- [x] Add anonymous access checks for campaign history and admin referrals.
- [x] Add an admin list scenario using a configured stable sample with paid and pending donations. Assert first-column image placement and SQL-consistent paid totals, last paid date, openings, and ratio.
- [x] Update `Documentation/TESTS.md` with the referral category filter, required configuration, staging side effects, and live-run prerequisites. Keep existing payment run instructions.
- [x] Build the Selenium project in Release and list tests using `--list-tests`. Do not run live cases in this implementation session.
- [x] Review changes against the specification for flaky waits, silent passes, data leakage, and unsafe cleanup. Preserve unrelated development settings and leave committing/pushing to a separate user request.
- [x] Address review feedback: require admin sample dates and formatted ratios that distinguish paid-only calculations from incorrect all-donation calculations.

## Expected file scope

- `BancoAlimentar.AlimentaEstaIdeia.Web.SeleniumUITest/AutomatedUITests.cs`: minimum helper reuse or partial-class change only.
- `BancoAlimentar.AlimentaEstaIdeia.Web.SeleniumUITest/ReferralUITests.cs`: new test cases and narrowly scoped referral helpers.
- `BancoAlimentar.AlimentaEstaIdeia.Web.SeleniumUITest/ReferralTestSupport.cs`: shared waits, staging guards, and exact-identity fixture cleanup.
- `Documentation/TESTS.md`: execution and configuration documentation.
- Test configuration placeholder keys only if needed; no secret values or package changes are planned.

## Validation commands

Run from the repository root after restoring dependencies:

```bash
dotnet build BancoAlimentar.AlimentaEstaIdeia.Web.SeleniumUITest/BancoAlimentar.AlimentaEstaIdeia.Web.Selenium.UITest.csproj --configuration Release --no-restore
dotnet test BancoAlimentar.AlimentaEstaIdeia.Web.SeleniumUITest/BancoAlimentar.AlimentaEstaIdeia.Web.Selenium.UITest.csproj --configuration Release --no-build --list-tests
git diff --check
```

Live referral execution remains unrun until separately requested. Payment completion, inbox delivery, CI pipeline repair, and changing production behavior are excluded.

## Results

- Release build: succeeded, 0 warnings and 0 errors.
- Discovery: 19 test cases total, consisting of the original 7 payment cases and 12 new referral cases.
- Filtered discovery: `Category=Referral` selects only the 12 new cases.
- Scoped `git diff --check`: passed.
- Independent review: no remaining actionable findings after the distinguishing-sample checks were added.
- No Chrome launch, staging database access, payment submission, commit, or push was performed. Existing unrelated development settings and pipeline edits were preserved.
