# Architecture

## Baseline

Branch codex/ci-selenium-deployment-gates starts from validated commit 18727f5b. PR #853 remains open against developer; origin/developer does not yet include its two Selenium commits. Keep this dependency explicit rather than reverting to a baseline without the test infrastructure.

## Pipeline structure

- Consolidated Azure developer-debug: PRs targeting developer/master and pushes to both branches with a C# change, plus manual queueing. BrowserRegression contains the Release solution build, all five .NET test projects with TRX/coverage, then the shared Selenium job (which also runs after .NET failures). The former standalone Selenium YAML is removed; retire its Azure definition and require the consolidated PR status on both branches.
- DependencyAudit: independent stage in developer-debug, with explicit empty dependsOn; runs weekly on master and on manual queues. Scheduled runs skip validation and omit all credentialed stages.
- Shared job: azure-pipelines/templates/selenium-job.yml; job-local .NET/test-result variables; checkout without persisted credentials; .NET 10, headless Chrome, loopback Mailpit, offline and self-contained selections, bounded readiness, always-run cleanup and result publication. No Azure credentials or Windows build artifacts.
- Developer/Core: first stage BrowserRegression. Compile-time branch/reason guards omit credentialed stages outside approved non-PR branch builds. Runtime conditions repeat these guards and explicitly require exact Succeeded dependency results, so failed, skipped, canceled, or SucceededWithIssues validation cannot release artifacts/deploy.
- Developer: Debug packaging Build depends on the combined .NET/Selenium BrowserRegression stage; web deployment depends on Build and BrowserRegression; function deployment depends on successful web deployment and both validation/build stages. Credentialed stages exist only for trusted developer CI/manual runs, never PRs, master, tags, feature refs, or schedules.
- Core: credentialed packaging Build depends on BrowserRegression; approved refs are refs/heads/developer and refs/heads/master. Explicit pr: none; manual feature/PR builds remain test-only.
- Keep preprod-release.yml unchanged. Database migrations still precede Zip Deploy. Smoke tests and promotion policy are deferred.

## Validation

Use Ruby standard-library YAML/Minitest policy tests on the self-hosted Default Linux agent and local machine, without adding application package dependencies. Validate template structure and branch/reason/failure truth tables; do not claim local policy evaluation is an Azure service compilation.

Investigate raw TRX identities before changing result publication. Any report normalization must preserve outcomes, diagnostics, timestamps, and original execution totals; it must never convert failures to successes or mask missing results.

## External settings

Existing pipeline UI trigger overrides and classic releases can bypass repository trigger intent. Document the required external review. YAML is not a security boundary against a contributor who can edit it; service-connection/environment branch control, approvals, protected refs, and trusted templates remain administrative follow-up.
