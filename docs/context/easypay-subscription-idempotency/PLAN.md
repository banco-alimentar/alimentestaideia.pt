# Implementation plan

1. [x] Inspect the existing subscription repository and webhook tests, including the current duplicate expectation.
2. [x] Change the subscription branch of `CompleteEasyPayPaymentAsync` to use one idempotent capture routine and pass the EasyPay single-payment ID into `EasyPayPaymentId`.
3. [x] Make repeated legacy subscription-capture calls reuse the existing provider/payment/date record and avoid creating a second donation or payment.
4. [x] Update repository and webhook integration tests for repeated delivery and identifier persistence.
5. [x] Run the repository and web integration test projects, then review the diff and working tree.

## Acceptance criteria

- Two identical successful webhook deliveries produce one initial plus one recurring subscription donation.
- The recurring payment has the EasyPay single-payment ID.
- The second delivery returns HTTP 200 and does not send a second invoice.
- Existing tests continue to pass.
