# EasyPay Reconciliation Approval Workflow

## Purpose

Generate a reviewable plan, approve explicit payment actions, dry-run them, and only then opt into applying them. `ReconcileIdentifiers` and `RetireIdentifier` preserve every payment/notification row and donation link; they only clear audited provider IDs. An explicitly selected `DeleteGroup` can delete only verified superseded failed attempts, after archiving their complete rows; it never cascades or rewrites dependent records. The workflow does not apply Entity Framework migrations automatically.

Files:
- [PowerShell command](../scripts/Invoke-EasyPayPaymentReconciliation.ps1)
- [SQL planner and reconciler](../BancoAlimentar.AlimentaEstaIdeia.Web/Migrations/Scripts/MergeDuplicateEasyPayPaymentIds.sql)
- [Repeatable regression tests](../scripts/Test-EasyPayPaymentReconciliation.ps1)
- [Local-copy decision report](EasyPay-Payment-Reconciliation-Report-2026-11-03.md)
- [CleanProdHarden interactive review app](CleanProdHarden-Tool.md): reviews, dry-runs, and applies the same actions one group at a time, with the same rules and audit tables, instead of approving one JSON plan.

Prerequisites: PowerShell 7+, `sqlcmd`, Windows authentication, and a connection with the required read permissions. Reconciliation also needs payment-update and audit-table creation/insert permissions. Deletion requires `DELETE` on payments, database `VIEW DEFINITION`, and read access to every referencing table; metadata visibility must be complete. Run commands from the repository root. Start with a backed-up, restorable local copy, not live production.

## 1. Generate A Plan

```powershell
./scripts/Invoke-EasyPayPaymentReconciliation.ps1 -Mode Plan -ServerInstance . -Database prod -PlanPath ./TestResults/EasyPay-Reconciliation-20261103.confirmed-paid-v5.plan.json
```

This command has already generated that local review file. Choose a new filename to generate another snapshot: the command refuses to overwrite an existing plan or its approvals. The output directory must already exist. Generated plans contain production-derived identifiers and amounts; keep them private and do not commit them.

Plan mode reads application tables and creates temporary working tables only. It never creates durable audit tables or modifies application data, even if the SQL `@DryRun` flag is zero. It takes shared table locks on payments, donations, and invoices to obtain consistent evidence; writers can be briefly blocked.

The generated JSON binds the plan to the SQL server, database, and a SHA-256 evidence hash. Per-group hashes cover every column of duplicate payment rows, every related donation row including forward/inverse confirmation references, and the complete external payment rows referenced by those donations' confirmations. Changing an external paid replacement therefore invalidates approval too. Raw transaction keys are compared and hashed within SQL but are not exported. The overall hash binds all current duplicate groups. Declared foreign-key dependencies are rechecked and locked during execution rather than trusted from exported JSON.

`Members.Requested` and `Members.Paid` are exported as JSON numbers using `decimal(19,4)`, for example `0.7000`, rather than the scientific notation produced by the underlying SQL `real` columns. Four fractional digits retain sub-cent review detail and match the existing amount-to-confirmation comparison scale. This is presentation rounding only: stored payment values, raw equality checks, and evidence fingerprints are unchanged. Two different stored values can display the same rounded number while `SameAmounts` still correctly reports a mismatch. Null remains null, and zero remains numeric zero. JSON uses a decimal point regardless of local currency formatting.

This does not recover precision already lost in floating-point storage. Changing monetary columns to exact decimal storage would require a separately reviewed model/schema migration, not a reconciliation-display fix.

### Owner Decision Rules

The owner follows the application's own payment contract. `Payments.DonationId` records every attempt for a donation and is NULL on many legacy rows. `Donations.ConfirmedPaymentId` is set only by `TryCompleteDonationPayment`, together with `PaymentStatus = Payed (1)`, after the amount is validated. The confirmation is therefore the strongest ownership evidence.

Plans use `PlanVersion: 5`; versions 1-4 are rejected. Rules are evaluated in this order:

| Decision rule | Condition | Generated `Action` |
| --- | --- | --- |
| `MultiConfirmed` | Two or more members are each confirmed by a different donation. | `null`. Resolve duplicate donations first (see below), or choose an owner from provider evidence. |
| `ReviewUnconfirmedPaidDonation` | A member links to a `Payed` donation whose `ConfirmedPaymentId` is NULL; that member may be its payer. | `null` |
| `ConfirmedPaid` | Exactly one member is confirmed by a `Payed` donation, its forward link is NULL or the same donation, its status is successful, and its positive paid amount matches the donation to four decimals. Sibling keys, types, amounts, and statuses do not matter. | `ReconcileIdentifiers` with that owner |
| `ReviewConfirmedOwner` | One confirmed member fails the `ConfirmedPaid` checks (for example, an unpaid donation or failed attempt). | `null` |
| `NoOwnerRetire` | No member is confirmed, completed, paid, or successful by its own evidence. Success inherited through status fallback from a donation confirmed by a payment outside the group is ignored. | `RetireIdentifier` |
| `ReviewUnconfirmedCapture` | Anything else, typically an unconfirmed successful or paid row. | `null` |

`SuggestedAction` (`ReconcileIdentifiers`, `RetireIdentifier`, `ResolveDuplicateDonations`, or `Review`) and `SuggestedCanonicalPaymentId` help the reviewer. Groups with `Action: null` block execution with error `51010` until a reviewer fills them.

For `MultiConfirmed` recurring pairs, verify the EasyPay capture and run `reconcile-waiting-subscription-donations --subscription-id <id> --cleanup-duplicates` from the Tools project, first as a dry run, then with `--apply`. When EasyPay holds one capture, it keeps the lower donation ID, moves any invoice to it, and removes the duplicate donation with its payments and notifications. That removal is a hard delete without an archive: back up first, then regenerate the plan. Groups with more than two confirmations, such as `4407`, need an explicit owner chosen from provider evidence.

### Status Evidence And Deletion Review

Version 4 introduced the per-payment relationship markers described below, version 3 explicit actions, and version 2 status evaluation. Generate a fresh plan and review its evidence; do not just change an old plan's version or carry approvals forward.

When `Payments.Status` is non-null, it takes precedence. Values `paid`, `Success`, `ok`, `approved`, and `COMPLETED` are successful, ignoring case and surrounding spaces, matching `DonationPaymentCompletion.IsSuccessfulPaymentStatus`. Any other non-null value is classified as non-successful for planning, including blank text and the literal text `NULL`; those are not SQL nulls and do not trigger donation fallback.

Only a SQL null payment status triggers fallback. Use the donation linked by `Payments.DonationId`; if that link is missing, a uniquely confirming donation linked through `Donations.ConfirmedPaymentId` may supply the fallback. No unique applicable donation, a null donation status, or an unrecognized enum value leaves the status unknown. The [PaymentStatus enum](../BancoAlimentar.AlimentaEstaIdeia.Model/PaymentStatus.cs) maps `Payed = 1` to successful and `NotPayed = 2`, `WaitingPayment = 3`, and `ErrorPayment = 4` to non-successful.

Each member exports raw `Status`, `StatusSource`, `StatusDonationId`, numeric `DonationPaymentStatus`, `DonationPaymentStatusName`, and nullable `IsSuccessfulStatus`. Group success/non-success/unknown counts explain the classification. A unique successful member is preferred as a review suggestion over date ranking, but conflicts or unknown evidence cannot approve themselves. Successful status, rather than a required completion timestamp, supports the confirmation-backed rules; amounts, keys, confirmation ownership, and other existing checks still apply.

Each payment is independently marked `DeletionCandidate: true` for any of the following review rules, including in mixed groups:

| Member condition | `DeletionCandidateReason` | Interpretation |
| --- | --- | --- |
| `Payments.DonationId IS NULL` | `MissingDonationId` | Mark the missing forward link even if status is successful or another donation confirms the payment. Do not infer that the payment is unused. |
| Following `DonationId`, the donation has `PaymentStatus = 1`, a non-null `ConfirmedPaymentId`, and that ID is absent from **all** members of the group | `PaidDonationConfirmedOutsideGroup` | Mark the payment as outside the donation's selected group of captures. Confirmation pointing to a sibling does not match this rule. |
| Resolved status is non-successful and neither relationship rule takes priority | `UnsuccessfulStatus` | Status-based review retained from earlier versions. Waiting or unpaid attempts can still be active. |

Members expose `MissingDonationLink` and `HasExternalDonationConfirmation` as the underlying relationship evidence. A candidate with unresolvable status also has `DeletionBlockedByUnknownStatus: true`; missing status does not become an inferred failure. `DeletionBlockedByFinancialEvidence` protects candidate rows with successful status, completed/paid amounts, inverse confirmations, or paid-donation evidence not explained by a validated replacement. A null forward link never erases `Donations.ConfirmedPaymentId` references or other dependencies.

At group level, `DeletionCandidateCount` and `HasDeletionCandidates` describe any marked members; `MissingDonationCandidateCount`, `ExternalConfirmationCandidateCount`, and `DeletionUnknownStatusCount` explain the counts. Group `DeletionCandidate` is true only when every member is marked. The flags do not select an action, authorize partial deletion, or make a group executable. Dependency inspection includes individual candidates even when the group itself is not wholly marked.

Group `DeletionBlockedByFinancialEvidence: true` means at least one marked member has the financial warning above. Non-successful statuses with contradictory evidence also contribute to `StatusConflictCount`. A failed attempt linked to a paid donation is not a conflict when the donation is demonstrably confirmed by a different successful paid payment. A false warning flag is not by itself permission to delete: all evidence, dependency, action, and approval checks below must pass.

### Identifier Retirement

`RetireIdentifier` requires `CanonicalPaymentId: null` and a reason. It clears the provider ID on every member, keeps every row and relationship, and records each original ID in `dbo.EasyPayPaymentIdRetirementAudit`. Execution refuses it with error `51013` when any member is confirmed by a donation; choose an owner instead. Webhooks look payments up by transaction key and write the provider ID back on completion, so a late notification for a retired attempt can still be recorded.

### Explicit Group Deletion

`DeleteGroup` is never generated automatically; prefer `RetireIdentifier` unless deletion is specifically required. Selecting `Action: "DeleteGroup"` requires `CanonicalPaymentId: null` and an explicit group reason, in addition to root approval and normal freshness checks. It selects every current member of that duplicate group; do not remove members or groups from the plan.

Every member must be uncompleted, have exactly zero paid, be unconfirmed by any donation, and have one `DeletionBasis`:

- `SupersededByReplacement`: its forward-linked donation is `Payed` and confirmed by a payment outside the group. The replacement must have a successful status with a positive paid amount matching the donation, or be a `COMPLETED` PayPal payment with PayPal payment and payer IDs. The member's own status must be non-successful; a NULL status that only inherits `Payed` from that externally paid donation is accepted.
- `AbandonedOrphan`: no donation link and no successful status (NULL status allowed).

On the local copy, the deletions plan `TestResults/EasyPay-Reconciliation-20261103.deletions-v5.plan.json` marks `45370`, `45391`, `45552`, and `45576` as `DeleteGroup`.

`HasSafeDeletionEvidence` requires every target to resolve non-successful, have no completion timestamp, have exactly zero paid amount, and be unconfirmed by any donation. Every target must have a forward-linked donation that is `Payed`, whose `ConfirmedPaymentId` identifies a different payment outside the entire duplicate group. That replacement must have its own non-null successful provider status, a positive paid amount matching the donation at four decimal places, and no contradictory non-null forward donation link. A null replacement status cannot inherit success circularly from the same donation.

The broader version-4 markers do not relax these executable deletion rules. In particular, `MissingDonationId` alone cannot justify `DeleteGroup`, and the executor does not delete arbitrary marked subsets from a mixed group. Confirmed, successful, completed, paid, unknown, and dependent rows remain protected; any broader cleanup requires a separately reviewed action rather than changing evidence flags.

Members export `DonationConfirmedPaymentId`, `ReplacementPaymentStatus`, decimal `ReplacementPaid`, `HasSuccessfulReplacement`, and a `Dependencies` array. The planner reports `DependencyCheckSupported` and `CanDeleteGroup` as current diagnostics. Execution recalculates those checks and refuses any dependent row through declared foreign keys, including `CASCADE` and `SET NULL`; notifications, payment items, confirmation links, and other history are not deleted or detached. Unsupported composite/non-ID references, incomplete metadata visibility, and any enabled payment trigger block deletion. Application-only references without database foreign keys still require operator review.

For group `45370`, the local copy confirms donation `44002` points to payment `45369` with status `Success` and paid amount `25.1000`, rather than the failed attempts `45370/45371`. Version 5 classifies it as `NoOwnerRetire` and prefills `RetireIdentifier`; `CanDeleteGroup` remains true if a reviewer explicitly prefers deletion. No root approval or database apply was performed.

If deletion is chosen instead, the relevant approval fields for this group are:

```json
{
  "GroupId": 45370,
  "Action": "DeleteGroup",
  "CanonicalPaymentId": null,
  "Reason": "Failed attempts 45370 and 45371 are superseded by successful confirmed payment 45369; preserve donation 44002 and archive the failed rows."
}
```

This is a fragment of the existing group, not a replacement for the complete plan. `SuggestedAction`, `DeletionCandidate`, and `CanDeleteGroup` are evidence only; `Action` is the explicit instruction. Use the exact property name `Action`, not lowercase `action`.

On the inspected local copy, version 5 classifies 54 `ConfirmedPaid`, 4 `NoOwnerRetire` (`45370`, `45391`, `45552`, `45576`), 2 `ReviewConfirmedOwner` (`44950`, `45570`: confirmed failed attempts on unpaid donations, suggested owners `44955` and `45573`), and 5 `MultiConfirmed` (`4407`, `61665`, `62044`, `62104`, `62107`) groups. Root approval remains blank.

## 2. Review And Approve The JSON

Edit only these approval fields in the exported JSON:

| Location | Field | Requirement |
| --- | --- | --- |
| Root | `ApprovedBy` | Non-empty reviewer identity/name. |
| Root | `ApprovalReason` | Non-empty explanation approving this complete plan and its selected ownership rules. |
| Each group | `Action` | Explicitly `ReconcileIdentifiers`, `RetireIdentifier`, or `DeleteGroup`; null/unknown actions are rejected. |
| Each group | `CanonicalPaymentId` | One member payment for `ReconcileIdentifiers`; must be null for `RetireIdentifier` and `DeleteGroup`. |
| Each group | `Reason` | Non-empty justification for that ownership/deletion decision. Generated reasons can be retained if the reviewer accepts them. |

Preserve `PlanVersion`, `ServerName`, `DatabaseName`, `PlanEvidenceHash`, `GroupId`, `ExpectedEvidenceHash`, and all evidence/membership fields. Do not remove groups. If the underlying data changes, generate and review a new plan rather than copying hashes into an old one.

Example approval fields, not approvals for any actual production group:

```json
{
  "ApprovedBy": "Reviewer name",
  "ApprovalReason": "Reviewed the complete plan against the decision report and provider evidence."
}
```

The generated plan is the full document: do not replace it with this small example. For a flagged group, fill in its existing `CanonicalPaymentId` and `Reason` fields only after resolving it.

These are trusted-operator approvals, not cryptographic signatures or an authorization system. SQL permissions remain the security boundary. The executed plan records both the declared reviewer and the actual Windows-authenticated SQL login.

If evidence proves that separate rows represent separate genuine captures, restore their verified distinct provider IDs using a separately reviewed, audited correction. This null-clearing workflow does not invent replacement IDs, retire every member of a group, or repair accounting/confirmation records.

## 3. Dry-Run The Complete Approved Plan

Pause application/background writers before reconciling; the script holds an exclusive payment-table lock and shared donation/invoice locks until the transaction ends. The SQL lock timeout is 15 seconds; the command query timeout defaults to 120 seconds and can be set with `-QueryTimeoutSeconds`.

```powershell
./scripts/Invoke-EasyPayPaymentReconciliation.ps1 -Mode DryRun -ServerInstance . -Database prod -PlanPath ./TestResults/EasyPay-Reconciliation-20261103.confirmed-paid-v5.plan.json
```

Execution refuses missing, duplicated, extra, or stale groups, a different server/database, incomplete approvals, owners outside their group, and existing per-payment audit conflicts. It also checks all non-null provider-ID lengths and invoice sequence uniqueness before writes. An unreviewed group blocks the whole transaction; there is no partial-apply option.

Dry-run executes approved identifier updates, archives, and explicit group deletions, reports affected IDs, then rolls everything back, including newly created audit tables. It is not read-only: locks and transaction logging occur. Identifier-only updates can fire payment triggers; `DeleteGroup` refuses enabled payment triggers. External trigger effects are not guaranteed reversible. Catchable errors roll back and rethrow; cancellation can bypass `CATCH`, so check transaction state before reusing a cancelled SQL session.

`-WhatIf` is also available on the command. It does not execute SQL and is not a substitute for `-Mode DryRun`, which validates the plan against live data.

## 4. Explicitly Apply After Verification

```powershell
./scripts/Invoke-EasyPayPaymentReconciliation.ps1 -Mode Apply -ServerInstance . -Database prod -PlanPath ./TestResults/EasyPay-Reconciliation-20261103.confirmed-paid-v5.plan.json
```

Apply prompts for confirmation. Use `-Confirm:$false` only inside a separately authorized automation step; it suppresses the confirmation prompt but does not bypass SQL approval or staleness checks. Neither the command nor the plan approves or applies itself.

Each `ReconcileIdentifiers` group retains its original provider ID on the selected owner and clears only other occurrences to `NULL`. Each explicit `DeleteGroup` archives complete source rows before deleting the checked failed attempts, preserving the external paid payment and every donation/dependent record. Durable records are created atomically in:

- `dbo.EasyPayPaymentIdReconciliationAudit`: changed payment ID, canonical payment ID, original provider ID, and timestamp. The original four-column audit schema remains supported; existing records are not overwritten.
- `dbo.EasyPayPaymentIdReconciliationPlans`: plan evidence hash, reviewer, approval reason, complete approved JSON including group reasons/members, timestamp, and actual SQL login. It retains the context of every ownership decision without duplicating the full plan on each changed row.
- `dbo.EasyPayPaymentIdRetirementAudit`: retired payment ID, group ID, original provider ID, plan evidence hash, and timestamp. It is created only for an explicit retirement action.
- `dbo.EasyPayPaymentDeletionAudit`: deleted payment ID, group ID, replacement payment ID, original provider ID, full unrounded source-row JSON, plan evidence hash, and deletion timestamp. It is created only for an explicit deletion action. The plan hash links each archived row to reviewer/reason/actual-login evidence in the plans table. Existing archive records cannot be overwritten.

Audit tables are created transactionally only when needed and persist only on explicit commit. A failure anywhere in a mixed-action plan rolls back every update, archive, and deletion. A SQL failure must not be interpreted as success even if earlier output rows were displayed. Keep a verified database backup; the row archive supports review and a separately designed restore, not an automatic undo command after commit.

A successfully applied plan is consumed: replaying it fails as stale because its duplicate groups no longer exist. Generate a fresh empty plan, approve its root fields, and apply it for a verified no-op. Empty plans do not add audit records. Any future schema/model change must preserve these operational audit tables as history.

After reconciliation, verify zero duplicate non-null provider IDs, expected retained IDs and archives, exactly the approved deletion count, unchanged notification/donation counts and relationships, intact paid replacements, and no unintended field changes. Then rehearse the actual pending migration and application behavior locally. Roll out per tenant with backups and paused writers, refreshing the plan against each target database; a local-copy plan deliberately cannot be applied to a different production server/database.

## Regression Tests

```powershell
./scripts/Test-EasyPayPaymentReconciliation.ps1
```

The tests create a unique disposable LocalDB database and temporary JSON plans, exercise real SQL and the PowerShell command, and remove both afterward. The 107 checks cover the version-5 decision rules (confirmed paid owners despite sibling differences, unconfirmed Payed donations, multi-confirmed groups, retirement guards), `RetireIdentifier` dry-run/apply/audit/replay, per-payment missing-link/outside-group markers, inverse-confirmation protection, mixed-group dependency visibility, unknown blockers, statuses/precision/approvals/drift/preflights, explicit deletion prerequisites, changed external replacements, missing actions, full-row archive equality, archive-error mixed-plan rollback, dependencies including `CASCADE`/`SET NULL`, composite keys, incomplete metadata permissions, enabled triggers, dry-run/WhatIf, explicit mixed apply, consumed-plan rejection, and index compatibility. The test command never targets a pre-existing database. `-ServerInstance` can select another SQL Server, but only a new disposable database is used.

## Direct SQL Use

The SQL file defaults to `@Mode = N'Plan'`, `@DryRun = 1`, and `@ApprovedPlan = NULL`. Direct users can execute Plan mode in VS Code and review the returned JSON. Reconciliation requires setting `@Mode = N'Reconcile'` and supplying the full reviewed JSON as an escaped Unicode SQL string in `@ApprovedPlan`; the PowerShell command handles that escaping and session settings automatically. Only an explicit `@DryRun = 0` in Reconcile mode commits.