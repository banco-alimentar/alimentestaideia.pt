# Architecture: Subscription Payment Callback and Reconciliation

## Existing boundaries

- `EasyPayGenericNotification` receives generic Easypay notifications.
- `EasyPayApiWebhookVerifier` verifies callback authenticity by querying Easypay.
- `SubscriptionRepository` currently finds subscription donations and updates subscription/payment state.
- `DonationRepository` contains the established donation-payment completion rules, including confirmed-payment assignment.
- `SubscriptionPaymentApi` exposes subscription-resource reads.
- `SinglePaymentApi` exposes single-payment reads.
- The Tools project contains operational database utilities and is the appropriate home for a one-off reconciliation command.

## Design

### Shared verification service

Introduce a focused subscription-capture verification/application service rather than duplicating provider and amount checks in the controller or command. The service should:

- Resolve the local subscription by callback transaction key.
- Verify the Easypay subscription ID and key using `GET /2.0/subscription/{id}`.
- Verify the callback payment ID using `GET /2.0/single/{id}`.
- Validate provider paid status, merchant key, requested/paid value, local donation amount, and capture date.
- Return a typed result containing the local subscription/donation context, provider payment details, and a rejection reason.

The subscription GET establishes subscription identity. The single-payment GET establishes that a particular payment was paid. Neither call should be treated as evidence beyond the fields it actually returns.

### Atomic completion

Use the existing donation completion rules, extended through a dedicated idempotent method for a verified subscription capture. The method must:

- Re-read the target donation and payment inside the write transaction.
- Detect an already-completed donation and return success without changes.
- Update the exact payment with provider ID, provider amounts, status, and completion timestamp.
- Set donation status to `Payed` and assign the exact payment as `ConfirmedPayment`.
- Create a missing local recurring donation/payment row only when a verified provider payment ID and matching subscription context exist; this is a database-only operation.
- Never call Easypay write endpoints.
- Commit all related changes atomically.

The implementation must not reuse the current `CreateSubscriptionDonationAndPayment` method unchanged because it creates a waiting donation and does not complete the payment invariant.

### Callback flow

`EasyPayGenericNotification` should remain a thin adapter:

1. Bind and validate notification shape.
2. Invoke the shared verification service for `subscription_capture`.
3. Invoke the atomic completion method for valid captures.
4. Return an appropriate acknowledgement and structured reason.

Malformed callbacks, provider lookup failures, and rejected evidence must not mutate payment state. Existing notification acknowledgement semantics should be preserved unless the provider contract requires a different response.

### One-off reconciliation command

Add a dedicated command in the Tools project with explicit parameters similar to:

- tenant/environment scope;
- `--dry-run` default behavior;
- `--apply` explicit mutation mode;
- optional subscription/donation filters for controlled investigation;
- output path or structured console output.

The query must join subscription donations to subscriptions, donations, and all local payment rows, filtering `Donation.PaymentStatus == WaitingPayment`. It must not be limited to the ten known examples.

For each candidate, use only GET provider lookups and the same shared verification/evidence rules as the callback flow. The command must classify and report every candidate, including provider-unavailable and ambiguous cases.

Dry-run must not call `SaveChanges`, transaction commit, or provider write methods. Apply mode must use a bounded transaction per record or safe batch, with re-checks immediately before update.

### Idempotency and concurrency

- Use provider payment ID plus local subscription/donation context as the stable identity for a repaired capture.
- Treat a donation with an already assigned successful confirmed payment as complete.
- Re-check state in the transaction to prevent a reconciliation run from overwriting a concurrent callback.
- Preserve unrelated payment rows and never replace an existing confirmed payment with a different ambiguous candidate.
- Add or use database constraints/indexes where compatible with the existing schema; do not silently alter historical data to create uniqueness.
- Record an audit/telemetry event for each applied or skipped candidate.

## Failure handling

Classify failures explicitly: missing local subscription, missing provider ID, provider lookup failure, provider payment not paid, key mismatch, amount mismatch, date mismatch, multiple candidates, malformed data, and database conflict. Only verified paid records are eligible for repair.

Provider credentials and payment secrets must never be included in reports. Correlation IDs, internal donation IDs, subscription IDs, provider payment IDs, and safe reason codes are acceptable.

## Testing strategy

- Unit-test evidence matching and classification with provider response fixtures.
- Integration-test generic subscription callbacks using mocked Easypay APIs.
- Assert donation status, confirmed payment, amounts, and completion timestamp after valid callbacks.
- Assert no mutation for invalid/ambiguous callbacks.
- Assert duplicate callbacks are idempotent.
- Test reconciliation against more than ten candidates, dry-run no-write behavior, apply behavior, and repeat-apply idempotency.
- Assert the mock Easypay client receives no POST/capture/create calls during reconciliation.
