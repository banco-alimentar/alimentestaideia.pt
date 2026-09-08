# Architecture: Easypay Subscription Payment Webhooks and Reconciliation

## Design

Subscription captures use one read-only Easypay subscription lookup and the embedded transaction collection returned by that response. They must not call `SingleIdGet` with `notification.Id`, because that ID can be the subscription ID rather than an individual payment ID.

The flow is split into three concerns:

1. Provider access: fetch and normalize a subscription and its embedded transactions.
2. Pure selection/matching: validate subscription identity, amounts, dates, keys, paid state, and ambiguity without database or network side effects.
3. Local persistence: atomically update or create the local payment/donation and enforce idempotency.

The remediation tool reuses the same provider normalization and matching rules. It is an orchestration layer and must not contain a second, divergent matching algorithm.

## Identifier boundaries

- `Subscription.EasyPaySubscriptionId`: provider subscription ID.
- `Subscription.TransactionKey`: merchant key for the subscription.
- `CreditCardPayment.EasyPayPaymentId`: individual provider transaction/payment ID.
- `CreditCardPayment.TransactionKey`: merchant transaction key.

The subscription ID must never be stored in `CreditCardPayment.EasyPayPaymentId`.

## Provider access

Add or reuse a read-only subscription reader abstraction exposing only `GET /2.0/subscription/{id}`. The web adapter uses the tenant/food-bank credential factory; the tools adapter uses its configured environment. Neither adapter may call provider write operations, and subscription reconciliation must not use `SinglePaymentApi` to enumerate or enrich embedded transactions.

The generated Easypay SDK models remain unchanged. Map them to application-level evidence containing provider subscription ID, provider transaction ID, transaction key, payment date, requested amount, paid amount, and payment status.

## Transaction selection

The pure selector must:

1. Validate provider subscription ID, transaction key, and subscription value.
2. Prefer an exact embedded transaction ID match when `notification.Id` is an individual payment ID.
3. Detect when `notification.Id` equals the subscription ID and never treat it as a payment ID.
4. In that case, match embedded transactions by transaction key, callback date, amounts, and paid status.
5. Accept exactly one match; reject zero or multiple matches with stable reason codes.
6. Reject missing IDs, subscription IDs used as payment IDs, key/amount/date mismatches, and non-paid transactions.

No arbitrary ordering may resolve ambiguity.

## Webhook flow

For `SubscriptionCreate`, keep the existing subscription lookup and activation behavior without payment lookup.

For successful `SubscriptionCapture`:

1. Resolve the local subscription by notification transaction key.
2. Fetch the provider subscription.
3. Select and validate an embedded transaction.
4. Pass canonical payment evidence to `SubscriptionRepository`.
5. Persist the individual provider transaction ID and complete/create the local donation.
6. Send invoice/email only after local payment completion succeeds.

Non-subscription payment notifications retain their existing `SinglePaymentApi` behavior.

## Local persistence

The repository owns local mutation and transaction boundaries. Provider reads happen before a local database transaction. Each repair or completion is atomic and idempotent:

- Exact payment ID already mapped correctly: no-op.
- Existing placeholder or wrong ID with a unique provider match: update it.
- Missing recurring donation with complete, unique provider evidence: create it locally and mark it paid.
- Existing confirmed payment belonging to another donation: report a conflict and do not reassign.
- Repeated callback or tool execution: no duplicate donation/payment.

The initial donation is not cloned when it can safely be completed, and it is never duplicated merely because a provider callback was retried.

## Remediation tool

The tool scans all local subscriptions by default and supports existing local/provider subscription filters. It loads each provider subscription once, plans actions in memory, prints each action with local/provider identifiers and values, and then:

- dry-run: performs no `SaveChanges`, transaction commit, or provider write;
- apply: requires an explicit flag, rereads local records before each repair, and writes one local repair atomically at a time.

Permitted actions are local payment-ID correction, local completion of a verified payment, and local creation of a missing recurring donation when the match is unambiguous. Provider write operations are always zero. Ambiguous, unmatched, mismatched, deleted-provider, and lookup-failure cases remain unchanged and are reported.

## Observability and tests

Use structured reason codes and safe identifiers for provider lookup, transaction selection, rejection, completion, duplicate, ambiguity, and database failure events. Never log credentials.

Tests must cover both notification-ID formats, multiple embedded transactions, exact and date/amount matches, ambiguity, mismatches, missing local donations, wrong local IDs, duplicate callbacks, dry-run immutability, apply-mode idempotency, and zero provider writes. Existing non-subscription webhook tests must remain passing.
