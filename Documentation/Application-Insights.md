# Application Insights telemetry



This project sends telemetry to **Azure Application Insights** from the web application (`BancoAlimentar.AlimentaEstaIdeia.Web`) and from scheduled Azure Functions (`BancoAlimentar.AlimentaEstaIdeia.Function`).



In addition to the automatic ASP.NET Core / Azure Functions telemetry (HTTP requests, dependencies, exceptions from unhandled errors, `ILogger` output), the codebase emits **custom events**, **traces**, and **exceptions** through the Application Insights SDK.



## Telemetry severity



Use this column when reading the catalog below or building alerts.



| Severity | Meaning | Typical tables | Action |
|----------|---------|----------------|--------|
| **Normal** | Expected business flow, audit, or success signal | `customEvents`, `traces`, `requests` (success) | No action unless volume is unexpected |
| **Warning** | Handled anomaly: missing data, rejected webhook, auth denied, validation blocked | `customEvents`, `traces` | Investigate if frequent or clustered |
| **Error** | Explicit failure recorded as an event or failed request (no stack trace) | `customEvents`, `requests` (failed), some `traces` | Investigate cause and impact |
| **Exception** | Thrown error with stack trace | `exceptions`, `TrackException` calls | Investigate and fix |



Notes:



- **`TrackMetric` is not used** in the current codebase.

- **`CreateSinglePayment`** is **Normal** when payment is created; check custom property `Exception` — if present, treat as **Warning** (API call failed but was caught).

- **Dynamic event names** (e.g. `CreditCardPayment-NotFound`) follow the same severity as the static `*-NotFound` events (**Warning**).

- **404 HTTP requests** are marked successful by `Ignore404ErrorsTelemetryInitializer` and usually appear as **Normal** in failure blades.



## Configuration



| Component | Setup |
|-----------|--------|
| **Web** | `Startup.cs` — `services.AddApplicationInsightsTelemetry()` with custom initializers and processors |
| **Functions** | `Program.cs` — `AddApplicationInsightsTelemetryWorkerService()` + `ConfigureFunctionsApplicationInsights()` |



Both use a shared `TelemetryClient` (injected in the web app; created from `TelemetryConfiguration` in functions).



## SDK methods used



| Method | Purpose | Default severity |
|--------|---------|------------------|
| `TrackEvent(name, properties)` | Business milestones, validation failures, audit-style signals | Depends on event (see catalog) |
| `TrackEvent(EventTelemetry)` | Same as above, when properties are built incrementally | Depends on event |
| `TrackException(ex)` / `TrackException(ex, properties)` | Unexpected errors, often with contextual properties | **Exception** |
| `TrackTrace(message)` / `TrackTrace(message, properties)` | Informational or failure messages without stack trace | **Normal** or **Error** (see catalog) |
| `StartOperation<RequestTelemetry>(name)` + `StopOperation()` | Outbound HTTP calls from `MultiBancoPaymentNotificationFunction` | **Normal** unless dependency fails |



## Automatic property enrichment



These run on **every** telemetry item (or on HTTP requests) and add context without an explicit `TrackEvent` call.



### `DonationFlowTelemetryInitializer` (Web)



Adds `DonationSessionId` when the donation session key is present in `HttpContext.Items`.



**Source:** `BancoAlimentar.AlimentaEstaIdeia.Web/Telemetry/DonationFlowTelemetryInitializer.cs`



### `HttpTelemetryInitializer` (Web)



On HTTP request telemetry, adds when available:



| Property | Source |
|----------|--------|
| `DonationSessionId` | Donation flow session |
| `TenantId`, `TenantName` | Current tenant |
| `DonationId` | `HttpContext.Items` |
| `GenericNotification-*` | EasyPay generic webhook body (`/easypay/generic`) |
| `PaymentNotification-*` | EasyPay payment webhook body (`/easypay/payment`) |



Also sets `telemetry.Context.User` from the authenticated user.



**Source:** `BancoAlimentar.AlimentaEstaIdeia.Web/Telemetry/HttpTelemetryInitializer.cs`



### `Ignore404ErrorsTelemetryInitializer` (Web)



Marks HTTP 404 responses as successful so they do not appear as failed requests in failure blades.



**Source:** `BancoAlimentar.AlimentaEstaIdeia.Web/Telemetry/Ignore404ErrorsTelemetryInitializer.cs`



### Telemetry processors (Web)



| Processor | Effect |
|-----------|--------|
| `RemoveAzureStorageTelemetryFilter` | Drops noisy Azure Storage dependency telemetry |
| `FileNotFoundAzureStroageBlobFilter` | Drops 404 blob-not-found dependency noise |
| `WebApplicationStatusFilter` | Filters low-value application status probes |



**Source:** `BancoAlimentar.AlimentaEstaIdeia.Web/Telemetry/Filtering/`



### Browser SDK (Web layouts)



Tenant layouts include the Application Insights JavaScript snippet with a client-side initializer (donation session correlation). This sends page views and client telemetry to the same Application Insights resource.



## Custom events catalog



All custom events appear in Application Insights under **Logs → `customEvents`**. The `name` column matches the event names below.



### Azure Functions



| Severity | Event | When | Properties | Source |
|----------|-------|------|------------|--------|
| Normal | `FunctionCoreExecuted` | Tenant processing finished without throwing | `TenantId`, `Name` | `MultiTenantFunction.RunFunctionCore` |
| Normal | `DonationReportPublished` | Donation report uploaded to blob storage | `PagesUploaded`, `PaidAmount`, `PaidCount` | `GenerateDonationReportFunction.GenerateReportAsync` |



**Related non-event telemetry (Functions):**



| Severity | Signal | When | Source |
|----------|--------|------|--------|
| Normal | Trace | Report generation skipped (disabled, no data, etc.) | `GenerateDonationReportFunction` |
| Error | Trace | Report generation failed (message in trace body) | `GenerateDonationReportFunction` |
| Normal | Trace | `"Subscription {id} has been deleted."` | `DeleteOldSubscriptionFunction` |
| Normal | Trace | `"There was {n} elements to be proccesed."` | `MultiBancoPaymentNotificationFunction` |
| Exception | Exception | Outer function failure (`FunctionName` property on report function) or per-tenant failure | All timer functions via `MultiTenantFunction` / function `Run` handlers |
| Normal | Request | Outbound GET to Multibanco reminder endpoint | `MultiBancoPaymentNotificationFunction` (`StartOperation`) |



See also [Azure Functions](Azure-Functions.md).



### Platform / configuration



| Severity | Event | When | Properties | Source |
|----------|-------|------|------------|--------|
| Normal | `DatabaseMigration` | EF Core pending migrations applied for a tenant | `PendingMigrations`, `Tenant` | `TentantConfigurationInitializer.MigrateDatabaseAsync` |
| Warning | `ServicePrincipal-Secret-NotFound` | Tenant service principal secret missing in SAS Key Vault | `EnvironmentName`, `TenantId`, `SasSPKeyVaultKeyName` | `KeyVaultConfigurationManager` |
| Warning | `SecretNotFound` | Required Key Vault secret not found while loading tenant config | — | `KeyVaultConfigurationManager` |



**Related exceptions:** Key Vault initialization and secret-load failures (**Exception**) include `EnvironmentName`, `Stage`, and related context in `KeyVaultConfigurationManager`.



### Payments — EasyPay single donations



| Severity | Event | When | Properties (typical) | Source |
|----------|-------|------|----------------------|--------|
| Normal / Warning | `CreateSinglePayment` | EasyPay single payment API call attempted | `TransactionKey`, `PaymentMethod`, `DonationId`, `UserId`, `Amount`, `TenantName`, `TenantId`, `EasyPayId` or `Exception` | `Payment.cshtml.cs` → `CreateEasyPayPaymentAsync` |
| Normal | `ExistingSinglePayment` | Existing EasyPay payment found for donation | `PublicId`, `PaymentId`, `PaymentStatus` | `Payment.cshtml.cs` |
| Warning | `ExistingSinglePayment-NotFound` | EasyPay returned payments but none match stored id | `PublicId`, `PaymentId`, `PaymentStatus`, `Count-Index` | `Payment.cshtml.cs` |
| Warning | `DonationIsNull` | Payment page loaded without a donation | `OriginalDonationId`, `PublicDonationId` | `Payment.cshtml.cs` |
| Warning | `Donation-Multibanco-NotFound` | Multibanco payment created but donation missing | `TransactionKey`, `targetPayment.id` | `Payment.cshtml.cs` |
| Warning | `Payment-Multibanco-NotFound` | Multibanco EasyPay response missing | `TransactionKey`, `Donation.Id` | `Payment.cshtml.cs` |
| Warning | `{PaymentType}-NotFound` | Payment webhook/update could not find payment row (dynamic name, e.g. `CreditCardPayment-NotFound`) | `{Type}TransactionKey`, `EasyPayId` | `DonationRepository.UpdatePaymentAsync` |
| Warning | `Donation-{PaymentType}-NotFound` | Payment exists but linked donation missing (dynamic) | `{Type}TransactionKey`, `PaymentId` | `DonationRepository` |
| Error | `WebhookDonationAmountMismatch` | Paid amount does not match donation | `DonationId`, `TransactionKey`, `ExpectedAmount`, `Requested`, `Paid` | `DonationRepository.TryCompleteDonationPayment` |
| Normal | `UpdatePaymentTransaction-Donation-Payed` | Generic EasyPay notification marked donation paid | `EasyPayId`, `TransactionKey`, `BasePaymentId`, `DonationId`, `PaymentStatus`, `Message` | `DonationRepository.UpdatePaymentTransaction` |
| Warning | `UpdatePaymentTransaction-Donation-Is-Null` | Generic notification could not resolve donation | `EasyPayId`, `TransactionKey`, … | `DonationRepository` |
| Warning | `PayedDonation-To-Failed-Payment-Try` | EasyPay tried to fail an already paid donation | `DonationId`, `EasyPayId`, `TransactionKey`, `BasePaymentId`, `Message` | `DonationRepository` |



### Payments — EasyPay webhooks & verification



| Severity | Event | When | Properties | Source |
|----------|-------|------|------------|--------|
| Warning | `EasypayWebhookRejected` | Webhook rejected (verification failed) | `Reason` | `EasyPayControllerBase.WebhookVerificationFailed` |
| Warning | `EasypayApiLookupFailed` | Easypay API lookup failed during verification | `TransactionKey`, `Detail` | `EasyPayApiWebhookVerifier` |
| Warning | `EasypaySubscriptionLookupFailed` | Subscription lookup failed during verification | `TransactionKey`, `Detail` | `EasyPayApiWebhookVerifier` |
| Error | `WebhookAmountMismatch` | Webhook amounts do not match donation | `TransactionKey`, `Detail` | `EasyPayApiWebhookVerifier` |
| Normal | `SendInvoiceEmail` | Invoice email sent after payment | `DonationId`, `PublicId`, `ConfirmedPayment.Status` | `EasyPayControllerBase.SendInvoiceEmail` |
| Normal | `EmailAlreadySent` | Duplicate invoice email prevented | `DonationId`, `PaymentId` | `EasyPayControllerBase` |
| Warning | `DonationWrongStatus` | Invoice email skipped — wrong payment status | `DonationId`, `DonationPaymentStatus`, `ConfirmedPayment.Status` | `EasyPayControllerBase` |
| Warning | `DonationNotFound` | Donation missing during invoice email / thanks flow | `DonationId`, `Method` or `Page`, `UserId` | `EasyPayControllerBase`, `Thanks.cshtml.cs`, `SubscriptionThanks.cshtml.cs` |
| Warning | `EmailIsNotEanbled` | Email sending disabled in configuration | — | `EasyPayControllerBase`, `Mail.cs` |



### Payments — subscriptions



| Severity | Event | When | Properties | Source |
|----------|-------|------|------------|--------|
| Normal | `CreateEasyPaySubscriptionPaymentAsync` | Subscription checkout payment created | Subscription/donation context properties | `SubscriptionPayment.cshtml.cs` |
| Warning | `SubscriptionNotFound` | Subscription missing in thanks or webhook handling | `Page`, `SubscriptionId` / webhook fields, `UserId` | `SubscriptionThanks.cshtml.cs`, `SubscriptionRepository` |
| Normal | `PaymentDateIsEqual` | Subscription capture skipped (same-day guard) | `Operation`, `easyPayId`, `transactionKey`, `status`, `Date` | `SubscriptionRepository` |
| Warning | `TransactionKeyIsNull` | Subscription webhook missing transaction key | `Operation`, `easyPayId`, `status`, … | `SubscriptionRepository` |
| Error | `SubscriptionNotDeleted` | EasyPay delete returned non-success | — (raw response in preceding trace) | `Subscriptions/Delete.cshtml.cs` |
| Warning | `WhenDeletingSubscripionUserIsNotValidGet` | User not allowed to view delete page | `CurrentLoggedUser`, `SubcriptionId`, `SubscriptionUser` | `Subscriptions/Delete.cshtml.cs` |
| Warning | `WhenDeletingSubscripionUserIsNotValid` | User not allowed to delete subscription | Same as above | `Subscriptions/Delete.cshtml.cs` |
| Warning | `WhenDeletingSubscripionUserIsNotValidGetDetails` | User not allowed to view subscription details | Same as above | `Subscriptions/Details.cshtml.cs` |



### Invoices



| Severity | Event | When | Properties | Source |
|----------|-------|------|------------|--------|
| Normal | `FindInvoiceByPublicId` | Invoice lookup by public donation id | `publicId`, `donation.Id`, `InvoiceId`, `InvoiceStatusResult` | `InvoiceRepository` |
| Warning | `PublicDonationIdNotFound` | Public id not found for invoice lookup | `PublicId` | `InvoiceRepository` |
| Warning | `FindInvoiceByDonation-DonationNotFound` | Donation missing when creating invoice | `DonationId`, `UserId`, `Function` | `InvoiceRepository` |
| Warning | `CreateInvoice-DonationUserIdNotFound` | Donation belongs to another user | `DonationId`, `UserId`, `Function` | `InvoiceRepository` |
| Warning | `CreateInvoice-InvoiceWithPaymentStatusNotPayed` | Invoice requested before payment complete | `DonationId`, `UserId`, `Function` | `InvoiceRepository` |
| Warning | `CreateInvoice-ConfirmedPaymentNull` | No confirmed payment on donation | `DonationId`, `UserId`, `Function` | `InvoiceRepository` |
| Warning | `CreateInvoice-InvoiceRequestedTooLate` | Invoice outside allowed year window | `DonationId`, `UserId`, `Function` | `InvoiceRepository` |
| Warning | `CreateInvoice-ConfirmedFailedPaymentStatus` | Confirmed payment in failed state | `DonationId`, `UserId`, `ConfirmedPaymentStatusId`, `Function` | `InvoiceRepository` |
| Warning | `GenerateInvoiceUnauthorized` | Invoice download denied | `PublicDonationIdHash` (redacted) | `GenerateInvoice.cshtml.cs` |
| Warning | `GenerateInvoiceErrorMessage` | Invoice too old to generate | `PublicDonationIdHash` | `GenerateInvoice.cshtml.cs` |
| Warning | `CheckStatus-DonationNotFound` | Payment-status polling page, donation missing | `PublicDonationIdHash` | `CheckPaymentStatusInvoice.cshtml.cs` |
| Normal | `ClaimInvoiceComplete` | User completed claim-invoice flow | `PublicId` | `ClaimInvoice.cshtml.cs` |



**Related exceptions:** Invalid GUID passed to `FindInvoiceByPublicId` (**Exception**); invoice sequence anomalies via `TrackExceptionTelemetry` in `InvoiceRepository` (**Exception**).



### Email (`Mail.cs`)



| Severity | Event | When | Properties |
|----------|-------|------|------------|
| Normal | `SendInvoiceEmailWantsReceipt` | Invoice/receipt email path for donation | `DonationId`, … |
| Normal | `SendInvoiceEmailNoReceipt` | Thanks email without receipt | `DonationId` |
| Normal | `SendSubscriptionEmailWantsReceipt` | Subscription receipt email | `DonationId` |
| Error | `Error.SendSubscriptionEmailNoReceipt` | Subscription thanks email failed | `DonationId` |
| Normal | `EmailSent` | Email dispatched successfully | — |



**Related exceptions:** Email send failures and missing template files (**Exception**) in `Mail.cs`.



### Donation flow / thanks pages



| Severity | Event | When | Properties | Source |
|----------|-------|------|------------|--------|
| Normal | `ThanksOnGetSuccess` | Thanks page loaded for completed donation | `DonationId`, `UserId`, `PublicId` | `Thanks.cshtml.cs`, tenant `Thanks.cshtml.cs`, `SubscriptionThanks.cshtml.cs` |
| Warning | `DonorEmailNotFound` | Multibanco reference email could not be sent | `DonationId`, `UserId` | `Multibanco.cshtml.cs` |
| Warning | `DonationIdNotValid` | Invalid donation id on Multibanco page | `DonationId` | `Multibanco.cshtml.cs` |



### Account / GDPR



| Severity | Event | When | Properties | Source |
|----------|-------|------|------------|--------|
| Normal | `DownloadPersonalData` | User exported personal data (GDPR) | `UserId` | `DownloadPersonalData.cshtml.cs` |



## KQL queries — finding errors



Run these in **Application Insights → Logs** (or **Monitoring → Logs**). Adjust `ago(24h)` as needed.



### All exceptions (last 24 hours)



```kusto

exceptions

| where timestamp > ago(24h)
| project timestamp, cloud_RoleName, type, outerMessage, problemId, customDimensions
| order by timestamp desc

```



### Exception count by type



```kusto

exceptions

| where timestamp > ago(7d)
| summarize count() by type, outerMessage
| order by count_ desc

```



### Function exceptions



```kusto

exceptions

| where timestamp > ago(7d)
| where cloud_RoleName contains "Function"

    or tostring(customDimensions.FunctionName) != ""

| project timestamp, type, outerMessage, customDimensions
| order by timestamp desc

```



### Failed HTTP requests (Web)



```kusto

requests

| where timestamp > ago(24h)
| where success == false
| where resultCode !in ("404")  // 404s are often marked successful by our initializer
| project timestamp, name, url, resultCode, duration, cloud_RoleName, customDimensions
| order by timestamp desc

```



### Failed dependencies



```kusto

dependencies

| where timestamp > ago(24h)
| where success == false
| project timestamp, name, type, target, resultCode, duration, cloud_RoleName
| order by timestamp desc

```



### Custom events — errors and warnings only



```kusto

let errorEvents = dynamic([

    "WebhookDonationAmountMismatch", "WebhookAmountMismatch",

    "Error.SendSubscriptionEmailNoReceipt", "SubscriptionNotDeleted"

]);

let warningEvents = dynamic([

    "EasypayWebhookRejected", "EasypayApiLookupFailed", "EasypaySubscriptionLookupFailed",

    "DonationIsNull", "DonationNotFound", "SubscriptionNotFound",

    "SecretNotFound", "ServicePrincipal-Secret-NotFound",

    "PayedDonation-To-Failed-Payment-Try", "EmailIsNotEanbled"

]);

customEvents

| where timestamp > ago(24h)
| where name in (errorEvents) or name in (warningEvents)

    or name endswith "-NotFound"

    or name contains "NotFound"

    or name startswith "CreateInvoice-"

    or name startswith "WhenDeletingSubscripion"

| extend severity = case(

    name in (errorEvents), "Error",

    name in (warningEvents) or name endswith "-NotFound", "Warning",

    "Warning")

| project timestamp, name, severity, customDimensions
| order by timestamp desc

```



### Payment amount mismatches



```kusto

customEvents

| where name in ("WebhookDonationAmountMismatch", "WebhookAmountMismatch")
| where timestamp > ago(7d)
| project timestamp, name, customDimensions
| order by timestamp desc

```



### EasyPay webhook rejections and lookup failures



```kusto

customEvents

| where timestamp > ago(7d)
| where name in (

    "EasypayWebhookRejected",

    "EasypayApiLookupFailed",

    "EasypaySubscriptionLookupFailed")

| project timestamp, name, customDimensions
| order by timestamp desc

```



### Key Vault / configuration problems



```kusto

union customEvents, exceptions

| where timestamp > ago(7d)
| where (itemType == "customEvent" and name in ("SecretNotFound", "ServicePrincipal-Secret-NotFound"))

    or (itemType == "exception" and cloud_RoleName contains "Web")

| where outerMessage contains "KeyVault" or outerMessage contains "Secret"

    or name in ("SecretNotFound", "ServicePrincipal-Secret-NotFound")

| project timestamp, itemType, name, type, outerMessage, customDimensions
| order by timestamp desc

```



### Donation report — success vs skip vs failure



```kusto

union customEvents, traces, exceptions

| where timestamp > ago(7d)
| where (itemType == "customEvent" and name == "DonationReportPublished")

    or (itemType == "trace" and message contains "report")

    or (itemType == "exception" and tostring(customDimensions.FunctionName) == "GenerateDonationReportFunction")

| project timestamp, itemType, name, message, type, outerMessage, customDimensions
| order by timestamp desc

```



### Error and warning traces (Functions and report failures)



```kusto

traces

| where timestamp > ago(7d)
| where message contains "failed"

    or message contains "error"

    or message contains "Error"

    or message contains "exception"

| project timestamp, message, cloud_RoleName, customDimensions
| order by timestamp desc

```



### Combined error dashboard (single view)



```kusto

union

    (exceptions

    | where timestamp > ago(24h)
    | extend category = "Exception", detail = outerMessage, signal = type),

    (requests

    | where timestamp > ago(24h) and success == false and resultCode !in ("404")
    | extend category = "FailedRequest", detail = name, signal = resultCode),

    (customEvents

    | where timestamp > ago(24h)
    | where name in ("WebhookDonationAmountMismatch", "WebhookAmountMismatch",

        "Error.SendSubscriptionEmailNoReceipt", "SubscriptionNotDeleted")

        or name endswith "-NotFound"

        or name contains "Mismatch"

        or name in ("EasypayWebhookRejected", "SecretNotFound", "ServicePrincipal-Secret-NotFound")

    | extend category = "CustomEvent", detail = name, signal = name)
| project timestamp, category, signal, detail, cloud_RoleName, customDimensions
| order by timestamp desc
| take 100

```



## Example KQL queries — normal operations



**Last donation report publications:**



```kusto

customEvents

| where name == "DonationReportPublished"
| order by timestamp desc
| take 20

```



**All custom events in the last 24 hours:**



```kusto

customEvents

| where timestamp > ago(24h)
| summarize count() by name
| order by count_ desc

```



**Successful thanks-page loads:**



```kusto

customEvents

| where name == "ThanksOnGetSuccess"
| where timestamp > ago(24h)
| summarize count() by bin(timestamp, 1h)
| order by timestamp desc

```



## Maintaining this document



When adding or renaming `TrackEvent`, `TrackTrace`, or `TrackException` calls:



1. Assign a **severity** (Normal / Warning / Error / Exception).

2. Update this file and keep event names stable where possible so dashboards and alerts continue to work.

3. Add the event to the error KQL `dynamic([...])` lists if it represents a warning or error.



Primary search command:



```powershell

rg "Track(Event|Trace|Exception)\(" --glob "*.cs"

```


