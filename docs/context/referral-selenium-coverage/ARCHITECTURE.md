# Referral Selenium test architecture

## Existing conventions

- The .NET 10 xUnit project is `BancoAlimentar.AlimentaEstaIdeia.Web.SeleniumUITest/BancoAlimentar.AlimentaEstaIdeia.Web.Selenium.UITest.csproj`.
- `AutomatedUITests.cs` owns Chrome, configuration, login, staging SQL access, and existing payment flows.
- The base URL is `https://dev.alimentestaideia.pt`. Existing SQL guards reject the production catalog and can require the expected staging catalog.
- Campaign creation uses `form#create_section`, `input[name='Code']`, and `#create-campaign-submit`.
- Referral visits use `/Referral/{code}`; campaign information uses `.referral-campaign-title`, `.referral-campaign-tagline`, and `.referral-campaign-image`.
- Admin rows use `/Admin/Referrals`, with the image first and paid metrics in subsequent columns. Metric calculations match the paid status used by the application, `PaymentStatus.Payed`.

## Guardrails

- Reuse existing Chrome, login, configuration, and SQL helpers. Keep new scenarios in a separate referral test source file; use a partial `AutomatedUITests` class if this is the smallest way to reuse private helpers and retain per-class serialization.
- Mark new cases with a referral category trait so they can run without the payment tests.
- Use `WebDriverWait` for DOM changes, image loading, scroll completion, and SQL persistence. Do not add fixed sleeps to the new flows.
- Check warning viewport geometry after the page reload, not merely whether the warning exists in the DOM. Do not scroll it into view from the assertion.
- Verify QR images have a data URI, are complete, and have nonzero natural dimensions; do not claim the QR content is decoded unless a decoder assertion is actually implemented.
- Use stable structural selectors and an explicit supported culture where localized text or numeric formatting matters.
- Test fixtures may create uniquely named referrals owned by the configured test account. Cleanup must match the exact tracked ID, code, and owner, check that no donation references the referral, and remove only its own opening events before the referral.
- Use read-only queries for configured admin sample donations. Require a sample containing paid and pending donations to make the paid-only assertion meaningful. Do not synthesize or delete financial records in staging.
- Read credentials from user secrets or environment variables. Document configuration key names without committing values.
- Do not change production application code to make a test pass. Report a detected product regression separately.

## Validation boundaries

The development session builds and discovers tests only. It does not run live browser cases or invoke payment providers. A reviewer checks test independence, cleanup scope, meaningful assertions, and unchanged existing payment tests before handoff.
