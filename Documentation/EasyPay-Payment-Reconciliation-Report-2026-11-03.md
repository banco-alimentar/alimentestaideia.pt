# EasyPay Payment Reconciliation Decision Report

## Scope And Status

- Snapshot: 2026-11-03 18:56:50 UTC, local SQL Server `amdtook2` (`.`), database `prod`.
- Source: the user's local production-data copy, queried with Windows authentication. This is not a fresh inspection of the live production database.
- Purpose: identify decisions needed before applying `20261024174319_HardenPaymentConcurrency` from commit `053040ca8dc495d1e94c3afc57d4d74ce571e3fd`.
- Status: **proposals only; no mapping is approved by this report**. No database rows, schema, or reconciliation rules were changed while producing it.
- Handling: internal operational report containing production-derived record identifiers and amounts. Donor names, contact details, credentials, and raw transaction keys are excluded.

Related files: [reconciliation script](../BancoAlimentar.AlimentaEstaIdeia.Web/Migrations/Scripts/MergeDuplicateEasyPayPaymentIds.sql), [pending migration](../BancoAlimentar.AlimentaEstaIdeia.Web/Migrations/20261024174319_HardenPaymentConcurrency.cs).

Implementation follow-up: the [plan-and-approval workflow](EasyPay-Payment-Reconciliation-Workflow.md) now implements automated suggestions and explicit exception review. This report remains the original local-copy evidence snapshot, not an approved execution plan; descriptions of the former ambiguity guard are historical. The 53 suggestions and 12 review groups described here apply to the former version-1 rules. Status-aware version-2 planning additionally evaluates provider/donation status, preserves unknown/conflicting evidence, and marks wholly non-successful groups for deletion review without deleting records. Consult the workflow for the latest classifications; old plans and this report do not approve any status-aware action.

Version-3 follow-up: the workflow now supports explicit audited `DeleteGroup` actions for unsuccessful attempts proven to be superseded by a separate confirmed successful paid payment, with strict dependency checks and full-row archives. This supersedes this report's earlier no-deletion executor description, not its historical evidence. Group `45370` has such an external replacement (`45369`); its requested deletion intent is recorded in a fresh local plan but has not been applied. All old plans require regeneration and fresh review.

Version-4 follow-up: per-payment review now also marks missing forward donation links and forward-linked paid donations confirmed outside the entire group, including mixed groups. Missing links do not erase inverse confirmations or financial evidence. The latest workflow documents 89 marked members and their blockers; these broader markers do not weaken actual deletion safeguards or approve old plans.

## Executive Summary

The migration requires unique non-null `EasyPayPaymentId` values. Keeping all records is compatible with that requirement: preserve the identifier on an approved owner, retain each original identifier in the audit table, and clear the other occurrences to `NULL`. For genuinely different charges, restoring their real distinct provider payment IDs is preferable when evidence is available.

There are 65 duplicate-ID groups containing 141 payments and 76 extra identifier occurrences. The current reconciliation script blocks the whole transaction because 59 groups trigger its manual-review checks. Setting `@DryRun = 0` does not bypass those checks.

| Review tier | Groups | Payment rows | Extra ID occurrences | Interpretation |
| --- | ---: | ---: | ---: | --- |
| A: permitted by current policy | 6 | 13 | 7 | One forward-linked donation per group, no completed payments; transaction keys differ. Policy eligibility is not proof of the correct owner. |
| B1: matching evidence, one confirmed owner | 40 | 80 | 40 | Same type, populated transaction key, and requested/paid amounts; one payment is confirmed by a donation. All groups contain two completed payments. |
| B2: confirmed owner plus unfinished zero-value rows | 13 | 34 | 21 | One confirmed, completed payment has a positive amount; other rows are unfinished with requested/paid amounts of zero. Same type and populated transaction key within each group. |
| C: unresolved ownership | 6 | 14 | 8 | Five groups have multiple confirmed owners; one has none. Do not select an owner automatically. |
| Total | 65 | 141 | 76 | No records need to be deleted to resolve identifier uniqueness. |

Of the 59 blocked groups, 53 have exactly one payment referenced by `Donations.ConfirmedPaymentId`: the 40 B1 groups and 13 B2 groups. This is evidence for a candidate, not independent proof of provider-ID ownership.

## Baseline And Migration Checks

| Check | Result |
| --- | ---: |
| All payment rows | 61,032 |
| Non-null provider IDs | 55,940 |
| Payment notification rows | 36,977 |
| Donation rows | 59,106 |
| Provider IDs longer than 128 characters | 0 |
| Duplicate invoice groups for `(Year, COALESCE(FoodBankId, 0), Sequence)` | 0 |

The prior dry run stopped with SQL error `51002`, before payment updates or audit-table creation. These baseline counts were unchanged afterward and no transaction remained open. The invoice and length checks cover known data constraints in the migration, not the entire application workflow or every later migration.

## How To Read The Evidence

- `Payments.DonationId` is the forward donation link. Its absence does **not** mean that no donation references the payment: `Donations.ConfirmedPaymentId` can still point to it. Do not repair or remove either relationship as part of identifier reconciliation.
- A confirmed owner is a payment referenced by at least one donation's `ConfirmedPaymentId`. All references within each group were checked; multiple such payment rows make ownership unresolved.
- Current rank means the existing script's completed-first, newest-`Created`, highest-`Id` ordering. It is not approval and does not prove which provider event a row represents.
- Type/key/amount comparisons were evaluated in SQL. A matching key requires the same populated transaction key on every row. Amount equality compares the stored values; the exception details below use rounded currency displays for readability.
- Matching values can reflect copied data or duplicate processing, but can also conceal separate real captures. This report does not claim that matching rows represent the same financial transaction.
- Candidate owner means the payment proposed to retain the existing provider ID after review. If authoritative evidence identifies distinct genuine payment IDs instead, use those IDs rather than clearing them indiscriminately.

## A: Permitted By Current Policy

All six groups have no completed payments and different transaction keys within the group. The latest row is what the current script would select, but any provider evidence should take precedence. Three groups already have a donation confirmation despite having no `Completed` timestamp. Do not infer completion state from confirmation alone.

| Provider ID | Payment IDs | Forward donation | Current-policy owner | Additional evidence or caveat |
| --- | --- | ---: | ---: | --- |
| 9371c5eb-841b-41ad-a60a-94ece9bf11be | 44950, 44954, 44955 | 43587 | 44955 | Donation 43587 confirms 44955; amounts agree. |
| 02199856-86a2-417b-91b8-3ea5d8439d92 | 45370, 45371 | 44002 | 45371 | No confirmed owner; amounts agree. |
| 78520732-8793-4adf-a9bf-839b157c9c07 | 45391, 45392 | 44020 | 45392 | No confirmed owner; amounts agree. |
| 807aeaea-115b-4132-bbfb-a217c5d9c96d | 45552, 45553 | 44176 | 45553 | No confirmed owner; amounts agree. |
| 5a95aa01-2cc6-42c5-bcdc-70b02b23e0ee | 45570, 45573 | 44191 | 45573 | Donation 44191 confirms 45573; amounts agree. |
| c17cd5fc-8dbe-48b3-8bd6-72b8af6f5c02 | 60989, 60990 | 58707 | 60990 | Donation 58707 confirms 60990; requested/paid amounts differ between rows. Investigate before approval. |

These seven extra occurrences are not processed independently by the current script: any blocked group aborts the entire transaction. Processing only A would still leave the migration blocked by the remaining groups.

## B1: Matching Evidence And One Confirmed Owner

Each group has two completed rows with the same type, populated transaction key, and requested/paid amounts. One row has a forward donation link and is the sole confirmed owner. The other row lacks the forward link. No conflicting non-null forward/confirmed donation links were found in these groups.

Recommend reviewing the confirmation-backed owner below rather than approving the entire group merely because its values match. The paired-row pattern suggests historical copying or reconciliation, but its origin has not been verified from operational records.

| Provider ID | Payment IDs | Confirmed donation | Candidate owner | Current rank owner |
| --- | --- | ---: | ---: | ---: |
| d887881c-45f6-4bf1-a9ec-6af6e8b58e84 | 22847, 61415 | 21866 | 61415 | 22847 |
| c41f7b26-6dc1-4252-92f8-d1fa1a7eb10d | 31993, 61390 | 30724 | 61390 | 61390 |
| 4c63fff7-1afd-4704-ba10-c66240bc0329 | 34257, 61391 | 32941 | 61391 | 61391 |
| ca314100-98dd-430c-b4cb-b570ae253ee4 | 34346, 61392 | 33031 | 61392 | 61392 |
| e2172432-5cfc-488e-a6eb-78941d2bfe7f | 34431, 61393 | 33135 | 61393 | 61393 |
| c2da7276-30ac-412a-ba67-1ce3cc5f24fd | 34501, 61394 | 33209 | 61394 | 61394 |
| ccecd3b4-3ada-4f59-9a45-5c7127119be7 | 34575, 61395 | 33277 | 61395 | 61395 |
| 735e84ab-b116-42da-a701-34e1b17f648e | 31691, 61396 | 30431 | 61396 | 61396 |
| ed7216d5-62e1-43d1-a291-416095a04d77 | 29774, 61397 | 28516 | 61397 | 61397 |
| 3d2f945a-a3a2-4f03-9559-8dda3a62e79f | 34404, 61398 | 33108 | 61398 | 61398 |
| 516c1f7c-1b06-405a-8591-8a36aac32a32 | 29695, 61399 | 28436 | 61399 | 61399 |
| 4cbd7ef5-d649-497f-aabb-da2a027ebb75 | 34209, 61400 | 32899 | 61400 | 61400 |
| 801c3c4e-6376-44a9-8e36-371e89cd98a3 | 34316, 61401 | 33000 | 61401 | 61401 |
| 172ef2b6-09f7-4928-8569-99c6523c5e15 | 34399, 61402 | 33103 | 61402 | 61402 |
| 42c4bfdd-65df-476b-a136-05b912fd9e6c | 34475, 61403 | 33183 | 61403 | 61403 |
| 6f76a2f5-a53f-49c0-8c27-14cb9d21b05b | 28048, 61404 | 26842 | 61404 | 61404 |
| 35449786-653f-41d0-93ef-2f97ee95fc5b | 28134, 61405 | 26925 | 61405 | 61405 |
| 0ffcd0cc-34d8-41c2-8830-c94a0335cbcd | 28297, 61406 | 27075 | 61406 | 61406 |
| cd5f315a-3861-4921-b011-9ca19c96b921 | 28377, 61407 | 27153 | 61407 | 61407 |
| 4740f120-3a67-47e3-9e09-ef8c1d28e0cd | 28462, 61408 | 27236 | 61408 | 61408 |
| d225b526-a463-4fd9-9453-279212ea2fff | 25926, 61409 | 24846 | 61409 | 61409 |
| 43127eeb-8cdb-47bf-a51a-29c71c7e817c | 28523, 61410 | 27293 | 61410 | 61410 |
| def6508f-a2c8-47d7-b7b3-b5477f017db8 | 34593, 61411 | 33295 | 61411 | 61411 |
| bf60fd1d-89e4-4481-a7a0-ff3ea92c919e | 15809, 61412 | 15034 | 61412 | 61412 |
| e6a3e893-adbd-4fdc-aaf7-57fbc0a7a868 | 15968, 61413 | 15195 | 61413 | 61413 |
| 5f810c17-5973-4492-93cb-957cb61435d2 | 19798, 61414 | 18935 | 61414 | 61414 |
| 6b6ac8c9-0ad1-4356-89f3-bfd28683b037 | 15696, 61416 | 14916 | 61416 | 61416 |
| cf83e4d6-ab45-46e5-845d-bab43fc0f2bb | 15697, 61417 | 14917 | 61417 | 61417 |
| 90ab27ed-e04b-49b9-9706-df06b1676035 | 8593, 61418 | 8144 | 61418 | 61418 |
| 1ca317cc-878c-474e-89dd-071923d5db89 | 9880, 61419 | 9323 | 61419 | 61419 |
| acdf25aa-93ad-4949-ae5a-e99b03c9f22d | 10008, 61420 | 9448 | 61420 | 61420 |
| 06d09bf6-543e-4c5e-b44d-6f0a1ccf7c3b | 15253, 61421 | 14484 | 61421 | 61421 |
| dcd4cefd-7fb6-4599-9119-2b1d38a64216 | 15520, 61422 | 14752 | 61422 | 61422 |
| 4221e39f-f78e-4939-856e-010e562cc182 | 15625, 61423 | 14849 | 61423 | 61423 |
| 0d7afa93-0637-4499-9175-874412bcd7be | 6839, 61424 | 6524 | 61424 | 61424 |
| 8f51a3fd-3c96-4b4c-953b-1d980a693355 | 10287, 61425 | 9738 | 61425 | 61425 |
| e772fc48-bd91-4851-8f64-10d96f9aa2f4 | 15592, 61426 | 14816 | 61426 | 61426 |
| 8ba0a0c8-a553-4bfb-b053-a5df4baf8604 | 15698, 61427 | 14918 | 61427 | 61427 |
| 9e90556f-c3f3-4c14-a1de-dcd383b9a449 | 15812, 61428 | 15037 | 61428 | 61428 |
| 1ed4b63d-caa4-47c3-bf50-72db5ecd56bd | 15985, 61429 | 15211 | 61429 | 61429 |

### Ranking Exception: Payments 22847 And 61415

Payment `22847` has no forward donation link and is created/completed roughly a second after `61415`. Donation `21866` confirms `61415`, which also has the forward donation link. The existing ranking would therefore retain the ID on `22847`, while the confirmation-backed proposal retains it on `61415`. This is a concrete reason not to approve a blanket newest-row rule. The current script blocks this group, so that selection has not been applied.

## B2: Confirmed Payment Plus Zero-Value Rows

All 13 groups have one completed, positive-value payment, confirmed by the donation shown, and one or more unfinished rows whose requested and paid amounts are both zero. Types and populated transaction keys agree within each group. These zero-value rows are consistent with unfinished attempts or placeholders, but that explanation still requires review rather than being asserted as fact.

The candidate's requested amount, paid amount, and confirmed donation amount agree at the displayed currency precision. Clearing an extra provider ID must not change its zero amounts, status, donation links, or any other values.

| Provider ID | Candidate owner | Confirmed donation | Candidate paid amount | Other payment IDs with requested/paid = 0 |
| --- | ---: | ---: | ---: | --- |
| b2294574-3c0d-4981-8f5f-b86709360683 | 4441 | 4231 | 0.70 | 4442 |
| 868dba43-6e13-4afa-ac2d-931fd82fb850 | 9598 | 9046 | 5.40 | 9599 |
| f91e063c-1846-4cb8-bd89-13c9c4eb575d | 9807 | 9251 | 15.00 | 9808, 9809 |
| f6398c4a-979b-41f6-a0e8-bc6f515cd298 | 9970 | 9407 | 15.00 | 9971 |
| e09e59df-b4b8-4582-8b9e-54fae95f126d | 10145 | 9573 | 15.00 | 10146 |
| 8cff3c4f-4135-4462-9f66-20038d7f1b43 | 10189 | 9613 | 10.10 | 10190, 10191, 10192 |
| 7767523f-e766-4115-a074-304af171018f | 10310 | 9758 | 10.10 | 10311, 10312 |
| 255118b1-79f3-4d88-8d8d-f225953a95ee | 10395 | 9831 | 15.00 | 10396 |
| c0fa39ec-50b3-4ab5-9ace-4a1c5e2c6411 | 10432 | 9859 | 10.10 | 10433, 10434 |
| 8363076e-b53f-4604-82af-89e15e450a48 | 13514 | 12826 | 15.00 | 13515 |
| ee5e6ba8-2602-4f72-9b73-2edde07ac1d3 | 15657 | 14877 | 20.70 | 15658 |
| 33ce8cd5-7ff0-4704-8b56-f0c1e1b13670 | 15765 | 14992 | 20.70 | 15766, 15767, 15768 |
| d6da087b-ded0-49c2-b1ee-0354f7a22c7c | 28217 | 26998 | 10.40 | 28218, 28219 |

The first two groups span forward-linked donations: `4442` links to donation `4232`, and `9599` links to donation `9047`. Neither extra payment is confirmed by a donation. Preserve those relationships; selecting the completed candidate does not authorize deleting or cancelling the other donation.

## C: Six Unresolved Groups

No owner is proposed for these groups. Current ranking is listed only to show what a blanket ranking rule would do.

| Provider ID | Payments and confirmed donations | Paid amounts | Timing evidence | Current rank, not approved |
| --- | --- | --- | --- | ---: |
| 6bb9314a-957c-49e2-a743-8cd335c1157b | 4407 -> 4196; 4416 -> 4205; 4422 -> 4210; 4427 -> 4216 | 0.70 each | Four completed rows created on 2021-10-24, 25, 26, and 27. | 4427 |
| 18faf489-5b1c-4d7d-8937-595e5821399b | 45576 and 45577: no forward links or confirmed donations | 0.00 each | Neither completed; created 2025-05-30 12:49:38.966 and 12:49:40.441. Transaction keys differ. | 45577 |
| 48dd984d-7770-4549-b24b-4fcee024139d | 61665 -> 59319; 61666 -> 59320 | 55.00 each | Both created/completed 2026-10-05 11:25:10. | 61666 |
| 3d282805-a559-4976-a145-59089ee46edf | 62044 -> 59655; 62045 -> 59656 | 14.50 each | Both created/completed 2026-10-28 10:05:09. | 62045 |
| 19d852ca-7bcc-49f7-93f6-4cdbee5a9ca4 | 62104 -> 59708; 62105 -> 59709 | 10.05 each | Both created/completed 2026-11-01 11:45:14. | 62105 |
| 4ad9c015-a495-4033-a63f-efc3eb8b8c8c | 62107 -> 59712; 62108 -> 59713 | 60.30 each | Both created/completed 2026-11-01 18:12:17. | 62108 |

The five multi-owner groups have matching types, transaction keys, and requested/paid amounts. That does not resolve the competing confirmations.

### Decisions Needed For C

1. For `4407/4416/4422/4427`, check provider capture/event records across the four dates and any historical use of a subscription identifier as a payment identifier. Two of the confirmed donations (`4210`, `4216`) have invoices. Do not assume that one identifier means one charge, or that four confirmations mean four charges.
2. For `45576/45577`, check whether either attempt is still meaningful or has provider-side evidence. An explicitly approved latest-attempt owner is possible, but remains a policy choice rather than a confirmed financial match. Retiring the ID on both is another possible policy; it would need a script/audit design change and review of provider lookups, not an implicit fallback.
3. For each of the four recent paired confirmations, establish whether one provider capture was recorded against two donations or two genuine captures were assigned the same stored ID. Use provider records and payment-processing logs; identical timestamps and amounts alone cannot distinguish them. If accounting records are duplicated, handle that in a separate reviewed correction, not by modifying donation confirmations in this script.

## Options

| Option | What changes | Benefit | Cost or risk | Recommendation |
| --- | --- | --- | --- | --- |
| Restore genuine provider IDs | Replace incorrect IDs with verified, distinct payment IDs; audit every original value and replacement. | Best preserves real provider-to-payment identity when rows represent different genuine captures. | Requires authoritative evidence and validation that each replacement is a payment ID, not a subscription ID; ensure tenant-wide uniqueness. | Prefer for proven distinct charges. |
| Approve explicit canonical mappings | Keep the existing ID on the reviewed owner; audit original IDs and clear only other occurrences to `NULL`. | Fits the current filtered unique index, preserves every row/link, and makes ownership decisions reviewable. | ID-only lookups will resolve only to the chosen owner. Preserved history is not proof that accounting inconsistencies were corrected. | Recommended for verified copies/unfinished duplicate attempts; review all candidates before approval. |
| Defer unresolved groups | Keep data unchanged while gathering provider evidence. | Avoids an unsupported ownership decision. | Any remaining duplicate non-null IDs continue to block the migration, even if other groups are reconciled. | Keep C blocked until an explicit decision is recorded. |
| Explicit historical ownership policy | Where evidence cannot be recovered, an authorized owner accepts a documented deterministic rule for historical IDs, or retirement of an unusable ID. Retain the original values and the reason. | Can resolve otherwise irrecoverable historical ambiguity without deleting rows. | Does not establish the true capture history; can misroute ID-based lookups. Retirement of all occurrences is outside the current script/audit contract. | Exception only, with explicit business approval and tested lookup behavior; never a silent default. |
| Redesign the uniqueness contract | Change the model/index and every lookup to support shared provider IDs or a separate provider-event identity. | Appropriate only if shared IDs are genuinely part of the domain. | Changes the hardening/idempotency guarantees and needs a separately designed migration and application changes. | Not a shortcut for the pending migration. |
| Delete records, invent IDs, or remove safeguards | Merge/delete history, fabricate provider IDs, or bypass validation/uniqueness. | May superficially unblock index creation. | Violates preservation requirements or conceals unresolved identity problems. | Do not use. |

## Recommended Approval And Rollout

1. Review A, B1, and B2 candidates; investigate A group `60989/60990` and the B1 ranking exception explicitly. Use provider evidence where available and record why each selected owner is correct.
2. Resolve the six C groups or obtain an explicit, recorded exception policy. Do not mark groups approved merely because they appear in this report.
3. Add a reviewed mapping mechanism to the reconciliation script in a separate change. Each approval should identify the provider ID, selected owner, expected member IDs/evidence, reviewer, and reason. Fail if an unreviewed group appears or the approved membership/evidence changes. Keep the existing audit, transaction, and no-deletion guarantees.
4. If genuine replacement provider IDs are needed, extend the audit design in that change to preserve original and replacement values; do not silently reuse the current null-clearing-only assumptions.
5. On a backed-up, restorable local copy, complete a successful dry run first, then explicitly commit the reviewed reconciliation. Check zero duplicate non-null IDs, expected audit mappings, preserved payment/notification counts and relationships, and no unintended field changes.
6. Apply the actual pending migration locally, then validate payment-ID lookups, recurring-payment handling, notification retry behavior, and relevant automated tests. The data checks in this report are not a substitute for that rehearsal.
7. Refresh this evidence against each target tenant before rollout; the local snapshot cannot authorize changes to a database that has changed. During an agreed maintenance window, back up, pause application/background writers, review a fresh dry run, apply the approved reconciliation, apply migrations in order, and resume only after verification.

The reconciliation script and pending migration have deliberately not been modified by this report. Production execution and selection of unresolved owners are not approved.