# EasyPay subscription reconciliation report

`report-easypay-subscriptions` is a read-only audit tool for the two paired targets:

| Local target | Required EasyPay endpoint |
| --- | --- |
| Developer database | `https://api.test.easypay.pt` |
| Production database | `https://api.prod.easypay.pt` |

The command requires separate configuration for each target, validates the endpoint pairing, and does not fall back to the Tools project's default connection string or global EasyPay settings. It performs SQL reads and EasyPay subscription-detail GETs only; it does not update either system. Use database credentials restricted to `SELECT` and EasyPay credentials with the subscription read scope only.

## Configuration

Store these settings in the Tools project's user secrets or another approved secret store. Do not put database credentials or API keys in committed `appsettings.json` files.

```json
{
  "SubscriptionReconciliation": {
	"Developer": {
	  "ConnectionString": "<developer SQL connection string>",
	  "Tenant": {
		"PaymentStrategy": "SharedPaymentProcessor"
	  },
	  "EasyPay": {
		"BaseUrl": "https://api.test.easypay.pt",
		"AccountId": "<developer test account id>",
		"ApiKey": "<developer read-only API key>"
	  }
	},
	"Production": {
	  "ConnectionString": "<production SQL connection string>",
	  "Tenant": {
		"PaymentStrategy": "SharedPaymentProcessor"
	  },
	  "EasyPay": {
		"BaseUrl": "https://api.prod.easypay.pt",
		"AccountId": "<production account id>",
		"ApiKey": "<production read-only API key>"
	  }
	}
  }
}
```

For `IndividualPaymentProcessorPerFoodBank`, set `Tenant:PaymentStrategy` to that value and configure `EasyPay:AccountId-<foodBankId>` and `EasyPay:ApiKey-<foodBankId>` for each food bank represented in the corresponding database. The tool uses the initial donation's food bank to select credentials. Never reuse developer credentials for production or vice versa.

One way to set a secret without saving it in the repository is:

```powershell
dotnet user-secrets set "SubscriptionReconciliation:Developer:ConnectionString" "<developer SQL connection string>" --project BancoAlimentar.AlimentaEstaIdeia.Tools\BancoAlimentar.AlimentaEstaIdeia.Tools.csproj
```

Repeat for the other keys and production target. User Secrets are suitable for local development; use your approved secret store for operational production runs.

## Run

From the repository root:

```powershell
dotnet run --project BancoAlimentar.AlimentaEstaIdeia.Tools\BancoAlimentar.AlimentaEstaIdeia.Tools.csproj -- report-easypay-subscriptions --output reports\easypay-subscriptions.json
```

`--output` is optional; the default is a timestamped JSON file in the current directory. The command reports a nonzero exit code if either target fails, while preserving the other target's results in the report. Review the reported database server/catalog and endpoint before acting on any findings. Do not commit generated reports; they contain internal subscription identifiers and reconciliation data.

## Report contents and database impact

For each local subscription the report includes local status, deletion flag, payment type, frequency, amount, expiration, and related donation/payment/failed-email counts. It fetches provider ID, key-match result, amount, method status/type, retry count, failover, expiration and max captures. It flags cases where the scheduled retry reconciliation would set retries to three, without issuing a PATCH. It also aggregates local notification delivery states and provider lookup failures. Donor names, contact details, payment keys, and API credentials are not included.

The `databaseImpact` section summarizes the effect of prior payment changes:

- Raising the Easypay retry count is provider-side only; it requires no local write or schema migration. The existing scheduled function preserves each subscription's failover value.
- A verified failed capture can update or create a recurring donation, payment and subscription-donation link using the existing schema. Failure-email delivery tracking uses the existing `PaymentNotifications` table and delivery states. The report counts rows already present; it cannot predict future webhooks.
- The local subscription model stores a concrete expiration time. The current generated Easypay detail model does not expose `unlimited_payments`; the tool reports the local and provider expiration fields and `max_captures`, but does not infer a lifetime-policy change or modify either value. Confirm the intended lifetime and provider representation before any migration or bulk update.
- `databaseWrites`, `easypayWriteOperations`, and `schemaChangesRequired` are all zero for this command.

If a developer database was restored from production, some stored EasyPay IDs may not exist in the test account; those provider lookups will be reported as failures rather than repaired.
