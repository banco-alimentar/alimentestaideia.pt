-- Plans duplicate-ID reconciliation, identifier retirement, and explicitly approved deletion of superseded failed groups.
-- Run per tenant database BEFORE applying migration HardenPaymentConcurrency.
-- Plan mode returns reviewable JSON without changing application data or schema.
-- To execute, paste the reviewed JSON into @ApprovedPlan, set @Mode = N'Reconcile',
-- and retain @DryRun = 1 until the approved plan has passed a dry run.
-- Pause application/background writers before reconciling; only @DryRun = 0 commits.

IF @@TRANCOUNT > 0
BEGIN
  RAISERROR('Run this script without an existing transaction. No changes were made.', 10, 1);
  RETURN;
END;

SET XACT_ABORT ON;
SET NOCOUNT ON;
SET LOCK_TIMEOUT 15000;
DECLARE @Mode nvarchar(10) = N'Plan';
DECLARE @DryRun bit = 1;
DECLARE @ApprovedPlan nvarchar(max) = NULL;

BEGIN TRY
  BEGIN TRANSACTION;

  IF @Mode IS NULL OR @Mode NOT IN (N'Plan', N'Reconcile')
    THROW 51005, 'Mode must be Plan or Reconcile.', 1;

  DECLARE @LockedRows bigint;
  IF @Mode = N'Reconcile'
    SELECT @LockedRows = COUNT_BIG(*) FROM dbo.Payments WITH (TABLOCKX, HOLDLOCK);
  ELSE
    SELECT @LockedRows = COUNT_BIG(*) FROM dbo.Payments WITH (TABLOCK, HOLDLOCK);

  SELECT @LockedRows = COUNT_BIG(*) FROM dbo.Donations WITH (TABLOCK, HOLDLOCK);
  SELECT @LockedRows = COUNT_BIG(*) FROM dbo.Invoices WITH (TABLOCK, HOLDLOCK);

  IF OBJECT_ID('tempdb..#PaymentFacts') IS NOT NULL DROP TABLE #PaymentFacts;
  IF OBJECT_ID('tempdb..#GroupPlan') IS NOT NULL DROP TABLE #GroupPlan;
  IF OBJECT_ID('tempdb..#ApprovedGroups') IS NOT NULL DROP TABLE #ApprovedGroups;
  IF OBJECT_ID('tempdb..#PaymentDependencies') IS NOT NULL DROP TABLE #PaymentDependencies;
  IF OBJECT_ID('tempdb..#DeletePayments') IS NOT NULL DROP TABLE #DeletePayments;

  ;WITH Ranked AS
  (
    SELECT payment.Id AS PaymentId, payment.EasyPayPaymentId, payment.DonationId,
           payment.Created, payment.Completed, payment.Discriminator AS PaymentType,
          payment.TransactionKey, payment.Requested, payment.Paid, payment.Status,
           MIN(payment.Id) OVER (PARTITION BY EasyPayPaymentId) AS GroupId,
           ROW_NUMBER() OVER (
             PARTITION BY EasyPayPaymentId
             ORDER BY CASE WHEN Completed IS NOT NULL THEN 0 ELSE 1 END,
                      Created DESC,
                      Id DESC) AS CurrentRank,
           COUNT(*) OVER (PARTITION BY EasyPayPaymentId) AS PaymentCount
    FROM dbo.Payments payment
    WHERE EasyPayPaymentId IS NOT NULL
  )
  SELECT ranked.*, confirmation.ConfirmationCount, confirmation.ConfirmedDonationId,
         confirmation.ConfirmedDonationAmount, confirmation.ConfirmedDonationPaymentStatus,
         statusDonation.StatusDonationId, statusDonation.DonationPaymentStatus,
         relationshipReview.HasExternalDonationConfirmation,
         CONVERT(bit, CASE WHEN donation.PaymentStatus = 1 AND donation.ConfirmedPaymentId IS NULL
                          THEN 1 ELSE 0 END) AS LinkedToUnconfirmedPaidDonation,
         CONVERT(bit, CASE WHEN ranked.DonationId IS NULL THEN 1 ELSE 0 END) AS MissingDonationLink,
         CONVERT(bit, CASE WHEN ranked.DonationId IS NULL OR relationshipReview.HasExternalDonationConfirmation = 1
                          THEN 1 ELSE 0 END) AS RelationshipDeletionCandidate,
         donation.ConfirmedPaymentId AS DonationConfirmedPaymentId,
         replacement.Status AS ReplacementPaymentStatus, replacement.Paid AS ReplacementPaid,
         CONVERT(bit, CASE WHEN donation.PaymentStatus = 1 AND replacement.Id IS NOT NULL
          AND (replacement.DonationId IS NULL OR replacement.DonationId = donation.Id)
          AND ((LOWER(LTRIM(RTRIM(replacement.Status)) COLLATE Latin1_General_100_CI_AS)
                  IN (N'paid', N'success', N'ok', N'approved', N'completed')
                AND replacement.Paid > 0
                AND CONVERT(decimal(19,4), replacement.Paid) = CONVERT(decimal(19,4), donation.DonationAmount))
            -- PayPal rows have no amount columns; the app accepts COMPLETED with payment and payer IDs.
            OR (replacement.Discriminator = N'PayPalPayment'
                AND LOWER(LTRIM(RTRIM(replacement.Status)) COLLATE Latin1_General_100_CI_AS) = N'completed'
                AND NULLIF(LTRIM(RTRIM(replacement.PayPalPaymentId)), N'') IS NOT NULL
                AND NULLIF(LTRIM(RTRIM(replacement.PayerId)), N'') IS NOT NULL))
          AND NOT EXISTS (SELECT 1 FROM Ranked member WHERE member.GroupId = ranked.GroupId
                  AND member.PaymentId = replacement.Id)
          THEN 1 ELSE 0 END) AS HasSuccessfulReplacement,
         CONVERT(nvarchar(30), CASE WHEN ranked.Status IS NOT NULL THEN N'Payment'
                                   WHEN donation.Id IS NOT NULL THEN N'Donation'
                                   WHEN confirmation.ConfirmationCount = 1 THEN N'ConfirmedDonation'
                                   ELSE N'Unknown' END) AS StatusSource,
         CONVERT(bit, CASE
           WHEN ranked.Status IS NOT NULL THEN
             CASE WHEN LOWER(LTRIM(RTRIM(ranked.Status)) COLLATE Latin1_General_100_CI_AS)
                            IN (N'paid', N'success', N'ok', N'approved', N'completed') THEN 1 ELSE 0 END
           WHEN statusDonation.DonationPaymentStatus = 1 THEN 1
           WHEN statusDonation.DonationPaymentStatus IN (2, 3, 4) THEN 0
           ELSE NULL END) AS IsSuccessfulStatus
  INTO #PaymentFacts
  FROM Ranked ranked
  LEFT JOIN dbo.Donations donation ON donation.Id = ranked.DonationId
  LEFT JOIN dbo.Payments replacement ON replacement.Id = donation.ConfirmedPaymentId
  OUTER APPLY
  (
    SELECT COUNT(*) AS ConfirmationCount, MIN(donation.Id) AS ConfirmedDonationId,
           MIN(donation.DonationAmount) AS ConfirmedDonationAmount,
           MIN(donation.PaymentStatus) AS ConfirmedDonationPaymentStatus
    FROM dbo.Donations donation
    WHERE donation.ConfirmedPaymentId = ranked.PaymentId
  ) confirmation
  CROSS APPLY
  (
    SELECT CASE WHEN donation.Id IS NOT NULL THEN donation.Id
                WHEN confirmation.ConfirmationCount = 1 THEN confirmation.ConfirmedDonationId END AS StatusDonationId,
           CASE WHEN donation.Id IS NOT NULL THEN donation.PaymentStatus
                WHEN confirmation.ConfirmationCount = 1 THEN confirmation.ConfirmedDonationPaymentStatus END AS DonationPaymentStatus
  ) statusDonation
  CROSS APPLY
  (
    SELECT CONVERT(bit, CASE WHEN donation.PaymentStatus = 1 AND donation.ConfirmedPaymentId IS NOT NULL
                 AND NOT EXISTS (SELECT 1 FROM Ranked member WHERE member.GroupId = ranked.GroupId
                                 AND member.PaymentId = donation.ConfirmedPaymentId)
               THEN 1 ELSE 0 END) AS HasExternalDonationConfirmation
  ) relationshipReview
  WHERE ranked.PaymentCount > 1;

  ;WITH GroupFacts AS
  (
    SELECT GroupId, COUNT(*) AS PaymentCount,
           SUM(CASE WHEN DonationId IS NULL THEN 1 ELSE 0 END) AS OrphanCount,
           SUM(CASE WHEN RelationshipDeletionCandidate = 1 OR IsSuccessfulStatus = 0 THEN 1 ELSE 0 END) AS DeletionCandidateCount,
           SUM(CASE WHEN HasExternalDonationConfirmation = 1 THEN 1 ELSE 0 END) AS ExternalConfirmationCandidateCount,
           SUM(CASE WHEN (RelationshipDeletionCandidate = 1 OR IsSuccessfulStatus = 0)
                AND ((IsSuccessfulStatus = 1 AND NOT (StatusSource = N'Donation' AND HasExternalDonationConfirmation = 1))
                  OR Completed IS NOT NULL OR Paid > 0 OR ConfirmationCount > 0
                  OR (DonationPaymentStatus = 1 AND HasSuccessfulReplacement = 0))
              THEN 1 ELSE 0 END) AS DeletionFinancialConflictCount,
           SUM(CASE WHEN (RelationshipDeletionCandidate = 1 OR IsSuccessfulStatus = 0)
                AND IsSuccessfulStatus IS NULL
                AND NOT (DonationId IS NULL AND ConfirmationCount = 0 AND Completed IS NULL AND Paid = 0)
              THEN 1 ELSE 0 END) AS DeletionUnknownStatusCount,
           SUM(CASE WHEN Completed IS NOT NULL THEN 1 ELSE 0 END) AS CompletedCount,
           SUM(CASE WHEN IsSuccessfulStatus = 1 THEN 1 ELSE 0 END) AS SuccessfulStatusCount,
           -- Fallback success from a donation paid by another payment is not evidence about this row.
           SUM(CASE WHEN IsSuccessfulStatus = 1 AND NOT (StatusSource = N'Donation' AND HasExternalDonationConfirmation = 1)
                    THEN 1 ELSE 0 END) AS OwnSuccessCount,
           SUM(CASE WHEN IsSuccessfulStatus = 0 THEN 1 ELSE 0 END) AS NonSuccessfulStatusCount,
           SUM(CASE WHEN IsSuccessfulStatus IS NULL THEN 1 ELSE 0 END) AS UnknownStatusCount,
           MIN(CASE WHEN IsSuccessfulStatus = 1 THEN PaymentId ELSE 2147483647 END) AS SuccessfulPaymentId,
           SUM(CASE WHEN IsSuccessfulStatus = 0 AND (Completed IS NOT NULL OR Paid > 0 OR ConfirmationCount > 0
                         OR (DonationPaymentStatus = 1 AND HasSuccessfulReplacement = 0))
                    THEN 1 ELSE 0 END) AS StatusConflictCount,
           SUM(CASE WHEN Completed IS NOT NULL OR Paid > 0 OR ConfirmationCount > 0
                         OR (DonationPaymentStatus = 1 AND HasSuccessfulReplacement = 0) THEN 1 ELSE 0 END) AS FinancialEvidenceCount,
           SUM(CASE WHEN Completed IS NULL AND Paid = 0 AND ConfirmationCount = 0
                     AND ((HasSuccessfulReplacement = 1
                           AND (IsSuccessfulStatus = 0
                             OR (IsSuccessfulStatus = 1 AND StatusSource = N'Donation' AND HasExternalDonationConfirmation = 1)))
                       OR (DonationId IS NULL AND COALESCE(IsSuccessfulStatus, 0) = 0))
                    THEN 1 ELSE 0 END) AS DeletableAttemptCount,
           SUM(CASE WHEN ConfirmationCount > 0 THEN 1 ELSE 0 END) AS ConfirmedOwnerCount,
           SUM(CASE WHEN LinkedToUnconfirmedPaidDonation = 1 THEN 1 ELSE 0 END) AS UnconfirmedPaidDonationLinkCount,
           SUM(CASE WHEN Paid > 0 THEN 1 ELSE 0 END) AS PaidMemberCount,
           CASE WHEN COUNT(DISTINCT COALESCE(NULLIF(TransactionKey, N''), N'')) = 1
                  AND SUM(CASE WHEN NULLIF(TransactionKey, N'') IS NULL THEN 1 ELSE 0 END) = 0
                THEN 1 ELSE 0 END AS SameTransactionKey,
           CASE WHEN SUM(CASE WHEN Requested IS NULL OR Paid IS NULL THEN 1 ELSE 0 END) = 0
                  AND MIN(COALESCE(Requested, 0)) = MAX(COALESCE(Requested, 0))
                  AND MIN(COALESCE(Paid, 0)) = MAX(COALESCE(Paid, 0))
                THEN 1 ELSE 0 END AS SameAmounts
    FROM #PaymentFacts
    GROUP BY GroupId
  )
  SELECT grouped.*, ranked.EasyPayPaymentId, ranked.PaymentId AS CurrentRankPaymentId,
         candidate.PaymentId AS ConfirmedCandidatePaymentId,
         evidenceRule.DecisionRule,
         CASE WHEN evidenceRule.DecisionRule = N'ConfirmedPaid' THEN candidate.PaymentId
              WHEN evidenceRule.DecisionRule IN (N'MultiConfirmed', N'NoOwnerRetire') THEN NULL
              WHEN grouped.SuccessfulStatusCount = 1 THEN grouped.SuccessfulPaymentId
              WHEN evidenceRule.DecisionRule = N'ReviewConfirmedOwner' THEN candidate.PaymentId END AS SuggestedCanonicalPaymentId,
            CONVERT(bit, CASE WHEN grouped.DeletionCandidateCount = grouped.PaymentCount THEN 1 ELSE 0 END) AS DeletionCandidate,
            CONVERT(bit, CASE WHEN grouped.DeletionFinancialConflictCount > 0
                 THEN 1 ELSE 0 END) AS DeletionBlockedByFinancialEvidence,
          CONVERT(bit, CASE WHEN grouped.DeletableAttemptCount = grouped.PaymentCount THEN 1 ELSE 0 END) AS HasSafeDeletionEvidence,
         CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), CONCAT(
           (SELECT payment.* FROM dbo.Payments payment
            JOIN #PaymentFacts member ON member.PaymentId = payment.Id
            WHERE member.GroupId = grouped.GroupId ORDER BY payment.Id FOR JSON PATH, INCLUDE_NULL_VALUES),
           N'|',
           (SELECT donation.* FROM dbo.Donations donation
            WHERE EXISTS (SELECT 1 FROM #PaymentFacts member WHERE member.GroupId = grouped.GroupId
                          AND (member.PaymentId = donation.ConfirmedPaymentId OR member.DonationId = donation.Id))
            ORDER BY donation.Id FOR JSON PATH, INCLUDE_NULL_VALUES),
           N'|',
           (SELECT replacement.* FROM dbo.Payments replacement
            WHERE EXISTS (SELECT 1 FROM #PaymentFacts member WHERE member.GroupId = grouped.GroupId
                          AND member.DonationConfirmedPaymentId = replacement.Id)
            ORDER BY replacement.Id FOR JSON PATH, INCLUDE_NULL_VALUES)
         ))), 2) AS ExpectedEvidenceHash
  INTO #GroupPlan
  FROM GroupFacts grouped
  JOIN #PaymentFacts ranked ON ranked.GroupId = grouped.GroupId AND ranked.CurrentRank = 1
  OUTER APPLY
  (
    SELECT TOP (1) member.* FROM #PaymentFacts member
    WHERE member.GroupId = grouped.GroupId AND member.ConfirmationCount > 0 ORDER BY member.PaymentId
  ) candidate
  CROSS APPLY
  (
    SELECT CASE
      WHEN grouped.ConfirmedOwnerCount > 1 THEN N'MultiConfirmed'
      -- A Payed donation without ConfirmedPaymentId may have been paid by this group.
      WHEN grouped.UnconfirmedPaidDonationLinkCount > 0 THEN N'ReviewUnconfirmedPaidDonation'
      WHEN grouped.ConfirmedOwnerCount = 1 AND candidate.ConfirmationCount = 1
       AND candidate.ConfirmedDonationPaymentStatus = 1
       AND (candidate.DonationId IS NULL OR candidate.DonationId = candidate.ConfirmedDonationId)
       AND candidate.IsSuccessfulStatus = 1 AND candidate.Paid > 0
       AND CONVERT(decimal(19,4), candidate.Paid) = CONVERT(decimal(19,4), candidate.ConfirmedDonationAmount)
        THEN N'ConfirmedPaid'
      WHEN grouped.ConfirmedOwnerCount = 1 THEN N'ReviewConfirmedOwner'
      WHEN grouped.OwnSuccessCount = 0 AND grouped.CompletedCount = 0 AND grouped.PaidMemberCount = 0
        THEN N'NoOwnerRetire'
      ELSE N'ReviewUnconfirmedCapture' END AS DecisionRule
  ) evidenceRule;

  CREATE TABLE #PaymentDependencies
  (
    PaymentId int NOT NULL,
    ReferencingTable nvarchar(517) NOT NULL,
    ReferencingColumn sysname NOT NULL,
    ReferenceCount bigint NOT NULL
  );

  DECLARE @DependencyCheckSupported bit = CASE WHEN HAS_PERMS_BY_NAME(DB_NAME(), N'DATABASE', N'VIEW DEFINITION') = 1
    AND NOT EXISTS (
      SELECT 1 FROM sys.foreign_keys foreignKey
      JOIN sys.foreign_key_columns keyColumn ON keyColumn.constraint_object_id = foreignKey.object_id
      JOIN sys.columns referencedColumn ON referencedColumn.object_id = keyColumn.referenced_object_id
                                       AND referencedColumn.column_id = keyColumn.referenced_column_id
      WHERE foreignKey.referenced_object_id = OBJECT_ID(N'dbo.Payments')
        AND (referencedColumn.name <> N'Id' OR
             (SELECT COUNT(*) FROM sys.foreign_key_columns component WHERE component.constraint_object_id = foreignKey.object_id) <> 1)
    ) THEN 1 ELSE 0 END;

  DECLARE @DependencySql nvarchar(max) = N'';
  SELECT @DependencySql += N'INSERT #PaymentDependencies (PaymentId, ReferencingTable, ReferencingColumn, ReferenceCount) '
    + N'SELECT member.PaymentId, N''' + REPLACE(OBJECT_SCHEMA_NAME(foreignKey.parent_object_id) + N'.' + OBJECT_NAME(foreignKey.parent_object_id), N'''', N'''''')
    + N''', N''' + REPLACE(parentColumn.name, N'''', N'''''') + N''', COUNT_BIG(*) FROM '
    + QUOTENAME(OBJECT_SCHEMA_NAME(foreignKey.parent_object_id)) + N'.' + QUOTENAME(OBJECT_NAME(foreignKey.parent_object_id))
    + N' dependency WITH (TABLOCK, HOLDLOCK) JOIN #PaymentFacts member ON dependency.' + QUOTENAME(parentColumn.name)
    + N' = member.PaymentId JOIN #GroupPlan grouped ON grouped.GroupId = member.GroupId '
    + N'WHERE member.RelationshipDeletionCandidate = 1 OR member.IsSuccessfulStatus = 0 GROUP BY member.PaymentId;' + CHAR(10)
  FROM sys.foreign_keys foreignKey
  JOIN sys.foreign_key_columns keyColumn ON keyColumn.constraint_object_id = foreignKey.object_id
  JOIN sys.columns parentColumn ON parentColumn.object_id = keyColumn.parent_object_id AND parentColumn.column_id = keyColumn.parent_column_id
  JOIN sys.columns referencedColumn ON referencedColumn.object_id = keyColumn.referenced_object_id AND referencedColumn.column_id = keyColumn.referenced_column_id
  WHERE foreignKey.referenced_object_id = OBJECT_ID(N'dbo.Payments') AND referencedColumn.name = N'Id'
    AND (SELECT COUNT(*) FROM sys.foreign_key_columns component WHERE component.constraint_object_id = foreignKey.object_id) = 1;

  IF EXISTS (SELECT 1 FROM #GroupPlan WHERE DeletionCandidateCount > 0)
    EXEC sys.sp_executesql @DependencySql;

  DECLARE @ServerName nvarchar(128) = CONVERT(nvarchar(128), SERVERPROPERTY('ServerName'));
  DECLARE @DatabaseName nvarchar(128) = DB_NAME();
  DECLARE @GroupEvidence nvarchar(max) = (
    SELECT GroupId, ExpectedEvidenceHash FROM #GroupPlan ORDER BY GroupId FOR JSON PATH);
  DECLARE @PlanEvidenceHash varchar(64) = CONVERT(varchar(64), HASHBYTES('SHA2_256',
    CONVERT(varbinary(max), CONCAT(@ServerName, N'|', @DatabaseName, N'|', @GroupEvidence))), 2);
  DECLARE @OverlengthProviderIds bigint = (SELECT COUNT_BIG(*) FROM dbo.Payments WHERE LEN(EasyPayPaymentId) > 128);
  DECLARE @ConflictingInvoiceGroups bigint = (
    SELECT COUNT_BIG(*) FROM (SELECT [Year], COALESCE(FoodBankId, 0) AS Scope, [Sequence]
      FROM dbo.Invoices GROUP BY [Year], COALESCE(FoodBankId, 0), [Sequence] HAVING COUNT_BIG(*) > 1) conflicts);

  IF @Mode = N'Plan'
  BEGIN
    SELECT (
      SELECT 5 AS PlanVersion, @ServerName AS ServerName, @DatabaseName AS DatabaseName,
             SYSUTCDATETIME() AS GeneratedAtUtc, @PlanEvidenceHash AS PlanEvidenceHash,
             CAST(NULL AS nvarchar(200)) AS ApprovedBy, CAST(NULL AS nvarchar(2000)) AS ApprovalReason,
             @OverlengthProviderIds AS OverlengthProviderIds, @ConflictingInvoiceGroups AS ConflictingInvoiceGroups,
             JSON_QUERY(COALESCE(NULLIF((
              SELECT groupPlan.GroupId, groupPlan.EasyPayPaymentId, groupPlan.ExpectedEvidenceHash, groupPlan.DecisionRule,
                  groupPlan.SuggestedCanonicalPaymentId,
                  CASE WHEN groupPlan.DecisionRule = N'ConfirmedPaid'
                    THEN groupPlan.SuggestedCanonicalPaymentId END AS CanonicalPaymentId,
                  CASE WHEN groupPlan.DecisionRule = N'ConfirmedPaid' THEN N'Only member confirmed by a Payed donation, with successful status and paid amount matching that donation.'
                    WHEN groupPlan.DecisionRule = N'NoOwnerRetire' THEN N'No member is confirmed, completed, paid, or successful by its own evidence, and no linked Payed donation lacks a confirmed payment.' END AS Reason,
                  groupPlan.CurrentRankPaymentId, groupPlan.ConfirmedOwnerCount, groupPlan.UnconfirmedPaidDonationLinkCount,
                  groupPlan.CompletedCount, groupPlan.PaidMemberCount, groupPlan.OwnSuccessCount,
                  groupPlan.SameTransactionKey, groupPlan.SameAmounts,
                  groupPlan.SuccessfulStatusCount, groupPlan.NonSuccessfulStatusCount, groupPlan.UnknownStatusCount,
                  groupPlan.StatusConflictCount, groupPlan.DeletionCandidate, groupPlan.DeletionBlockedByFinancialEvidence,
                  groupPlan.DeletionCandidateCount, groupPlan.OrphanCount AS MissingDonationCandidateCount,
                  groupPlan.ExternalConfirmationCandidateCount, groupPlan.DeletionUnknownStatusCount,
                  CONVERT(bit, CASE WHEN groupPlan.DeletionCandidateCount > 0 THEN 1 ELSE 0 END) AS HasDeletionCandidates,
                  groupPlan.HasSafeDeletionEvidence, @DependencyCheckSupported AS DependencyCheckSupported,
                  CONVERT(bit, CASE WHEN groupPlan.HasSafeDeletionEvidence = 1 AND @DependencyCheckSupported = 1
                      AND NOT EXISTS (SELECT 1 FROM #PaymentDependencies dependency JOIN #PaymentFacts member
                                      ON member.PaymentId = dependency.PaymentId WHERE member.GroupId = groupPlan.GroupId)
                      AND NOT EXISTS (SELECT 1 FROM sys.triggers WHERE parent_id = OBJECT_ID(N'dbo.Payments') AND is_disabled = 0)
                    THEN 1 ELSE 0 END) AS CanDeleteGroup,
                  CASE groupPlan.DecisionRule WHEN N'ConfirmedPaid' THEN N'ReconcileIdentifiers'
                    WHEN N'NoOwnerRetire' THEN N'RetireIdentifier' END AS Action,
                  CASE groupPlan.DecisionRule WHEN N'ConfirmedPaid' THEN N'ReconcileIdentifiers'
                    WHEN N'NoOwnerRetire' THEN N'RetireIdentifier'
                    WHEN N'MultiConfirmed' THEN N'ResolveDuplicateDonations' ELSE N'Review' END AS SuggestedAction,
                      JSON_QUERY((SELECT member.PaymentId, member.DonationId, member.PaymentType,
                                         member.Status, member.StatusSource, member.StatusDonationId,
                                         member.DonationPaymentStatus,
                                         CASE member.DonationPaymentStatus WHEN 1 THEN N'Payed' WHEN 2 THEN N'NotPayed'
                                           WHEN 3 THEN N'WaitingPayment' WHEN 4 THEN N'ErrorPayment' ELSE N'Unknown' END AS DonationPaymentStatusName,
                                         member.IsSuccessfulStatus, member.ConfirmationCount, member.ConfirmedDonationPaymentStatus,
                                         member.LinkedToUnconfirmedPaidDonation,
                                         member.MissingDonationLink, member.HasExternalDonationConfirmation,
                                         CONVERT(bit, CASE WHEN member.RelationshipDeletionCandidate = 1 OR member.IsSuccessfulStatus = 0
                                                 THEN 1 ELSE 0 END) AS DeletionCandidate,
                                         CASE WHEN member.MissingDonationLink = 1 THEN N'MissingDonationId'
                                           WHEN member.HasExternalDonationConfirmation = 1 THEN N'PaidDonationConfirmedOutsideGroup'
                                           WHEN member.IsSuccessfulStatus = 0 THEN N'UnsuccessfulStatus' END AS DeletionCandidateReason,
                                         CONVERT(bit, CASE WHEN (member.RelationshipDeletionCandidate = 1 OR member.IsSuccessfulStatus = 0)
                                           AND ((member.IsSuccessfulStatus = 1
                                                 AND NOT (member.StatusSource = N'Donation' AND member.HasExternalDonationConfirmation = 1))
                                             OR member.Completed IS NOT NULL OR member.Paid > 0
                                             OR member.ConfirmationCount > 0 OR (member.DonationPaymentStatus = 1 AND member.HasSuccessfulReplacement = 0))
                                           THEN 1 ELSE 0 END) AS DeletionBlockedByFinancialEvidence,
                                         CONVERT(bit, CASE WHEN (member.RelationshipDeletionCandidate = 1 OR member.IsSuccessfulStatus = 0)
                                           AND member.IsSuccessfulStatus IS NULL
                                           AND NOT (member.DonationId IS NULL AND member.ConfirmationCount = 0 AND member.Completed IS NULL AND member.Paid = 0)
                                           THEN 1 ELSE 0 END) AS DeletionBlockedByUnknownStatus,
                                         CASE WHEN member.Completed IS NULL AND member.Paid = 0 AND member.ConfirmationCount = 0
                                                AND member.HasSuccessfulReplacement = 1
                                                AND (member.IsSuccessfulStatus = 0 OR (member.IsSuccessfulStatus = 1
                                                  AND member.StatusSource = N'Donation' AND member.HasExternalDonationConfirmation = 1))
                                                THEN N'SupersededByReplacement'
                                              WHEN member.Completed IS NULL AND member.Paid = 0 AND member.ConfirmationCount = 0
                                                AND member.DonationId IS NULL AND COALESCE(member.IsSuccessfulStatus, 0) = 0
                                                THEN N'AbandonedOrphan' END AS DeletionBasis,
                                         member.DonationConfirmedPaymentId, member.ReplacementPaymentStatus,
                                         CONVERT(decimal(19,4), member.ReplacementPaid) AS ReplacementPaid,
                                         member.HasSuccessfulReplacement,
                                         JSON_QUERY(COALESCE(NULLIF((SELECT dependency.ReferencingTable, dependency.ReferencingColumn, dependency.ReferenceCount
                                           FROM #PaymentDependencies dependency WHERE dependency.PaymentId = member.PaymentId
                                           ORDER BY dependency.ReferencingTable, dependency.ReferencingColumn FOR JSON PATH), N''), N'[]')) AS Dependencies,
                                         member.Created, member.Completed,
                                         CONVERT(decimal(19,4), member.Requested) AS Requested,
                                         CONVERT(decimal(19,4), member.Paid) AS Paid,
                                         member.ConfirmedDonationId, member.ConfirmedDonationAmount
                                  FROM #PaymentFacts member WHERE member.GroupId = groupPlan.GroupId
                                  ORDER BY member.PaymentId FOR JSON PATH, INCLUDE_NULL_VALUES)) AS Members
               FROM #GroupPlan groupPlan ORDER BY groupPlan.GroupId FOR JSON PATH, INCLUDE_NULL_VALUES
             ), N''), N'[]')) AS Groups
      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES
    ) AS ReconciliationPlan;
    ROLLBACK TRANSACTION;
    RETURN;
  END;

  IF COALESCE(ISJSON(@ApprovedPlan), 0) <> 1
    THROW 51006, 'Supply the reviewed JSON plan in ApprovedPlan. No changes were made.', 1;

  DECLARE @ApprovedBy nvarchar(200) = JSON_VALUE(@ApprovedPlan, '$.ApprovedBy');
  DECLARE @ApprovalReason nvarchar(2000) = JSON_VALUE(@ApprovedPlan, '$.ApprovalReason');
  IF COALESCE(TRY_CONVERT(int, JSON_VALUE(@ApprovedPlan, '$.PlanVersion')), 0) <> 5
     OR NULLIF(LTRIM(RTRIM(@ApprovedBy)), N'') IS NULL
     OR NULLIF(LTRIM(RTRIM(@ApprovalReason)), N'') IS NULL
    THROW 51006, 'PlanVersion 5, ApprovedBy, and ApprovalReason are required. Regenerate old plans to review the confirmed-payment decision rules.', 1;

  IF JSON_VALUE(@ApprovedPlan, '$.ServerName') IS NULL OR JSON_VALUE(@ApprovedPlan, '$.DatabaseName') IS NULL
     OR JSON_VALUE(@ApprovedPlan, '$.PlanEvidenceHash') IS NULL
     OR JSON_VALUE(@ApprovedPlan, '$.ServerName') <> @ServerName
     OR JSON_VALUE(@ApprovedPlan, '$.DatabaseName') <> @DatabaseName
     OR JSON_VALUE(@ApprovedPlan, '$.PlanEvidenceHash') <> @PlanEvidenceHash
    THROW 51007, 'The plan targets a different database or its evidence has changed. Generate and review a fresh plan.', 1;

  IF JSON_QUERY(@ApprovedPlan, '$.Groups') IS NULL
     OR LEFT(LTRIM(JSON_QUERY(@ApprovedPlan, '$.Groups')), 1) <> N'['
    THROW 51006, 'The approved plan must contain a Groups array.', 1;

  SELECT GroupId, ExpectedEvidenceHash, CanonicalPaymentId, Reason, Action
  INTO #ApprovedGroups
  FROM OPENJSON(@ApprovedPlan, '$.Groups') WITH
  (
    GroupId int '$.GroupId', ExpectedEvidenceHash varchar(64) '$.ExpectedEvidenceHash',
    CanonicalPaymentId int '$.CanonicalPaymentId', Reason nvarchar(2000) '$.Reason', Action nvarchar(30) '$.Action'
  );

  IF EXISTS (SELECT 1 FROM #ApprovedGroups GROUP BY GroupId HAVING COUNT(*) > 1)
     OR EXISTS (SELECT 1 FROM #ApprovedGroups WHERE GroupId IS NULL)
     OR EXISTS (SELECT 1 FROM #ApprovedGroups approved FULL JOIN #GroupPlan actual ON actual.GroupId = approved.GroupId
                WHERE approved.GroupId IS NULL OR actual.GroupId IS NULL OR approved.ExpectedEvidenceHash IS NULL
                   OR approved.ExpectedEvidenceHash <> actual.ExpectedEvidenceHash)
    THROW 51007, 'Plan groups are missing, duplicated, unexpected, or stale. No partial reconciliation is allowed.', 1;

  IF EXISTS (SELECT 1 FROM #ApprovedGroups WHERE Action IS NULL OR Action NOT IN (N'ReconcileIdentifiers', N'RetireIdentifier', N'DeleteGroup'))
    THROW 51010, 'Every group needs a supported Action: ReconcileIdentifiers, RetireIdentifier, or DeleteGroup.', 1;

  IF EXISTS (SELECT 1 FROM #ApprovedGroups WHERE NULLIF(LTRIM(RTRIM(Reason)), N'') IS NULL
             OR (Action = N'ReconcileIdentifiers' AND CanonicalPaymentId IS NULL)
             OR (Action IN (N'RetireIdentifier', N'DeleteGroup') AND CanonicalPaymentId IS NOT NULL))
    THROW 51002, 'Every action needs a reason. ReconcileIdentifiers requires an owner; RetireIdentifier and DeleteGroup require CanonicalPaymentId null.', 1;

  IF EXISTS (SELECT 1 FROM #ApprovedGroups approved WHERE approved.Action = N'ReconcileIdentifiers' AND NOT EXISTS
             (SELECT 1 FROM #PaymentFacts member WHERE member.GroupId = approved.GroupId
                                                   AND member.PaymentId = approved.CanonicalPaymentId))
    THROW 51008, 'An approved canonical payment is not a member of its duplicate group.', 1;

  IF EXISTS (SELECT 1 FROM #ApprovedGroups approved JOIN #GroupPlan actual ON actual.GroupId = approved.GroupId
             WHERE approved.Action = N'RetireIdentifier' AND actual.ConfirmedOwnerCount > 0)
    THROW 51013, 'RetireIdentifier is not allowed when a member is confirmed by a donation. Choose an owner with ReconcileIdentifiers.', 1;

  SELECT member.PaymentId, member.GroupId, member.DonationConfirmedPaymentId AS ReplacementPaymentId
  INTO #DeletePayments
  FROM #PaymentFacts member JOIN #ApprovedGroups approved ON approved.GroupId = member.GroupId
  WHERE approved.Action = N'DeleteGroup';

  IF EXISTS (SELECT 1 FROM #ApprovedGroups approved JOIN #GroupPlan actual ON actual.GroupId = approved.GroupId
             WHERE approved.Action = N'DeleteGroup' AND actual.HasSafeDeletionEvidence = 0)
    THROW 51011, 'DeleteGroup requires only unpaid/uncompleted/unconfirmed attempts that are either superseded by a different confirmed successful payment for their paid donation or abandoned without any donation link or successful status.', 1;

  IF EXISTS (SELECT 1 FROM #DeletePayments)
  BEGIN
    IF @DependencyCheckSupported = 0
       OR EXISTS (SELECT 1 FROM sys.triggers WHERE parent_id = OBJECT_ID(N'dbo.Payments') AND is_disabled = 0)
      THROW 51012, 'DeleteGroup requires database VIEW DEFINITION, supported single-column foreign keys, and no enabled payment triggers.', 1;

    IF EXISTS (SELECT 1 FROM #PaymentDependencies dependency JOIN #DeletePayments target ON target.PaymentId = dependency.PaymentId)
      THROW 51012, 'DeleteGroup has dependent records. No cascade, SET NULL, notification deletion, or relationship rewrite is allowed.', 1;
  END;

  IF @OverlengthProviderIds > 0 OR @ConflictingInvoiceGroups > 0
    THROW 51009, 'Provider-ID lengths or invoice sequences still violate migration requirements. Resolve them separately.', 1;

  IF EXISTS (SELECT 1 FROM #GroupPlan)
  BEGIN
    IF OBJECT_ID(N'dbo.EasyPayPaymentIdReconciliationAudit', N'U') IS NULL
    BEGIN
      CREATE TABLE dbo.EasyPayPaymentIdReconciliationAudit
      (
        PaymentId int NOT NULL,
        CanonicalPaymentId int NOT NULL,
        OriginalEasyPayPaymentId nvarchar(max) NOT NULL,
        ReconciledAtUtc datetime2(7) NOT NULL,
        CONSTRAINT PK_EasyPayPaymentIdReconciliationAudit PRIMARY KEY (PaymentId)
      );
    END;

    IF OBJECT_ID(N'dbo.EasyPayPaymentIdReconciliationPlans', N'U') IS NULL
    BEGIN
      CREATE TABLE dbo.EasyPayPaymentIdReconciliationPlans
      (
        PlanEvidenceHash varchar(64) NOT NULL,
        ApprovedBy nvarchar(200) NOT NULL,
        ApprovalReason nvarchar(2000) NOT NULL,
        ApprovedPlan nvarchar(max) NOT NULL,
        AppliedAtUtc datetime2(7) NOT NULL,
        ExecutedBy nvarchar(128) NOT NULL,
        CONSTRAINT PK_EasyPayPaymentIdReconciliationPlans PRIMARY KEY (PlanEvidenceHash)
      );
    END;

    IF EXISTS
    (
      SELECT 1
      FROM dbo.EasyPayPaymentIdReconciliationAudit audit
      JOIN #PaymentFacts member ON member.PaymentId = audit.PaymentId
      JOIN #ApprovedGroups approved ON approved.GroupId = member.GroupId
      WHERE approved.Action = N'ReconcileIdentifiers' AND member.PaymentId <> approved.CanonicalPaymentId
    )
      THROW 51003, 'A duplicate payment already has a reconciliation audit record. Review its original provider ID before making further changes.', 1;

    DECLARE @AppliedAtUtc datetime2(7) = SYSUTCDATETIME();
    INSERT dbo.EasyPayPaymentIdReconciliationPlans
      (PlanEvidenceHash, ApprovedBy, ApprovalReason, ApprovedPlan, AppliedAtUtc, ExecutedBy)
    VALUES (@PlanEvidenceHash, @ApprovedBy, @ApprovalReason, @ApprovedPlan, @AppliedAtUtc, ORIGINAL_LOGIN());

    UPDATE payment
    SET EasyPayPaymentId = NULL
    OUTPUT deleted.Id, approved.CanonicalPaymentId, deleted.EasyPayPaymentId, @AppliedAtUtc
    INTO dbo.EasyPayPaymentIdReconciliationAudit
      (PaymentId, CanonicalPaymentId, OriginalEasyPayPaymentId, ReconciledAtUtc)
    FROM dbo.Payments payment
    JOIN #PaymentFacts member ON member.PaymentId = payment.Id
    JOIN #ApprovedGroups approved ON approved.GroupId = member.GroupId
    WHERE approved.Action = N'ReconcileIdentifiers' AND member.PaymentId <> approved.CanonicalPaymentId;

    SELECT audit.PaymentId, audit.CanonicalPaymentId, audit.OriginalEasyPayPaymentId, audit.ReconciledAtUtc
    FROM dbo.EasyPayPaymentIdReconciliationAudit audit
    JOIN #PaymentFacts member ON member.PaymentId = audit.PaymentId
    JOIN #ApprovedGroups approved ON approved.GroupId = member.GroupId
    WHERE approved.Action = N'ReconcileIdentifiers' AND member.PaymentId <> approved.CanonicalPaymentId
    ORDER BY audit.CanonicalPaymentId, audit.PaymentId;

    IF EXISTS (SELECT 1 FROM #ApprovedGroups WHERE Action = N'RetireIdentifier')
    BEGIN
      IF OBJECT_ID(N'dbo.EasyPayPaymentIdRetirementAudit', N'U') IS NULL
      BEGIN
        CREATE TABLE dbo.EasyPayPaymentIdRetirementAudit
        (
          PaymentId int NOT NULL,
          GroupId int NOT NULL,
          OriginalEasyPayPaymentId nvarchar(max) NOT NULL,
          PlanEvidenceHash varchar(64) NOT NULL,
          RetiredAtUtc datetime2(7) NOT NULL,
          CONSTRAINT PK_EasyPayPaymentIdRetirementAudit PRIMARY KEY (PaymentId)
        );
      END;

      IF EXISTS (SELECT 1 FROM dbo.EasyPayPaymentIdRetirementAudit audit
                 JOIN #PaymentFacts member ON member.PaymentId = audit.PaymentId
                 JOIN #ApprovedGroups approved ON approved.GroupId = member.GroupId
                 WHERE approved.Action = N'RetireIdentifier')
        THROW 51003, 'A payment selected for retirement already has retirement audit history. History cannot be overwritten.', 1;

      UPDATE payment
      SET EasyPayPaymentId = NULL
      OUTPUT deleted.Id, member.GroupId, deleted.EasyPayPaymentId, @PlanEvidenceHash, @AppliedAtUtc
      INTO dbo.EasyPayPaymentIdRetirementAudit
        (PaymentId, GroupId, OriginalEasyPayPaymentId, PlanEvidenceHash, RetiredAtUtc)
      FROM dbo.Payments payment
      JOIN #PaymentFacts member ON member.PaymentId = payment.Id
      JOIN #ApprovedGroups approved ON approved.GroupId = member.GroupId
      WHERE approved.Action = N'RetireIdentifier';

      SELECT audit.PaymentId AS RetiredPaymentId, audit.GroupId, audit.OriginalEasyPayPaymentId, audit.RetiredAtUtc
      FROM dbo.EasyPayPaymentIdRetirementAudit audit
      JOIN #PaymentFacts member ON member.PaymentId = audit.PaymentId
      JOIN #ApprovedGroups approved ON approved.GroupId = member.GroupId
      WHERE approved.Action = N'RetireIdentifier'
      ORDER BY audit.GroupId, audit.PaymentId;
    END;

    IF EXISTS (SELECT 1 FROM #DeletePayments)
    BEGIN
      IF OBJECT_ID(N'dbo.EasyPayPaymentDeletionAudit', N'U') IS NULL
      BEGIN
        CREATE TABLE dbo.EasyPayPaymentDeletionAudit
        (
          PaymentId int NOT NULL,
          GroupId int NOT NULL,
          ReplacementPaymentId int NULL,
          OriginalEasyPayPaymentId nvarchar(max) NOT NULL,
          DeletedPaymentJson nvarchar(max) NOT NULL,
          PlanEvidenceHash varchar(64) NOT NULL,
          DeletedAtUtc datetime2(7) NOT NULL,
          CONSTRAINT PK_EasyPayPaymentDeletionAudit PRIMARY KEY (PaymentId)
        );
      END;

      IF EXISTS (SELECT 1 FROM dbo.EasyPayPaymentDeletionAudit audit JOIN #DeletePayments target ON target.PaymentId = audit.PaymentId)
        THROW 51003, 'A payment selected for deletion already has deletion audit history. History cannot be overwritten.', 1;

      INSERT dbo.EasyPayPaymentDeletionAudit
        (PaymentId, GroupId, ReplacementPaymentId, OriginalEasyPayPaymentId, DeletedPaymentJson, PlanEvidenceHash, DeletedAtUtc)
      SELECT payment.Id, target.GroupId, target.ReplacementPaymentId, payment.EasyPayPaymentId,
             (SELECT archived.* FROM dbo.Payments archived WHERE archived.Id = payment.Id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES),
             @PlanEvidenceHash, @AppliedAtUtc
      FROM dbo.Payments payment JOIN #DeletePayments target ON target.PaymentId = payment.Id;

      DELETE payment
      OUTPUT deleted.Id AS DeletedPaymentId
      FROM dbo.Payments payment JOIN #DeletePayments target ON target.PaymentId = payment.Id;
    END;
  END;

  IF EXISTS
  (
    SELECT 1
    FROM dbo.Payments
    WHERE EasyPayPaymentId IS NOT NULL
    GROUP BY EasyPayPaymentId
    HAVING COUNT(*) > 1
  )
    THROW 51004, 'Duplicate EasyPayPaymentId values remain. Reconciliation has been rolled back.', 1;

  IF @DryRun = 0
  BEGIN
    COMMIT TRANSACTION;
    PRINT 'Approved reconciliation actions committed. Only explicitly approved DeleteGroup rows can have been deleted; RetireIdentifier only cleared audited IDs.';
  END
  ELSE
  BEGIN
    ROLLBACK TRANSACTION;
    PRINT 'Dry run complete. All transactional changes were rolled back.';
  END;
END TRY
BEGIN CATCH
  IF XACT_STATE() <> 0
    ROLLBACK TRANSACTION;

  THROW;
END CATCH;
