# Implementation Plan: Easypay Subscription Payment Webhooks and Reconciliation

## Objective

Fix subscription-capture webhooks so they use embedded Easypay transactions, then extend the existing reconciliation tool to repair missing or incorrect local payment mappings using read-only provider data. No Easypay write operation is allowed.

## Baseline and constraints

- Preserve all pre-existing worktree changes.
- Keep normal, non-subscription payment notifications unchanged.
- Keep the existing command and filters for `reconcile-waiting-subscription-donations`.
- Dry-run remains the default; `--apply` is explicit.
- Provider reads happen before local writes; local changes are atomic and idempotent.

## Tasks

### Corrective review pass (2026-09-07)

- [x] Reject unknown or malformed callback IDs and exact-transaction validation failures without fallback.
- [x] Require a non-empty, exact provider transaction key.
- [x] Require embedded subscription payment evidence for successful subscription captures.
- [x] Reject cross-subscription local payment ownership conflicts.
- [x] Protect already-paid donations from fallback remapping in the remediation tool.
- [x] Keep ambiguous date/amount matches unchanged regardless of transaction order.
- [x] Re-read local donations before apply-mode repairs and re-check duplicate provider IDs before creation.
- [x] Add focused selector and repository safety tests.
- [ ] Unify the tool matcher and webhook selector behind one shared reconciliation service.
- [ ] Add end-to-end tool apply/dry-run tests with a database fixture.

### A. Shared provider evidence and matching

- Add a canonical subscription-payment evidence model.
- Add a pure embedded-transaction selector/normalizer.
- Distinguish subscription IDs from individual payment IDs.
- Support exact notification payment-ID matching and fallback matching by key/date/amount/status.
- Reject ambiguity, zero-value, unpaid, mismatched, and missing transactions without mutation.

### B. Production webhook

- Change subscription-capture verification to use the subscription response and embedded transactions.
- Remove `SingleIdGet` from the subscription-capture path.
- Pass canonical payment evidence to the generic notification handler and repository.
- Preserve standalone payment behavior.
- Add structured success/failure diagnostics with stable reason codes.

### C. Local persistence

- Refactor subscription capture completion to accept canonical evidence.
- Store the individual Easypay transaction ID in `CreditCardPayment.EasyPayPaymentId`.
- Complete existing placeholders, update incorrect IDs, or create missing recurring donations only for unambiguous verified paid transactions.
- Keep updates idempotent and reject conflicts without reassigning confirmed payments.

### D. Remediation tool

- Use embedded subscription transactions only; remove any subscription-payment `SingleIdGet` use.
- Reuse the same selection and validation rules as the webhook.
- Process subscriptions and provider transactions newest first.
- Support existing local/provider subscription filters.
- In dry-run, perform zero writes and report every proposed repair.
- In apply mode, perform only local atomic repairs and report provider writes as zero.
- Extend per-record and summary output for corrected IDs, completed/created donations, unmatched/ambiguous records, mismatches, conflicts, and provider lookup failures.

### E. Tests

- Add selector tests for both callback-ID formats, exact matches, fallback matches, ambiguity, mismatches, and zero-value/unpaid transactions.
- Extend repository tests for evidence persistence, wrong-ID correction, missing-donation creation, conflicts, zero-value protection, and idempotency.
- Extend webhook integration tests to assert no `SingleIdGet` call for subscription captures and preserve normal payment behavior.
- Extend reconciliation tests for dry-run immutability, apply repairs, no provider writes, ambiguity, and idempotent reruns.

### F. Validation

- Run focused builds/tests, then the full solution build/test suite.
- Run the tool in scoped dry-run mode and verify database writes and Easypay writes are both zero.
- Review the final diff so unrelated pre-existing changes are not included.

## Dependency graph

```text
A -> B -> C
A -> D
B + C -> E
C + D + E -> F
```

## Completion criteria

- Successful subscription callbacks no longer call `SingleIdGet` with a subscription ID.
- Local payment records contain individual Easypay transaction IDs.
- Missing historical subscription payments can be reconciled locally from embedded provider data.
- Dry-run is mutation-free; apply mode performs no provider writes.
- Ambiguous or inconsistent data remains unchanged and is clearly reported.
- Focused and full validation passes, or remaining failures are documented with their cause.
