# Admin communications unification

## Goal

Provide one admin communications list that includes general, identity, and payment-related email notifications.

## Requirements

- Show non-payment `EmailCommunication` records and `PaymentNotifications` records in `/Admin/Communications`.
- Do not show the payment-linked `EmailCommunication` audit row when the matching payment notification is already represented by `PaymentNotifications`.
- Clearly identify each row as Payment, Login, or Other.
- Use distinct accessible icons and text labels for each communication type.
- Filter by communication type, date range, and subject.
- Preserve existing search, sorting, pagination, user-name display, payment links, donation links, and subject display.
- Keep existing database tables and public/user-facing pages compatible.
- Preserve localized UI text for the supported resource languages.

## Classification

- `PaymentNotifications` records are Payment.
- Non-payment email audit records whose subject matches an identity login/security subject are Login.
- Remaining non-payment email audit records are Other.
