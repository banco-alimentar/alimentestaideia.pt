# EasyPay subscription capture idempotency

## Goal

Ensure that one successful EasyPay subscription capture creates or completes exactly one local recurring donation and payment, even when EasyPay delivers the notification more than once or through overlapping notification contracts.

## Required behavior

- A repeated successful subscription capture must return success without creating another donation, payment, subscription link, or invoice email.
- The local payment must retain the EasyPay single-payment ID, not the inner transaction ID.
- Existing local capture records must be reused when they can be identified by the subscription transaction key and capture date.
- The generic and payment webhook routes must converge on the same idempotent persistence behavior.
- Existing initial-capture and non-subscription payment behavior must remain unchanged.

## Verification

- Add repository coverage for repeated subscription-capture persistence.
- Update webhook integration coverage to post the same notification twice and assert one recurring donation/payment.
- Verify the correct EasyPay payment ID is shown on the local payment record.
