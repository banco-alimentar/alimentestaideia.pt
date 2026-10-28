# Architecture guardrails

## Existing components

- Reuse `AutomatedUITests` partial classes, `ReferralTestSupport`,
  `PasswordResetTestSupport`, and `MailpitClient` instead of adding a browser framework.
- `BrowserTestApplication` owns the Development Kestrel host and SQLite memory
  databases. Keep test controls and fixture creation in the test projects.
- Retain real Identity cookies, role requirements, form antiforgery, application
  pages, and SMTP email composition. Inspect persistence through owned scoped
  contexts with fresh reads.
- Respect campaign localization resources and existing StyleCop conventions.

## Test-only dependencies

- Give image scenarios an owned temporary storage root and local asset serving.
  Exercise the real image service and upload validation without accessing Azure or
  writing uploaded files into the repository's web root.
- Keep payment-provider simulation in the test assembly's dependency registrations
  or equivalent existing test boundary. Drive the real donation UI and existing
  payment-confirmation processing; do not invent a production test endpoint.
- Use deterministic token expiry through a test-only Identity provider/clock seam.
  Preserve token binding, protection, purpose, and security-stamp validation. Do not
  weaken production token validation or add wall-clock sleeps to expire tokens.
- New host-only cases require an explicit local-self-contained category. Include
  that category in the local runner without adding it to developer-slot runs.

## Assertions and reliability

- Use explicit waits for observable navigation, UI, persistence, or inbox state.
  Do not replace keyboard actions with script submissions in keyboard scenarios.
- Assert visible feedback and data invariants, not only successful navigation.
- Use representative non-tied and tied fixture values for admin sorting. Compare
  the rendered complete order and page boundaries with independently computed
  expected values; do not repeat the production query as the sole oracle.
- Validate captured messages for the unique recipient and current scenario.
  Do not log passwords, reset tokens, or token-bearing URLs.
- Verify that host-only controls cannot be selected on external local, developer,
  staging, or production targets. Dispose resources even on assertion failures.

## Application fixes

The current image-edit handler deletes the old file before validating/uploading its
replacement. The malformed reset-token GET also decodes input without a visible
format-error guard. Add regression tests before making any required narrow fixes.
Do not refactor unrelated payment or authentication behavior.
