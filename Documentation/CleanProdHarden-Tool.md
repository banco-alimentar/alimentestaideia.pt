# CleanProdHarden: Interactive Duplicate Payment Review

## Purpose

Migration `20261024174319_HardenPaymentConcurrency` adds the unique filtered index `IX_Payments_EasyPayPaymentId` and fails with SQL error `51000` while any non-null `EasyPayPaymentId` appears on more than one payment. [CleanProdHarden](../BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden) is a local Blazor Server app that lets an operator resolve those duplicate groups one at a time, instead of signing one JSON plan for every group.

For each group the app shows the raw payment, donation, donor, invoice, subscription, notification, and audit evidence. It also shows a plain-language narrative and a recommended action with its reasoning. The operator chooses the action, dry-runs it, and then applies it.

The app is a C# port of the rules, validations, and audit tables in [MergeDuplicateEasyPayPaymentIds.sql](../BancoAlimentar.AlimentaEstaIdeia.Web/Migrations/Scripts/MergeDuplicateEasyPayPaymentIds.sql). The [approval workflow](EasyPay-Payment-Reconciliation-Workflow.md) describes the same actions and evidence rules. The app reuses `ApplicationDbContext` and the entities from `BancoAlimentar.AlimentaEstaIdeia.Model`, so it reads the target database through Entity Framework Core like the website does. It does not call the EasyPay API and does not apply EF migrations.

## Safety Model

- **Local only.** The app listens on `http://127.0.0.1:5187`, answers only loopback requests (403 otherwise), and has no login. The reviewer types their name. It is recorded as `ApprovedBy`.
- **Read-only by default.** Committing requires `CleanProdHarden:AllowApply = true`. Dry runs are always allowed.
- **One group per transaction.** Each dry run or apply opens a serializable transaction. It locks `Payments` (`TABLOCKX, HOLDLOCK`) and `Donations` and `Invoices` (`TABLOCK, HOLDLOCK`), then re-reads the group. Dry runs always roll back. Pause writers before applying to production.
- **Evidence hash.** The group's evidence hash is computed the same way as the SQL planner's. It is recomputed inside the transaction. If the data changed since the operator reviewed it, the run stops with `51007`.
- **Dry run before apply.** An apply is accepted only when the latest dry run passed for exactly the same target server and database, group, evidence, action, owner, reason, and reviewer. Notes can change without a new dry run.
- **Typed confirmation.** Apply asks for the database name. A mismatch is rejected (`51100`) before anything changes.
- **Audited changes.** The app creates the script's audit tables on first use and writes to them inside the same transaction:
  - `dbo.EasyPayPaymentIdReconciliationPlans`
  - `dbo.EasyPayPaymentIdReconciliationAudit`
  - `dbo.EasyPayPaymentIdRetirementAudit`
  - `dbo.EasyPayPaymentDeletionAudit`

  The plan row records `Tool = CleanProdHarden`, the reviewer, the reason, and the evidence. The app verifies row counts and that the duplicates are gone before committing.
- **Separate decision store.** Decisions, notes, and every save, defer, dry run, and apply event are stored in a local SQLite file, never in the target database. The default location is `Environment.SpecialFolder.LocalApplicationData/CleanProdHarden/decisions.db`: `%LOCALAPPDATA%\CleanProdHarden` on Windows, `~/Library/Application Support/CleanProdHarden` on macOS, and `~/.local/share/CleanProdHarden` on Linux. Decisions are keyed by target server and database, so one store can hold a local copy and production side by side.

## Actions

| Action | Effect | Allowed when |
|---|---|---|
| `ReconcileIdentifiers` | The chosen owner keeps the provider ID; the other members' `EasyPayPaymentId` is set to `NULL`. Rows, amounts, and links are kept. | The owner is a group member (`51008`). |
| `RetireIdentifier` | Clears the provider ID on every member and keeps every row. | No member is confirmed by a donation (`51013`). |
| `DeleteGroup` | Archives the full rows, then deletes every member. | Every member is an unpaid, unconfirmed, uncompleted attempt that a validated replacement payment superseded, or an abandoned orphan. There must be no dependent rows, triggers, or missing `VIEW DEFINITION` permission (`51011`, `51012`). |

Every action needs a reason and a reviewer name. Every action is refused when a member already has reconciliation, retirement, or deletion audit history (`51003`).

## Patterns And Recommendations

The rule engine reproduces the SQL decision rules: `MultiConfirmed`, `ReviewUnconfirmedPaidDonation`, `ConfirmedPaid`, `ReviewConfirmedOwner`, `NoOwnerRetire`, and `ReviewUnconfirmedCapture`. A pattern classifier then explains how each group came about. The recommendations on the local production copy (65 groups) were:

| Pattern | Groups | Recommendation | Confidence |
|---|---:|---|---|
| Reconciliation tool copy | 40 | Reconcile to the completed row linked to the donation | High |
| Confirmed payment with zero-value retries | 14 | Reconcile to the confirmed payment | High |
| Simultaneous subscription charges | 4 | Reconcile; the suggested owner is the member whose donation has an invoice | Low |
| Superseded attempts | 3 | Retire; delete is an allowed alternative | High |
| Confirmed owner without paid evidence | 2 | Reconcile to the confirmed member | Medium |
| Abandoned orphan attempts | 1 | Retire; delete is an allowed alternative | High |
| Recurring charges sharing one ID | 1 | Reconcile to the suggested owner after a provider check | Low |

A Low or Medium recommendation is a starting point, not an approval. The page lists the follow-up checks, such as EasyPay backoffice captures or donations that need a separate correction.

### Differences From The Version-5 Plan

Compared with `CleaningScript/EasyPay-Reconciliation-20261103.deletions-v5.plan.json`:

- `45370`, `45391`, `45552`, `45576`: the plan chose `DeleteGroup`. The app recommends `RetireIdentifier`, following the workflow guidance to prefer non-destructive retirement. It also offers `DeleteGroup` because every member has a valid deletion basis.
- `4407`, `44950`, `45570`, `61665`, `62044`, `62104`, `62107`: the plan has `Action: null`. The app suggests an owner with Low or Medium confidence. The operator must still pick and justify the owner.

The plan is not imported. Decisions made in the app are recorded in its own store and in the audit tables.

## Setup And Run

Use a restored copy of the production database first. Store the connection string in user secrets; never in `appsettings.json`.

```bash
cd BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<SQL Server connection string>"
dotnet run
```

`dotnet run` uses the `http` launch profile, sets `ASPNETCORE_ENVIRONMENT=Development` so user secrets load, and opens `http://127.0.0.1:5187`. To allow commits for one session, start it with `CleanProdHarden__AllowApply=true dotnet run`. `CleanProdHarden__DecisionStorePath` moves the SQLite store. The SQL login needs `SELECT` on the reviewed tables. Applying also needs `UPDATE` (and `DELETE` for `DeleteGroup`) on `Payments`, `CREATE TABLE` and `INSERT` for the audit tables, and `VIEW DEFINITION` for deletion dependency checks.

## Workflow

1. **Dashboard (`/`).** Shows the target server and database and the review progress by pattern. Shows the audit tables and their row counts. Shows whether `HardenPaymentConcurrency` is applied, ready, or blocked, based on duplicate groups, provider IDs over 50 characters, and conflicting invoice groups. The dashboard never applies migrations.
2. **Groups (`/groups`).** Filter by pattern, rule, recommended action, decision status, or search text. Filters are kept in the URL.
3. **Group review (`/groups/{id}`).** Read the narrative, member comparison, raw cards, and recommendation. Pick an action, owner, and reason (the suggested reason follows the selected action). Save or defer, then dry-run and review the log. With apply enabled, type the database name and apply.
4. **History (`/history`).** Lists every recorded event for the current target.
5. After the last group, the dashboard should show zero duplicates. Back up, then apply the EF migrations in order, as described in the [approval workflow](EasyPay-Payment-Reconciliation-Workflow.md).

Repeat the workflow per tenant database. Decisions made on a local copy do not carry over: the server and database are part of every decision and dry-run fingerprint.

## Validation Codes

| Code | Meaning |
|---|---|
| `51002` | Reason missing or too long, or owner rules violated (an owner is required for `ReconcileIdentifiers` and not allowed otherwise) |
| `51003` | A member already has reconciliation, retirement, or deletion audit history |
| `51004` | Duplicates remain after the action |
| `51006` | Reviewer name (approval) missing or invalid |
| `51007` | Target or evidence changed since review |
| `51008` | Owner is not a member of the group |
| `51009` | Other migration requirements are still violated (provider IDs over 50 characters or conflicting invoice groups) |
| `51010` | No action selected or unsupported action |
| `51011` | `DeleteGroup` evidence is not safe |
| `51012` | `DeleteGroup` blocked by dependencies, triggers, or permissions |
| `51013` | `RetireIdentifier` refused because a member is confirmed |
| `51100` | Typed database name does not match |
| `51101` | Group no longer has duplicate provider IDs |
| `51102` | Changed or archived row count mismatch |
| `51103` | No matching passed dry run |
| `51104` | Apply disabled |
| `51199` | Unexpected error; the transaction was rolled back |

## Tests

```bash
dotnet test BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Tests/BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Tests.csproj --configuration Release
```

The unit tests cover:

- The rule port, including a check that the success-status list matches the SQL script.
- Pattern classification and recommendations.
- Every validation code.
- The decision store and the dry-run-before-apply gate.
- The loopback restriction.

Three SQL Server tests run only when `CLEANPRODHARDEN_TEST_SQL` contains a connection string. They dry-run the recommended action for one group of each pattern, check stale-evidence and unconfirmed-apply rejection, and assert that the evidence hashes are unchanged. They never commit.

## Build Note

The tool has a `wwwroot` folder. The Azure DevOps "Publish web" steps use `publishWebProjects: true`, which publishes every project with a `wwwroot`, so the build `drop` artifact may contain `BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.zip`. The release pipelines deploy only explicitly named packages, so the tool is never deployed. Its packaged settings contain no connection string and keep `AllowApply` off.
