# Local Selenium regression coverage

## Goal

Extend the existing browser suite with all seven high- and medium-priority groups
approved for planning by the user on October 27, 2026. Exercise real page navigation,
forms, JavaScript, Identity, persistence, and captured email without requiring Docker,
SQL Server, Azure credentials, or real payment transactions.

## Acceptance criteria

1. Campaign ownership and authorization:
   - A second authenticated account cannot view or edit another account's campaign
     by changing the campaign ID, including a forged edit submission.
   - A normal authenticated account cannot access the admin referral list.
   - Rejected requests preserve campaign ownership and values.
2. Campaign editing:
   - Saving a changed name and tagline persists after reload and updates the public
     referral presentation.
   - Active/inactive/reactivated campaigns have the expected public-link behavior
     and opening counts.
   - Public/private settings persist and affect public listing where applicable.
3. Donation attribution and confirmation:
   - A donation started through a referral retains that referral through locally
     simulated payment confirmation.
   - The real confirmation email captured by Mailpit contains the campaign name in
     the thank-you sentence and retains the remainder of the payment message.
   - Admin donation details link to the correct referral details.
   - The simulation never sends a real payment request or bypasses production
     attribution, payment-processing, or email-composition code under assertion.
4. Campaign images:
   - A valid image can be uploaded, displayed, replaced, and removed through the UI.
   - Unsupported and oversized uploads show feedback without losing the existing
     usable image or persisting a replacement.
5. Admin list edge cases:
   - Each supported sort column works in both directions with deterministic ties.
   - Next/previous pagination retains sort settings and neither duplicates nor
     omits fixture rows.
   - Empty lists, referrals without donations, pending-only donations, and zero
     openings display correct counts, amounts, dates, and safe ratios.
6. Password-reset security:
   - Expired, missing, and malformed tokens fail safely without changing passwords.
   - A valid token for one account cannot reset another account's password.
   - Both accounts remain usable with their expected passwords after rejection.
7. Mobile and keyboard navigation:
   - Campaign creation, editing, and password-reset forms work at a narrow viewport.
   - Duplicate-code feedback is automatically visible on mobile.
   - Representative forms can be completed and submitted using keyboard navigation,
     including their validation and feedback paths.

## Safety and non-goals

- Use synthetic identities, disposable databases, owned temporary files, and
  recipient-scoped Mailpit cleanup. Preserve unrelated work and local settings.
- New tests that require host control run only against the self-contained local
  target. Existing developer-slot selection and Azure settings remain unchanged.
- Do not assert that SQLite proves SQL Server migrations or provider-specific SQL.
- Do not add live charges, external inbox access, OAuth integration, a full
  accessibility audit, CI deployment, commits, pushes, or a pull request.
- Fix narrowly scoped application defects exposed by these cases when needed;
  broader behavior changes require separate approval.
