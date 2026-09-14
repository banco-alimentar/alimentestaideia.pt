# Architecture notes

- EasyPay webhook controllers are entry points only; payment persistence belongs in the repository layer.
- `SubscriptionRepository.CompleteSubscriptionCapture` is the canonical subscription-capture completion path because it already validates provider identity, ownership, amount, and existing records.
- `DonationRepository.CompleteEasyPayPaymentAsync` currently handles `/easypay/payment` and must delegate subscription captures without duplicating creation logic.
- `EasyPayPaymentId` represents EasyPay's single-payment ID (`notification.id`). The nested transaction ID must not overwrite it.
- Preserve existing EF Core model and localization conventions. Add no secrets or deployment configuration.
- Repeated webhook requests must be safe when handled sequentially. Database-level uniqueness should be considered only if it can be added without breaking existing historical data; application-level lookup and reuse are required for this change.
