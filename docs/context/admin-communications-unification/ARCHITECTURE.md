# Architecture

- Keep both existing persistence models and merge projected rows in the admin page model.
- Project each source into a shared admin row view model with a stable row key, timestamp, subject, user, payment, donation, and communication type.
- Filter each source before materializing where the filter can be translated by Entity Framework; merge, classify, sort, and paginate the shared rows in memory.
- Exclude payment-linked `EmailCommunication` rows from the general source to avoid duplicate display with `PaymentNotifications`.
- Keep classification in a small pure helper or enum-backed method so it can be unit tested without a database.
- Use existing `AdminSharedResources` localization and Font Awesome classes already used by the admin navigation.
- Keep the legacy `/Admin/PaymentNotifications` endpoint available unless the user explicitly requests removal; remove its admin navigation item only if the merged page is the sole intended entry point.
