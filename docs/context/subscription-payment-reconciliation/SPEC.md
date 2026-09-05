# Specification: Subscription Payment Callback and Reconciliation

## Overview

Fix subscription payment processing so Easypay subscription callbacks are validated against the correct provider resources and successful payments complete the linked donation in the local system.

Provide a one-off reconciliation operation that examines every subscription-linked donation currently in `WaitingPayment`. The operation must be safe to run repeatedly, must default to a dry run, and must never create or capture a new payment.

## Goals

- Correctly validate future Easypay subscription payment callbacks.
- Move a locally linked donation to the paid state only when provider payment evidence is unambiguous.
- Repair eligible historical `WaitingPayment` subscription donations without charging donors again.
- Make callback processing and reconciliation idempotent.
- Give operators a complete report of repaired, skipped, ambiguous, mismatched, and unavailable records.

## In scope

- `subscription_capture` callback validation and processing.
- Matching Easypay subscription identity and the actual captured payment.
- Updating the linked donation, payment record, and confirmed-payment relationship atomically.
- A one-off operational reconciliation covering all subscription-linked donations whose local status is `WaitingPayment`.
- Dry-run and explicit apply modes.
- Logging, metrics, and audit output for every candidate and outcome.
- Automated tests that prove no payment-creation endpoint is called.

## Out of scope

- Changing subscription prices, schedules, or provider subscriptions.
- Issuing refunds or charging donors.
- Treating an active Easypay subscription as evidence that a historical donation was paid.
- Automatically repairing records when the provider payment ID, merchant key, amount, or payment status cannot be verified.
- A recurring background reconciliation job. The reconciliation is an explicit one-off operation.

## User and operator journeys

### Future subscription capture

1. Easypay sends a `subscription_capture` notification.
2. The application validates the subscription callback against the local subscription and Easypay subscription resource.
3. The application validates the actual payment identified by the callback using the Easypay single-payment resource.
4. The application confirms the merchant key, paid status, amount, date, and local subscription relationship.
5. The application completes the existing linked donation or creates the recurring donation record only through a charge-free persistence path when the provider capture is verified.
6. Repeated delivery of the same callback produces no duplicate payment or donation and no new provider charge.

### One-off reconciliation

1. An operator runs the reconciliation in dry-run mode.
2. The operation scans all subscription-linked donations with local status `WaitingPayment`.
3. Each record is matched to available local callback/payment evidence and verified with read-only Easypay endpoints.
4. The report classifies each record as repaired-eligible, already complete, ambiguous, mismatched, missing evidence, malformed, provider unavailable, or otherwise skipped.
5. The operator reviews the dry-run report and explicitly runs apply mode.
6. Apply mode repairs only records with the same unambiguous evidence requirements.
7. A second run makes no further changes to already repaired records.

## Safety requirements

- Dry-run is the default; applying changes requires an explicit flag.
- Reconciliation may call only read-only provider endpoints.
- Reconciliation must not call subscription creation, payment creation, capture, checkout, or charge endpoints.
- A subscription resource alone is never sufficient evidence of a paid donation.
- Amounts must match the local donation amount within the project’s established currency precision.
- Provider payment status must be paid/successful.
- Provider and local identifiers must match the expected merchant/subscription relationship.
- Ambiguous candidates must remain unchanged and be reported for manual review.
- Each repair must be atomic and safe to retry.
- Existing paid donations must never be reverted by a late or duplicate callback.
- Logs must not contain secrets or full payment credentials.

## Acceptance criteria

1. A valid future `subscription_capture` is validated using the actual Easypay payment endpoint associated with its payment ID and the subscription endpoint for subscription identity.
2. A valid capture completes the linked donation and associates the exact payment as its confirmed payment.
3. A callback with a missing, unknown, unpaid, mismatched, or amount-inconsistent payment is rejected without changing donation state.
4. Duplicate callbacks do not create duplicate donation/payment records or invoke any payment-creation operation.
5. The reconciliation discovers all subscription-linked `WaitingPayment` donations, not a hard-coded list or a fixed count.
6. Dry-run performs no database writes and no provider write operations.
7. Apply mode repairs only records with independently verified paid payment evidence.
8. Apply mode does not create a new provider charge or capture.
9. Re-running apply mode is idempotent.
10. The report includes totals and per-record identifiers, outcome, reason, and provider lookup status.
11. Tests verify successful, invalid, ambiguous, duplicate, dry-run, apply, and no-charge scenarios.

## Operational constraints and open items for review

- The Easypay subscription resource does not expose historical capture history. Historical repair therefore depends on an actual payment/capture identifier from stored callback or payment data, a provider export, or another explicitly approved evidence source.
- Records without that evidence must be reported, not guessed or marked paid.
- The operation must be run per tenant or through the existing multi-tenant execution pattern with an explicit tenant scope.
- The operator must retain the dry-run output before applying changes.
