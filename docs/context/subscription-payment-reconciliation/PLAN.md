# Implementation plan: Subscription Payment Callback and Reconciliation

## Phase 1 — Baseline and evidence inventory

- [ ] Confirm the callback payload contract and generated Easypay models for `subscription_capture`.
- [ ] Map every local identifier: subscription ID, transaction key, donation ID, payment ID, and provider capture/payment ID.
- [ ] Identify how historical callback/payment IDs are stored and define the evidence sources available to reconciliation.
- [ ] Produce a read-only inventory query for all subscription-linked donations in `WaitingPayment`, including payment rows and candidate provider IDs.
- [ ] Document records that cannot be verified because Easypay does not expose capture history through the subscription endpoint.

## Phase 2 — Specification and verification contract

- [ ] Define typed verification outcomes and stable reason codes.
- [ ] Implement subscription identity verification with `GET /2.0/subscription/{subscriptionId}`.
- [ ] Implement actual payment verification with `GET /2.0/single/{paymentId}`.
- [ ] Validate provider status, merchant key, requested/paid amounts, local amount, subscription relationship, and capture date.
- [ ] Add tests for every rejection reason and provider lookup failure.

## Phase 3 — Permanent callback correction

- [ ] Refactor `subscription_capture` handling to use the shared verification contract.
- [ ] Replace date-only/local-status matching with exact verified payment identity where available.
- [ ] Add an atomic, charge-free completion method that sets payment success, donation `Payed`, and `ConfirmedPayment` together.
- [ ] Handle the case where a verified recurring capture has no local recurring donation/payment row through a dedicated database-only persistence path.
- [ ] Preserve existing paid state on duplicate or late callbacks.
- [ ] Add integration tests for valid, invalid, duplicate, concurrent, and missing-local-row callbacks.

## Phase 4 — One-off reconciliation command

- [ ] Add a dedicated Tools command/service that scans all subscription-linked `WaitingPayment` donations.
- [ ] Make dry-run the default and require explicit `--apply` for writes.
- [ ] Read the Easypay subscription's embedded transaction collection and use each transaction's
  identifier as the authoritative local `EasyPayPaymentId`; do not use `GET /single/{transaction-id}`
  to enumerate or enrich those transactions.
- [ ] Match transactions to local donations by existing provider ID first, then by a unique date and
  requested/paid amount match; for exactly two ambiguous recurring candidates, resolve only the case
  where at least one candidate is `WaitingPayment` with a zero-value confirmed EasyPay payment by
  removing the invalid duplicate and its local payment record, preserving the lowest local donation
  ID, and reconciling that donation; leave all other ambiguous matches unchanged.
- [x] Before provider matching, identify recurring donations older than seven days whose local
  `PaymentStatus` is `WaitingPayment` and whose EasyPay payment row has `Requested=0` and `Paid=0`;
  propose or apply deletion of the local donation and payment rows only, never the initial donation
  or any record protected by an invoice/shared-payment relationship.
- [x] Add a read-only subscription-state audit that reports local status/deletion state, Easypay
  method status, definite mismatches, and affected donation IDs; classify provider deleted/not-found
  results without changing local state.
- [ ] Reuse the same read-only provider verification and atomic completion logic as callbacks.
- [ ] Ensure the command cannot access provider write methods; test this with mocked API clients.
- [ ] Report totals and per-record outcomes: repaired, already complete, missing evidence, mismatch, ambiguous, malformed, provider unavailable, and database conflict.
- [ ] Add tenant scoping and safe batch/transaction boundaries.

## Phase 5 — Operational validation

- [ ] Run the inventory in the target environment and retain its output.
- [ ] Review all candidates classified as repairable before apply mode.
- [ ] Run apply mode once with the explicit scope and monitor telemetry/database changes.
- [ ] Re-run in dry-run mode and confirm no eligible repaired donation remains incorrectly `WaitingPayment`.
- [ ] Confirm no Easypay charge, capture, creation, or checkout operation was invoked.
- [ ] Record unresolved/ambiguous records for manual follow-up.

## Phase 6 — Final verification and delivery

- [ ] Run focused callback and reconciliation tests.
- [ ] Run the relevant full test projects and build with zero errors.
- [ ] Review the diff for unrelated changes and secret leakage.
- [ ] Obtain code-review and operational sign-off before running apply mode in production.

## Dependency graph

```text
Evidence inventory
        |
        v
Verification contract -----> Callback correction
        |                             |
        +----------------------------v
                         Reconciliation command
                                  |
                                  v
                         Dry-run and review
                                  |
                                  v
                         Explicit apply mode
```

## Review decisions required before implementation

1. Confirm the available historical provider-payment evidence source for records whose local callback payload does not retain the payment ID.
2. Confirm the tenant/environment scope for the one-off apply run.
3. Confirm that records without independently verifiable paid-payment evidence must remain unchanged, even if the Easypay subscription is active.
