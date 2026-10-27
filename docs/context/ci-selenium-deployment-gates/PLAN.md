# Approved implementation plan

Approval: user approved repository work for steps 1–3 on 2026-10-27. No additional plan approval is required for this scope.

## Tasks

- [x] Read repository/deployment documentation; inspect existing triggers and prior Azure evidence.
- [x] Fetch remote refs and select baseline; create codex/ci-selenium-deployment-gates from 18727f5b (depends on unmerged PR #853).
- [x] SDET: add configuration regression tests and confirm they fail before implementation (16 policy failures; 32 reporting failures).
- [x] Extract the validated Linux Selenium job into a shared, secret-free template.
- [x] Enable GitHub/Azure PR validation for developer and master, documentation-only feedback, and a stable aggregate GitHub status.
- [x] Omit credentialed stages for PR/non-trusted refs; add explicit same-revision Selenium dependencies and strict runtime guards.
- [x] Preserve artifact/release contracts and avoid duplicated automatic standalone push runs.
- [x] Diagnose and correct the seven-result TRX publishing discrepancy: 109 raw results/IDs but only 102 distinct names; prepend short theory labels and reject duplicate-name TRXs read-only.
- [x] Update operational/test documentation, including external settings follow-up and remaining steps 4–5.
- [x] Run policy tests, YAML/Bash checks, .NET build/helper/browser validation appropriate to the changed scope.
- [x] Independent code review completed with no findings.

## Validation results

- `ruby scripts/ci/tests/pipeline_safety_test.rb`: 17 tests, 10,521 assertions; all passed. Covers branch/reason and failure-state truth tables plus the actual GitHub aggregate shell.
- `ruby scripts/ci/tests/test_result_reporting_test.rb`: 54 tests, 926 assertions; all passed. Includes the 109-row/102-name regression, incomplete/failing reports, identity consistency, sanitized diagnostics, and input checksum preservation.
- Selenium project Release build: zero warnings/errors.
- Offline selection: 109 passed, zero failed/skipped; new TRX has 109 distinct result names.
- Headless self-contained browser selection: 60 passed, zero failed/skipped; duration 3m16s.
- Read-only verifier accepted both new reports and rejected the original Azure run 2412 report with colliding display names. Reports retained in ignored `BancoAlimentar.AlimentaEstaIdeia.Web.SeleniumUITest/TestResults/ci-gates.xoOo0v/`.
- Official checksum-verified actionlint 1.7.12: GitHub workflow passed (external ShellCheck/Pyflakes integrations disabled); inline Bash syntax checks and `git diff --check` passed.
- Azure dedicated preview endpoint with `previewRun=true`: nine non-executing previews passed using locally inlined templates. Standalone feature ref compiled; developer feature/PR/master refs omitted credentialed stages; developer trusted ref included all gates; Core feature/PR refs omitted packaging and trusted developer/master refs included gated packaging.
- Preview limitation: Azure predefined Build.Reason is read-only and remains Manual during manual preview; do not claim to have simulated an actual PR event by overriding it. Actual PR-shaped refs were previewed; the reason truth table is covered locally. New remote template fetching/publication and actual PR check reporting still need hosted verification after publishing this branch.
- Temporary Mailpit instance was started only for this validation and stopped afterward; its inbox and scratch tool/report downloads were cleaned up. No deployed application, cloud migration, service connection, environment approval, or branch-protection change was executed.

## Dependencies and completion

Tests precede implementation. Shared template precedes pipeline wiring. Pipeline wiring precedes documentation and review. No cloud deployment, migration, permission change, ruleset change, or PR merge is performed during validation. Remote hosted validation requires an explicitly safe test-only run after publication; report it as pending if not executed.
