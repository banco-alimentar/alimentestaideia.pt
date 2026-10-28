# Architecture

## Baseline

Branch codex/ci-selenium-deployment-gates starts from validated commit 18727f5b. PR #853 remains open against developer; origin/developer does not yet include its two Selenium commits. Keep this dependency explicit rather than reverting to a baseline without the test infrastructure.

## Pipeline structure

- GitHub Build and Test: PRs to developer/master, pushes to these branches, and manual dispatch; read-only repository token; no workflow-wide path exclusions. Add an always-running aggregate status that fails unless build and test jobs succeed.
- Standalone Azure Selenium: PRs to developer/master and manual queue only. Use the same shared Ubuntu 24.04 job as packaging/deployment pipelines.
- Shared job: azure-pipelines/templates/selenium-job.yml; job-local .NET/test-result variables; checkout without persisted credentials; .NET 10, headless Chrome, loopback Mailpit, offline and self-contained selections, bounded readiness, always-run cleanup and result publication. No Azure credentials or Windows build artifacts.
- Developer/Core: first stage BrowserRegression. Compile-time branch/reason guards omit credentialed stages outside approved non-PR branch builds. Runtime conditions repeat these guards and explicitly require exact Succeeded dependency results, so failed, skipped, canceled, or SucceededWithIssues validation cannot release artifacts/deploy.
- Developer: Build depends on BrowserRegression; web deployment depends on Build and BrowserRegression; function deployment depends on successful web deployment and both validation/build stages. Explicit pr: none; accidental manual PR builds remain test-only.
- Core: credentialed packaging Build depends on BrowserRegression; approved refs are refs/heads/developer and refs/heads/master. Explicit pr: none; manual feature/PR builds remain test-only.
- Keep preprod-release.yml unchanged. Database migrations still precede Zip Deploy. Smoke tests and promotion policy are deferred.

## Validation

Use Ruby standard-library YAML/Minitest policy tests on the hosted Ubuntu agent and local machine, without adding application package dependencies. Validate template structure and branch/reason/failure truth tables; do not claim local policy evaluation is an Azure service compilation.

Investigate raw TRX identities before changing result publication. Any report normalization must preserve outcomes, diagnostics, timestamps, and original execution totals; it must never convert failures to successes or mask missing results.

## External settings

Existing pipeline UI trigger overrides and classic releases can bypass repository trigger intent. Document the required external review. YAML is not a security boundary against a contributor who can edit it; service-connection/environment branch control, approvals, protected refs, and trusted templates remain administrative follow-up.
