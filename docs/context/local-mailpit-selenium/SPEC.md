# Local email capture and Selenium targets

## Goal

Use an email catcher for all actual email emitted by the isolated local test application, add browser coverage for forgot-password journeys, and allow explicit Selenium runs against either localhost or the developer slot.

## Required behavior

1. Local email capture stores outgoing messages in Mailpit rather than delivering to real recipients. Both Identity emails and donation emails use the same local SMTP destination.
2. Selenium has separate Local and Developer configurations. One invocation targets one environment; it does not silently run against both.
3. Shared browser journeys can run against either environment when their data prerequisites are satisfied. Existing sandbox payment journeys stay in the developer-run selection by default.
4. Local forgot-password tests submit requests through the browser, retrieve the real generated email from Mailpit, open the reset link, and verify authentication afterward.
5. Cover empty and invalid email addresses, unknown-email confirmation behavior, successful reset, weak and mismatched passwords, invalid reset tokens, and reuse of an already consumed token.
6. Test-only users are unique to a run. Never reset the ordinary site-test or admin account password.
7. Existing unit and in-process integration tests retain their email stubs. Those tests do not send actual emails; dedicated email-delivery tests use Mailpit.
8. The `LocalMailpit` profile uses the existing Development in-memory SQLite infrastructure database, even if an infrastructure SQL connection is inherited. Only the application database requires a dedicated SQL test catalog. Other profiles retain their existing infrastructure selection.
9. Local browser runs default to a self-contained Development host with test-owned SQLite in-memory application and infrastructure data, automatically seeded users/campaigns, an ephemeral loopback HTTP port, and real Mailpit capture. No SQL database provisioning, external application process, cloud secrets, or development certificate is required. The separately launched `LocalMailpit` SQL profile remains available as an explicit external-local option.
10. Verify application-model compatibility, unique referral codes, relational constraints, paid-only metrics, and real Identity flows with the browser host. Clearly distinguish this coverage from SQL Server migration/provider verification.

## Isolation and safety

- Capture is restricted to an explicit local test profile. Production, preproduction, and developer-slot email behavior remain unchanged.
- Local browser tests use an explicitly configured isolated test database, not production or the shared staging database. Developer tests keep the existing staging database guard.
- Local email-dependent cases are excluded from developer test commands unless a separate remote catcher setup is explicitly designed and approved later.
- Do not expose Mailpit publicly, enable relaying, or log message bodies, passwords, or reset URLs containing tokens.
- Scope mailbox searches and cleanup to unique test recipients and messages created by the current run. Never clear a shared inbox globally.
- Preserve existing referral tests and unrelated local settings. Do not commit, push, deploy, change Azure slot settings, or run live staging cases as part of this task.

## Verification

Build and discover both test selections, test configuration and mailbox parsing with local fixtures, and run existing affected non-live tests. End-to-end local execution requires an available local app, isolated database, Chrome, and Mailpit; report missing prerequisites rather than using staging as a fallback.
