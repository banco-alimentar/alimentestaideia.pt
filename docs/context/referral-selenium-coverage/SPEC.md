# Referral Selenium coverage

## Goal

Extend the existing live Chrome suite with referral campaign regressions, especially behavior that HTTP integration tests cannot verify: client-side validation, scrolling, and loaded QR images.

## User journeys

1. An authenticated user creates a unique campaign and sees its campaign card, working referral link, and loaded QR image. SQL contains one campaign owned by that user with a real creation date.
2. Submitting an active campaign code, including a differently cased code, displays the duplicate warning without creating another campaign or changing its owner. After the response loads, the warning scrolls into the viewport without an extra test-driven scroll.
3. Empty, too-short, and non-alphanumeric campaign codes produce visible client-side validation feedback and create no campaign.
4. Opening an active referral redirects to the donation page, displays its configured campaign information, and records exactly one opening and opening event.
5. Opening an inactive referral shows the inactive message and records no opening.
6. Opening an unknown referral shows the inactive message and does not create a referral or an opening event for the requested code.
7. Anonymous visitors to campaign history or the admin referral list are redirected to login.
8. An administrator sees referral images in the first column and SQL-consistent paid donation counts, paid amounts, last paid donation dates, opening counts, and opening-to-paid ratios. Pending donations do not contribute to paid metrics.

## Safety and scope

- Use the deployed dev site and its matching staging database, never production.
- New referral tests do not submit payment requests or send payment confirmation emails.
- Use unique test-owned campaign data and remove only those campaign records and their opening events after each test, provided no donations are associated.
- Do not alter or remove existing campaigns or financial records. Admin metrics use a configured, stable staging sample with paid and pending donations.
- Fail clearly when required credentials, admin access, or sample data are missing; do not report a test as passed without exercising its behavior.
- Preserve the existing seven payment tests and unrelated local development settings.
- This change adds tests and documentation only. Product behavior, CI pipelines, and deployment configuration are out of scope.

## Verification

Build the Selenium project and list its tests without launching Chrome or connecting to staging. Live execution is a separately requested validation step because it writes temporary staging campaign data. Do not describe discovery or compilation as a passing live test run.
