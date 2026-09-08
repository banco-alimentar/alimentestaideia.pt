# Specification: Easypay Subscription Payment Webhooks and Reconciliation

## Overview

Fix Easypay subscription-capture processing so successful subscription payments are validated using the subscription response and its embedded transaction records.

Update the remediation tool to reconcile missing or incorrectly mapped subscription payments using read-only Easypay lookups. The process must never create, capture, charge, modify, or delete an Easypay payment or subscription.

## Goals

- Persist successful Easypay subscription payments correctly.
- Store the individual Easypay transaction ID in `CreditCardPayment.EasyPayPaymentId`.
- Never store the Easypay subscription ID as a payment ID.
- Support callbacks where the notification `id` contains the subscription ID instead of the individual payment ID.
- Reconcile historical subscription payments missing from the local database.
- Keep webhook processing and reconciliation idempotent.
- Provide clear diagnostics for unmatched, ambiguous, invalid, or unavailable records.

## Scope

### Included

- `subscription_capture` webhook verification.
- Easypay subscription lookup using `GET /2.0/subscription/{subscriptionId}`.
- Matching embedded Easypay transactions to local donations.
- Completing existing local donations.
- Creating missing local recurring donation/payment records when provider evidence is unambiguous.
- Updating incorrect local Easypay payment IDs.
- Reconciliation of all subscription-linked donations requiring review, especially `WaitingPayment` donations.
- Dry-run and explicit apply modes.
- Logging, metrics, and execution summaries.
- Regression and integration tests.

### Excluded

- Creating or modifying Easypay subscriptions.
- Creating, capturing, retrying, or charging Easypay payments.
- Refunds or chargebacks.
- Automatically repairing ambiguous records.
- Automatically marking a donation paid based only on an active subscription.
- A recurring background reconciliation job.

## Identifier Rules

| Local field | Meaning |
|---|---|
| `Subscription.EasyPaySubscriptionId` | Easypay subscription ID |
| `Subscription.TransactionKey` | Merchant transaction key associated with the subscription |
| `CreditCardPayment.EasyPayPaymentId` | Individual Easypay payment/transaction ID |
| `CreditCardPayment.TransactionKey` | Merchant transaction key |
| `Donation.Id` | Local donation ID |

A subscription ID must never be stored as `CreditCardPayment.EasyPayPaymentId`.

## Use Cases

### Valid future subscription capture

1. Resolve the local subscription from the notification transaction key.
2. Validate the stored Easypay subscription ID.
3. Fetch the subscription using the read-only subscription endpoint.
4. Validate the provider subscription ID, merchant transaction key, subscription value, and applicable subscription status.
5. Select the corresponding embedded transaction from the subscription response.
6. Validate transaction ID, transaction key, paid amount, requested amount, payment date, and paid state.
7. Persist the embedded transaction ID as the local Easypay payment ID.
8. Complete the corresponding local donation and payment atomically.
9. Return a successful webhook response.

The subscription-capture flow must not call `SingleIdGet` using the subscription ID.

### Callback contains the actual transaction ID

If the notification ID matches an embedded transaction ID, that transaction is the preferred match and must still be validated against the subscription response.

### Callback contains the subscription ID

If the notification ID equals the subscription ID, it must not be treated as a payment ID. The transaction must be identified using transaction key, capture date, requested/paid amounts, and transaction status. If exactly one transaction matches, it may be processed. If multiple transactions match, the callback must be rejected without changing local state and the ambiguity logged.

### Missing local recurring donation

If a provider transaction is verified and no corresponding local recurring donation exists, create the missing local recurring donation and payment record in the database only when the match is complete and unambiguous. Use the provider transaction ID as `EasyPayPaymentId`, link it to the existing local subscription, and mark it paid. Do not call an Easypay write endpoint.

### Existing local payment with an incorrect provider ID

If a local donation and payment match a provider transaction by reliable evidence, but the local payment contains the subscription ID, a wrong payment ID, or no payment ID, update only the local payment with the verified embedded transaction ID.

### Duplicate, unmatched, or ambiguous callbacks

Repeated delivery must be idempotent. Unmatched or ambiguous records, amount/date/key/status conflicts, missing provider subscriptions, and non-paid provider transactions must remain unchanged and be reported with stable reason codes.

## Remediation Tool Requirements

- Inspect all local subscriptions in the selected scope and their linked donations.
- Support `--subscription-id <local-id>` and `--easypay-subscription-id <provider-id>` filters; default to all local subscriptions.
- Use only read-only Easypay subscription operations and embedded transactions.
- Never call payment creation, capture, retry, subscription creation/update, or deletion endpoints; do not use `SingleIdGet` to enumerate subscription transactions.
- Match in this order: exact existing payment ID; unique transaction-key/date/amount match; unique local placeholder by subscription/date; database-only creation of a missing recurring donation when unambiguous.
- Default to dry-run. Dry-run performs zero database writes and reports every proposed repair.
- Apply mode requires an explicit flag, validates each record before update, and applies each repair atomically.
- Display database, Easypay environment, filters, mode, per-record details, and a final summary.
- Summarize subscriptions processed, donations inspected, provider subscriptions read, provider transactions found, matches, repairs, creations, corrected IDs, unmatched/ambiguous/mismatched records, lookup failures, deleted provider subscriptions, conflicts, proposed/applied changes, and provider write operations, which must always be zero.

## Data Integrity and Safety

For a reconciled payment, the local payment ID, transaction key, requested/paid amounts, completion date, paid status, confirmed payment, and subscription link must correspond to the provider transaction. Existing paid donations must not be downgraded or reassigned. The initial donation must not be duplicated or deleted during recurring-payment reconciliation.

No new Easypay payment or subscription may be created. Only independently verified paid transactions may mark donations paid. Ambiguous matches must never be resolved by arbitrary ordering. Provider credentials must not be logged.

## Observability and Testing

Log structured events for subscription lookup, transaction selection/rejection, completion, duplicate callbacks, ambiguity, provider failures, and database failures, using safe correlation fields only. The prior `easypay_subscription_lookup_failed` error must not be emitted merely because the callback ID is a subscription ID.

Regression and reconciliation tests must cover actual-payment and subscription IDs in notifications, multiple transactions, missing local donations, incorrect payment IDs, duplicates, unpaid/inconsistent transactions, dry-run immutability, apply-mode repairs, no provider writes, ambiguous matches, and idempotent reruns.

## Deployment

Deploy the webhook fix before apply-mode reconciliation. Run dry-run first, review unresolved and ambiguous records, run apply only for the approved environment and scope, then rerun dry-run to confirm no valid repairs remain.
