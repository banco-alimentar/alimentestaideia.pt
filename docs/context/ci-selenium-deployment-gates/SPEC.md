# CI validation and deployment safety

Approved scope: repository implementation of steps 1–3, 2026-10-27.
Related issue: GitHub #848. This repository does not use Jira.

## Outcomes

- Pull requests targeting developer or master receive GitHub build/test and Azure Selenium results, including documentation-only changes.
- Pull-request and feature-branch runs do not retrieve Azure Key Vault secrets, publish release artifacts, or deploy application/function slots.
- Developer deployment is permitted only for successful non-PR developer branch builds.
- Core packaging is permitted only for successful non-PR developer/master branch builds.
- All packaging and developer deployment depend on successful isolated browser regression on the same source revision.
- The headless test job is shared rather than duplicated, with standalone PR/manual execution and no redundant standalone push execution.
- Existing release artifact names, migration order, slot names, and deployment methods are preserved.
- Test reports accurately represent executed cases; investigate the 109 executed versus 102 published offline results in Azure run 2412.

## Boundaries

No GitHub protection/ruleset edits, Azure permissions/approval edits, live deployments, database migrations, production promotion, or deployed-slot smoke testing are authorized here. YAML guards prevent accidental execution; Azure resource restrictions remain a separate administrative security boundary.

## Verification

Add failing configuration regression tests before implementation. Check PR, trusted branch, feature branch, failed/skipped/canceled Selenium, documentation-only, cleanup, and missing-results scenarios. Run local policy/helper/browser tests and independent review. Report hosted validation separately; do not queue a deployment-capable pipeline to validate changes.
