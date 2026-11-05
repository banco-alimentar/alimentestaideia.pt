#requires -Version 7.0
[CmdletBinding()]
param([string] $ServerInstance = '(localdb)\MSSQLLocalDB')

$ErrorActionPreference = 'Stop'
$database = 'EasyPayPlanTests_' + [guid]::NewGuid().ToString('N')
$temporaryDirectory = Join-Path ([System.IO.Path]::GetTempPath()) $database
$runner = Join-Path $PSScriptRoot 'Invoke-EasyPayPaymentReconciliation.ps1'
$sql = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot '../BancoAlimentar.AlimentaEstaIdeia.Web/Migrations/Scripts/MergeDuplicateEasyPayPaymentIds.sql'))
$created = $false
$passed = 0
$settings = 'SET NOCOUNT ON; SET ANSI_NULLS ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON; SET ARITHABORT ON; SET CONCAT_NULL_YIELDS_NULL ON; SET NUMERIC_ROUNDABORT OFF; SET QUOTED_IDENTIFIER ON;'

function Invoke-TestSql([string] $Batch) {
    $output = ($settings + "`n" + $Batch) | & sqlcmd -S $ServerInstance -E -b -I -x -l 30 -t 60 -d $database -y 0 -w 65535
    if ($LASTEXITCODE -ne 0) { throw ($output -join "`n") }
    return $output
}

function Write-Pass([string] $Message) {
    $script:passed++
    Write-Output "PASS: $Message"
}

function Copy-Plan($Plan) {
    return ($Plan | ConvertTo-Json -Depth 50 -Compress | ConvertFrom-Json)
}

function Get-ReconcileSql($Plan, [switch] $Commit, [switch] $NullDryRun) {
    $json = ($Plan | ConvertTo-Json -Depth 50 -Compress).Replace("'", "''")
    $configured = $sql.Replace("DECLARE @Mode nvarchar(10) = N'Plan';", "DECLARE @Mode nvarchar(10) = N'Reconcile';")
    $configured = $configured.Replace('DECLARE @ApprovedPlan nvarchar(max) = NULL;', "DECLARE @ApprovedPlan nvarchar(max) = N'$json';")
    if ($Commit) { $configured = $configured.Replace('DECLARE @DryRun bit = 1;', 'DECLARE @DryRun bit = 0;') }
    if ($NullDryRun) { $configured = $configured.Replace('DECLARE @DryRun bit = 1;', 'DECLARE @DryRun bit = NULL;') }
    return $configured
}

function Assert-Rejected($Plan, [int] $ErrorNumber, [string] $Message, [string] $Before = '', [string] $After = '', [switch] $KeepAudit) {
    $configured = (Get-ReconcileSql $Plan -Commit).Replace("'", "''")
    $noAudit = if ($KeepAudit) { '' } else {
        "IF OBJECT_ID(N'dbo.EasyPayPaymentIdReconciliationAudit', N'U') IS NOT NULL OR OBJECT_ID(N'dbo.EasyPayPaymentIdReconciliationPlans', N'U') IS NOT NULL THROW 51102, 'Rejected plan retained audit schema.', 1;"
    }
    Invoke-TestSql @"
$Before
DECLARE @Caught bit = 0;
BEGIN TRY
    EXEC sys.sp_executesql N'$configured';
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() <> $ErrorNumber THROW;
    SET @Caught = 1;
END CATCH;
IF @Caught <> 1 THROW 51100, 'Expected plan rejection was not raised.', 1;
$After
EXEC dbo.AssertBaseline;
$noAudit
"@ | Out-Null
    Write-Pass $Message
}

function Get-ApprovedDeletionFixturePlan {
    $document = ((Invoke-TestSql $sql) -join '') | ConvertFrom-Json
    $document.ApprovedBy = 'Disposable deletion-test reviewer'
    $document.ApprovalReason = 'Fixture only: paid replacement covers failed attempts; preserve all other records.'
    $deletion = $document.Groups | Where-Object GroupId -eq 11
    $deletion.Action = 'DeleteGroup'
    $deletion.CanonicalPaymentId = $null
    $deletion.Reason = 'Failed attempts superseded by confirmed successful payment 13; archive complete rows.'
    return $document
}

try {
    [System.IO.Directory]::CreateDirectory($temporaryDirectory) | Out-Null
    & sqlcmd -S $ServerInstance -E -b -I -x -l 30 -d master -Q "CREATE DATABASE [$database];"
    if ($LASTEXITCODE -ne 0) { throw 'Cannot create disposable test database.' }
    $created = $true
    Invoke-TestSql @'
CREATE TABLE dbo.Donations (Id int NOT NULL PRIMARY KEY, ConfirmedPaymentId int NULL, DonationAmount decimal(18,2) NOT NULL, PaymentStatus int NOT NULL DEFAULT (1));
CREATE TABLE dbo.Payments (Id int NOT NULL PRIMARY KEY, EasyPayPaymentId nvarchar(max) NULL, DonationId int NULL REFERENCES dbo.Donations(Id), Created datetime2 NOT NULL, Completed datetime2 NULL, Discriminator nvarchar(50) NOT NULL, TransactionKey nvarchar(100) NULL, Requested real NULL, Paid real NULL, ExtraPayload nvarchar(max) NULL, Status nvarchar(max) NULL DEFAULT (N'paid'), PayPalPaymentId nvarchar(100) NULL, PayerId nvarchar(100) NULL);
ALTER TABLE dbo.Donations ADD CONSTRAINT FK_Confirmation FOREIGN KEY (ConfirmedPaymentId) REFERENCES dbo.Payments(Id);
CREATE UNIQUE INDEX IX_Confirmation ON dbo.Donations(ConfirmedPaymentId) WHERE ConfirmedPaymentId IS NOT NULL;
CREATE TABLE dbo.Invoices (Id int NOT NULL PRIMARY KEY, [Year] int NOT NULL, FoodBankId int NULL, [Sequence] int NOT NULL);
CREATE TABLE dbo.PaymentNotifications (Id int NOT NULL PRIMARY KEY, PaymentId int NOT NULL REFERENCES dbo.Payments(Id), NotificationType int NOT NULL, Subject nvarchar(100) NULL, UNIQUE (PaymentId, NotificationType));
CREATE TABLE dbo.PaymentReferences (Id int NOT NULL PRIMARY KEY, PaymentId int NOT NULL REFERENCES dbo.Payments(Id));
CREATE TABLE dbo.GuardWork (Id int NOT NULL PRIMARY KEY);
INSERT dbo.Donations (Id, ConfirmedPaymentId, DonationAmount) VALUES (10, NULL, 15), (20, NULL, 5), (30, NULL, 1), (40, NULL, 7), (41, NULL, 7), (50, NULL, 10);
INSERT dbo.Payments (Id, EasyPayPaymentId, DonationId, Created, Completed, Discriminator, TransactionKey, Requested, Paid, ExtraPayload) VALUES
 (1, N'matching', 10, '2026-01-01', '2026-01-02', N'CreditCardPayment', N'key-match', 15, 15, N'preserve'),
 (2, N'matching', NULL, '2026-01-03', '2026-01-04', N'CreditCardPayment', N'key-match', 15, 15, N'preserve'),
 (3, N'zero-attempts', 20, '2026-01-01', '2026-01-02', N'CreditCardPayment', N'key-zero', 5, 5, N'preserve'),
 (4, N'zero-attempts', NULL, '2026-01-01', NULL, N'CreditCardPayment', N'key-zero', 0, 0, N'preserve'),
 (5, N'policy-review', 30, '2026-01-01', NULL, N'CreditCardPayment', N'key-first', 1, 0, N'preserve'),
 (6, N'policy-review', 30, '2026-01-02', NULL, N'CreditCardPayment', N'key-second', 2, 0, N'preserve'),
 (7, N'ambiguous', NULL, '2026-01-01', '2026-01-02', N'CreditCardPayment', N'key-ambiguous', 7, 7, N'preserve'),
 (8, N'ambiguous', NULL, '2026-01-01', '2026-01-02', N'CreditCardPayment', N'key-ambiguous', 7, 7, N'preserve'),
 (9, N'unrelated', 50, '2026-01-01', '2026-01-02', N'CreditCardPayment', N'key-unrelated', 10, 10, N'preserve'),
 (10, N'other-unrelated', 50, '2026-01-01', NULL, N'CreditCardPayment', N'other-key', 0, 0, N'preserve');
UPDATE dbo.Payments SET Status = N'pending' WHERE Id = 4;
UPDATE dbo.Donations SET ConfirmedPaymentId = CASE Id WHEN 10 THEN 1 WHEN 20 THEN 3 WHEN 40 THEN 7 WHEN 41 THEN 8 WHEN 50 THEN 9 END;
INSERT dbo.Invoices VALUES (1, 2026, 1, 1);
INSERT dbo.PaymentNotifications VALUES (1, 1, 1, N'Keep confirmed'), (2, 2, 1, N'Keep historical'), (3, 4, 1, N'Keep unfinished');
INSERT dbo.PaymentReferences VALUES (1, 2), (2, 4), (3, 5), (4, 8);
SELECT * INTO dbo.ExpectedPayments FROM dbo.Payments;
SELECT * INTO dbo.ExpectedDonations FROM dbo.Donations;
SELECT * INTO dbo.ExpectedNotifications FROM dbo.PaymentNotifications;
SELECT * INTO dbo.ExpectedReferences FROM dbo.PaymentReferences;
EXEC(N'CREATE PROCEDURE dbo.AssertBaseline AS BEGIN
 IF EXISTS (SELECT * FROM dbo.Payments EXCEPT SELECT * FROM dbo.ExpectedPayments) OR EXISTS (SELECT * FROM dbo.ExpectedPayments EXCEPT SELECT * FROM dbo.Payments) THROW 51110, ''Unexpected payment change.'', 1;
 IF EXISTS (SELECT * FROM dbo.Donations EXCEPT SELECT * FROM dbo.ExpectedDonations) OR EXISTS (SELECT * FROM dbo.ExpectedDonations EXCEPT SELECT * FROM dbo.Donations) THROW 51111, ''Unexpected donation change.'', 1;
 IF EXISTS (SELECT * FROM dbo.PaymentNotifications EXCEPT SELECT * FROM dbo.ExpectedNotifications) OR EXISTS (SELECT * FROM dbo.ExpectedNotifications EXCEPT SELECT * FROM dbo.PaymentNotifications) THROW 51112, ''Notification history changed.'', 1;
 IF EXISTS (SELECT * FROM dbo.PaymentReferences EXCEPT SELECT * FROM dbo.ExpectedReferences) OR EXISTS (SELECT * FROM dbo.ExpectedReferences EXCEPT SELECT * FROM dbo.PaymentReferences) THROW 51113, ''Foreign keys changed.'', 1;
 IF @@TRANCOUNT <> 0 THROW 51114, ''Open transaction left behind.'', 1;
END;');
'@ | Out-Null

    $planPath = Join-Path $temporaryDirectory 'review.plan.json'
    & $runner -Mode Plan -ServerInstance $ServerInstance -Database $database -PlanPath $planPath | Out-Null
    $generated = Get-Content -Raw $planPath | ConvertFrom-Json
    if ($generated.Groups.Count -ne 4 -or @($generated.Groups | Where-Object { $null -ne $_.CanonicalPaymentId }).Count -ne 2) { throw 'Incorrect automatic/manual classification.' }
    $matching = $generated.Groups | Where-Object GroupId -eq 1
    if ($matching.DecisionRule -ne 'ConfirmedPaid' -or $matching.CanonicalPaymentId -ne 1 -or $matching.CurrentRankPaymentId -ne 2 -or $matching.Action -ne 'ReconcileIdentifiers') { throw 'Confirmed owner did not override date ranking.' }
    if ($null -ne $generated.ApprovedBy -or $null -ne $generated.ApprovalReason) { throw 'Generated plan approves itself.' }
    Write-Pass 'Plan export classifies matching, zero-value, policy-review, and ownership-review groups without self-approval.'
    $unconfirmedPaid = $generated.Groups | Where-Object GroupId -eq 5
    $multiConfirmed = $generated.Groups | Where-Object GroupId -eq 7
    if (($generated.Groups | Where-Object GroupId -eq 3).DecisionRule -ne 'ConfirmedPaid' -or $unconfirmedPaid.DecisionRule -ne 'ReviewUnconfirmedPaidDonation' -or $null -ne $unconfirmedPaid.Action -or $multiConfirmed.DecisionRule -ne 'MultiConfirmed' -or $null -ne $multiConfirmed.Action -or $multiConfirmed.SuggestedAction -ne 'ResolveDuplicateDonations') { throw 'Version-5 decision rules or prefilled actions are incorrect.' }
    Write-Pass 'Confirmed paid owners are prefilled; multi-confirmed and unconfirmed paid-donation groups are left without an Action.'

    if ($generated.PlanVersion -ne 5) { throw 'Confirmed-payment decision plans must use version 5.' }
    $orphan = ($generated.Groups | Where-Object GroupId -eq 1).Members | Where-Object PaymentId -eq 2
    $linked = ($generated.Groups | Where-Object GroupId -eq 1).Members | Where-Object PaymentId -eq 1
    if (-not $orphan.DeletionCandidate -or $orphan.DeletionCandidateReason -ne 'MissingDonationId' -or -not $orphan.DeletionBlockedByFinancialEvidence -or $linked.DeletionCandidate -or -not $matching.HasDeletionCandidates -or $matching.DeletionCandidateCount -ne 1 -or $matching.DeletionCandidate) { throw 'Null DonationId was not flagged independently in a mixed group.' }
    Write-Pass 'Every null DonationId is marked individually without marking a linked sibling or bypassing paid/completed protection.'
    if (@($orphan.Dependencies | Where-Object ReferencingTable -eq 'dbo.PaymentNotifications').Count -ne 1 -or @($orphan.Dependencies | Where-Object ReferencingTable -eq 'dbo.PaymentReferences').Count -ne 1) { throw 'Mixed-group deletion candidates did not expose their dependent records.' }
    Write-Pass 'Dependencies are checked for individual candidates even when the whole group is not a deletion candidate.'
    $inverseConfirmed = ($generated.Groups | Where-Object GroupId -eq 7).Members | Where-Object PaymentId -eq 7
    if (-not $inverseConfirmed.DeletionCandidate -or -not $inverseConfirmed.DeletionBlockedByFinancialEvidence -or $inverseConfirmed.ConfirmedDonationId -ne 40 -or @($inverseConfirmed.Dependencies | Where-Object ReferencingTable -eq 'dbo.Donations').Count -ne 1) { throw 'Inverse confirmation or dependency evidence was lost for an orphaned candidate.' }
    Write-Pass 'A null forward link still exposes inverse donation confirmations and blocks unsafe deletion.'
    foreach ($status in @('paid', 'Success', ' ok ', 'COMPLETED', 'Approved')) {
        Invoke-TestSql "UPDATE dbo.Payments SET Status = N'$status' WHERE Id = 1; UPDATE dbo.Donations SET PaymentStatus = 2 WHERE Id = 10;" | Out-Null
        $statusPlan = ((Invoke-TestSql $sql) -join '') | ConvertFrom-Json
        $statusMember = ($statusPlan.Groups | Where-Object GroupId -eq 1).Members | Where-Object PaymentId -eq 1
        if ($statusMember.IsSuccessfulStatus -ne $true -or $statusMember.StatusSource -ne 'Payment') { throw "Successful provider status was not recognized: $status" }
    }
    Write-Pass 'paid/Success/ok/COMPLETED/approved recognize case/space variants and override donation fallback.'
    Invoke-TestSql 'UPDATE dbo.Payments SET Status = NULL WHERE Id IN (1, 2); UPDATE dbo.Payments SET DonationId = 10 WHERE Id = 2; UPDATE dbo.Donations SET PaymentStatus = 1 WHERE Id = 10;' | Out-Null
    $statusPlan = ((Invoke-TestSql $sql) -join '') | ConvertFrom-Json
    $statusGroup = $statusPlan.Groups | Where-Object GroupId -eq 1
    if ($statusGroup.SuccessfulStatusCount -ne 2 -or $statusGroup.Members[0].StatusSource -ne 'Donation' -or $statusGroup.Members[0].DonationPaymentStatusName -ne 'Payed') { throw 'Null statuses did not fall back to paid donation status.' }
    Write-Pass 'NULL provider statuses use forward-linked Donations.PaymentStatus and export the enum name.'
    foreach ($donationStatus in @(2, 3, 4)) {
        Invoke-TestSql "UPDATE dbo.Donations SET PaymentStatus = $donationStatus WHERE Id = 10;" | Out-Null
        $statusPlan = ((Invoke-TestSql $sql) -join '') | ConvertFrom-Json
        $statusGroup = $statusPlan.Groups | Where-Object GroupId -eq 1
        if (-not $statusGroup.DeletionCandidate -or -not $statusGroup.DeletionBlockedByFinancialEvidence -or $statusGroup.DecisionRule -ne 'ReviewConfirmedOwner' -or $null -ne $statusGroup.CanonicalPaymentId -or $null -ne $statusGroup.Action) { throw 'Unpaid/waiting/error groups were not marked for guarded deletion review.' }
    }
    Write-Pass 'NotPayed/WaitingPayment/ErrorPayment confirmations require owner review, never automatic ownership or deletion.'
    Invoke-TestSql 'UPDATE dbo.Payments SET DonationId = NULL WHERE Id = 1; UPDATE dbo.Donations SET PaymentStatus = 1 WHERE Id = 10;' | Out-Null
    $statusPlan = ((Invoke-TestSql $sql) -join '') | ConvertFrom-Json
    $statusMember = ($statusPlan.Groups | Where-Object GroupId -eq 1).Members | Where-Object PaymentId -eq 1
    if ($statusMember.IsSuccessfulStatus -ne $true -or $statusMember.StatusSource -ne 'ConfirmedDonation' -or $statusMember.StatusDonationId -ne 10) { throw 'Inverse confirmation fallback failed.' }
    Write-Pass 'An orphaned payment can fall back through its unique confirming donation.'
    Invoke-TestSql 'UPDATE dbo.Payments SET DonationId = 10 WHERE Id = 1; UPDATE dbo.Payments SET DonationId = NULL WHERE Id = 2;' | Out-Null
    $statusPlan = ((Invoke-TestSql $sql) -join '') | ConvertFrom-Json
    $statusGroup = $statusPlan.Groups | Where-Object GroupId -eq 1
    if ($statusGroup.UnknownStatusCount -ne 1 -or $statusGroup.DeletionCandidate -or $statusGroup.DecisionRule -ne 'ConfirmedPaid' -or $statusGroup.CanonicalPaymentId -ne 1) { throw 'An unknown sibling blocked or displaced the confirmed paid owner.' }
    $unknownMember = $statusGroup.Members | Where-Object PaymentId -eq 2
    if (-not $unknownMember.DeletionCandidate -or -not $unknownMember.DeletionBlockedByUnknownStatus -or $unknownMember.DeletionCandidateReason -ne 'MissingDonationId') { throw 'An unknown orphan was not marked with a separate status blocker.' }
    Write-Pass 'An unknown orphan is a relationship-review candidate, not an inferred failure, and does not block the confirmed paid owner.'
    Invoke-TestSql "UPDATE dbo.Payments SET Status = N'failed' WHERE Id = 1; UPDATE dbo.Payments SET Status = N'paid' WHERE Id = 2;" | Out-Null
    $statusPlan = ((Invoke-TestSql $sql) -join '') | ConvertFrom-Json
    $statusGroup = $statusPlan.Groups | Where-Object GroupId -eq 1
    if ($statusGroup.DecisionRule -ne 'ReviewConfirmedOwner' -or $statusGroup.SuggestedCanonicalPaymentId -ne 2 -or $null -ne $statusGroup.CanonicalPaymentId -or -not $statusGroup.DeletionCandidate -or -not $statusGroup.DeletionBlockedByFinancialEvidence) { throw 'Mixed status/relationship candidates lost their financial warning or were approved automatically.' }
    Write-Pass 'Mixed status/relationship candidates retain successful-owner suggestions, financial blockers, and explicit review.'
    Invoke-TestSql "UPDATE dbo.Payments SET Status = N'paid' WHERE Id IN (1, 2); UPDATE dbo.Donations SET PaymentStatus = 1 WHERE Id = 10;" | Out-Null
    Invoke-TestSql 'UPDATE dbo.Donations SET PaymentStatus = 2 WHERE Id = 30;' | Out-Null
    foreach ($status in @('declined', 'pending', '', 'NULL')) {
        Invoke-TestSql "UPDATE dbo.Payments SET Status = N'$status' WHERE Id IN (5, 6);" | Out-Null
        $statusPlan = ((Invoke-TestSql $sql) -join '') | ConvertFrom-Json
        $statusGroup = $statusPlan.Groups | Where-Object GroupId -eq 5
        if (-not $statusGroup.DeletionCandidate -or $statusGroup.DeletionBlockedByFinancialEvidence -or $statusGroup.SuccessfulStatusCount -ne 0 -or $statusGroup.NonSuccessfulStatusCount -ne 2 -or $null -ne $statusGroup.CanonicalPaymentId -or $statusGroup.DecisionRule -ne 'NoOwnerRetire' -or $statusGroup.Action -ne 'RetireIdentifier' -or @($statusGroup.Members | Where-Object { -not $_.DeletionCandidate -or $_.StatusSource -ne 'Payment' }).Count -ne 0) { throw 'Explicit non-success statuses were overridden or deletion-review flags were incorrect.' }
    }
    Write-Pass 'Entirely non-successful unpaid groups are prefilled for retirement, including blank/literal-NULL statuses, without donation fallback or deletion.'
    Invoke-TestSql "UPDATE dbo.Payments SET Status = N'failed' WHERE Id IN (5, 6); UPDATE dbo.Donations SET PaymentStatus = 1 WHERE Id = 30;" | Out-Null
    $statusPlan = ((Invoke-TestSql $sql) -join '') | ConvertFrom-Json
    $statusGroup = $statusPlan.Groups | Where-Object GroupId -eq 5
    if (-not $statusGroup.DeletionCandidate -or -not $statusGroup.DeletionBlockedByFinancialEvidence -or $statusGroup.NonSuccessfulStatusCount -ne 2) { throw 'Paid-donation evidence was ignored when flagging failed provider rows.' }
    Write-Pass 'A paid donation protects failed provider rows as conflicting financial evidence without overriding provider-status precedence.'
    Invoke-TestSql "UPDATE dbo.Payments SET Status = N'paid' WHERE Id IN (5, 6); UPDATE dbo.Payments SET Status = NULL WHERE Id = 1; UPDATE dbo.Donations SET PaymentStatus = 99 WHERE Id = 10;" | Out-Null
    $statusPlan = ((Invoke-TestSql $sql) -join '') | ConvertFrom-Json
    $statusGroup = $statusPlan.Groups | Where-Object GroupId -eq 1
    if ($statusGroup.UnknownStatusCount -ne 1 -or $statusGroup.DeletionCandidate -or $null -ne $statusGroup.Members[0].IsSuccessfulStatus) { throw 'An unknown donation enum value was treated as successful or failed.' }
    Write-Pass 'Unrecognized donation enum values remain unknown and cannot authorize deletion review.'
    Invoke-TestSql "UPDATE dbo.Payments SET Status = N'paid', Completed = NULL WHERE Id = 1; UPDATE dbo.Donations SET PaymentStatus = 1 WHERE Id = 10;" | Out-Null
    $statusPlan = ((Invoke-TestSql $sql) -join '') | ConvertFrom-Json
    $statusGroup = $statusPlan.Groups | Where-Object GroupId -eq 1
    if ($statusGroup.CanonicalPaymentId -ne 1 -or $statusGroup.DecisionRule -ne 'ConfirmedPaid') { throw 'Successful status evidence was incorrectly discarded because Completed was missing.' }
    Write-Pass 'Successful status can support a confirmed matching candidate even when its completion timestamp is missing.'
    Invoke-TestSql "UPDATE dbo.Payments SET Completed = '2026-01-02' WHERE Id = 1; UPDATE dbo.Payments SET Status = N'paid' WHERE Id = 4;" | Out-Null
    $statusPlan = ((Invoke-TestSql $sql) -join '') | ConvertFrom-Json
    $statusGroup = $statusPlan.Groups | Where-Object GroupId -eq 3
    if ($statusGroup.CanonicalPaymentId -ne 3 -or $statusGroup.DecisionRule -ne 'ConfirmedPaid' -or $statusGroup.DeletionCandidate) { throw 'A zero-value sibling marked successful displaced the confirmed paid owner.' }
    Write-Pass 'A zero-value sibling marked successful does not displace the confirmed paid owner.'
    Invoke-TestSql "UPDATE dbo.Payments SET Status = N'pending' WHERE Id = 4;" | Out-Null
    Invoke-TestSql "UPDATE dbo.Payments SET TransactionKey = N'different-key', Discriminator = N'MBWayPayment' WHERE Id = 2;" | Out-Null
    $statusPlan = ((Invoke-TestSql $sql) -join '') | ConvertFrom-Json
    $statusGroup = $statusPlan.Groups | Where-Object GroupId -eq 1
    Invoke-TestSql "UPDATE dbo.Payments SET TransactionKey = N'key-match', Discriminator = N'CreditCardPayment' WHERE Id = 2;" | Out-Null
    if ($statusGroup.DecisionRule -ne 'ConfirmedPaid' -or $statusGroup.CanonicalPaymentId -ne 1 -or $statusGroup.SameTransactionKey -ne 0) { throw 'Sibling key/type differences blocked a proven confirmed paid owner.' }
    Write-Pass 'Sibling transaction-key and type differences do not block a proven confirmed paid owner.'
    Invoke-TestSql 'UPDATE dbo.Payments SET DonationId = 30 WHERE Id = 2;' | Out-Null
    $statusPlan = ((Invoke-TestSql $sql) -join '') | ConvertFrom-Json
    $statusGroup = $statusPlan.Groups | Where-Object GroupId -eq 1
    Invoke-TestSql 'UPDATE dbo.Payments SET DonationId = NULL WHERE Id = 2;' | Out-Null
    if ($statusGroup.DecisionRule -ne 'ReviewUnconfirmedPaidDonation' -or $statusGroup.UnconfirmedPaidDonationLinkCount -ne 1 -or $null -ne $statusGroup.CanonicalPaymentId -or $null -ne $statusGroup.Action) { throw 'A possible payer of an unconfirmed Payed donation was cleared automatically.' }
    Write-Pass 'A sibling linked to a Payed donation without ConfirmedPaymentId blocks automatic ownership.'
    Invoke-TestSql 'EXEC dbo.AssertBaseline;' | Out-Null

    foreach ($amount in @('0.70', '0.65', '5.40', '9.90', '10.05')) {
        Invoke-TestSql "UPDATE dbo.Payments SET Requested = $amount, Paid = $amount WHERE Id IN (1, 2); UPDATE dbo.Donations SET DonationAmount = $amount WHERE Id = 10;" | Out-Null
        $precisionJson = (Invoke-TestSql $sql) -join ''
        $precisionDocument = [System.Text.Json.JsonDocument]::Parse($precisionJson)
        try {
            $precisionGroup = @($precisionDocument.RootElement.GetProperty('Groups').EnumerateArray() | Where-Object { $_.GetProperty('GroupId').GetInt32() -eq 1 })[0]
            foreach ($member in $precisionGroup.GetProperty('Members').EnumerateArray()) {
                foreach ($field in @('Requested', 'Paid')) {
                    $value = $member.GetProperty($field)
                    if ($value.ValueKind -ne [System.Text.Json.JsonValueKind]::Number -or $value.GetRawText() -notmatch '^\d+\.\d{4}$' -or $value.GetDecimal() -ne [decimal]::Parse($amount, [cultureinfo]::InvariantCulture)) {
                        throw "Currency serialization lost precision or readability for $amount/$field."
                    }
                }
            }
        }
        finally { $precisionDocument.Dispose() }
        Write-Pass "Requested/Paid $amount serialize as exact readable four-decimal JSON numbers."
    }
    Invoke-TestSql "UPDATE dbo.Payments SET Requested = 0.7004, Paid = 0.7004 WHERE Id IN (1, 2); UPDATE dbo.Donations SET DonationAmount = 0.70 WHERE Id = 10;" | Out-Null
    $precisionPlan = ((Invoke-TestSql $sql) -join '') | ConvertFrom-Json
    $precisionGroup = $precisionPlan.Groups | Where-Object GroupId -eq 1
    if ($precisionGroup.Members[0].Paid -ne [decimal]'0.7004' -or $null -ne $precisionGroup.CanonicalPaymentId) { throw 'Sub-cent amounts were rounded away or incorrectly approved.' }
    Write-Pass 'Sub-cent values remain visible and do not become false confirmation matches.'

    Invoke-TestSql 'UPDATE dbo.Payments SET Requested = 0.7, Paid = 0.7 WHERE Id = 1; UPDATE dbo.Payments SET Requested = 0.7000001, Paid = 0.7000001 WHERE Id = 2;' | Out-Null
    $precisionPlan = ((Invoke-TestSql $sql) -join '') | ConvertFrom-Json
    $precisionGroup = $precisionPlan.Groups | Where-Object GroupId -eq 1
    if ($precisionGroup.Members[0].Paid -ne $precisionGroup.Members[1].Paid -or $precisionGroup.SameAmounts -ne 0 -or $precisionGroup.CanonicalPaymentId -ne 1) { throw 'Display rounding concealed a stored amount mismatch, or a sibling mismatch blocked the confirmed owner.' }
    Write-Pass 'Raw comparisons detect distinct real values even when the displayed decimals are identical, without blocking the confirmed owner.'

    Invoke-TestSql 'UPDATE dbo.Payments SET Requested = NULL, Paid = NULL WHERE Id = 1;' | Out-Null
    $precisionPlan = ((Invoke-TestSql $sql) -join '') | ConvertFrom-Json
    $precisionMember = ($precisionPlan.Groups | Where-Object GroupId -eq 1).Members | Where-Object PaymentId -eq 1
    $zeroMember = ($precisionPlan.Groups | Where-Object GroupId -eq 3).Members | Where-Object PaymentId -eq 4
    if ($null -ne $precisionMember.Requested -or $null -ne $precisionMember.Paid -or $zeroMember.Requested -ne 0 -or $zeroMember.Paid -ne 0) { throw 'Currency serialization confused null and zero.' }
    Write-Pass 'Amount serialization preserves null and zero as distinct values.'
    Invoke-TestSql 'UPDATE dbo.Payments SET Requested = 15, Paid = 15 WHERE Id IN (1, 2); UPDATE dbo.Donations SET DonationAmount = 15 WHERE Id = 10;' | Out-Null
    $restored = ((Invoke-TestSql $sql) -join '') | ConvertFrom-Json
    if ($restored.PlanEvidenceHash -ne $generated.PlanEvidenceHash) { throw 'Amount presentation changed the raw evidence fingerprint.' }
    Write-Pass 'Decimal presentation leaves raw database evidence hashes unchanged.'

    $planAtCommitFlag = $sql.Replace('DECLARE @DryRun bit = 1;', 'DECLARE @DryRun bit = 0;')
    Invoke-TestSql $planAtCommitFlag | Out-Null
    Invoke-TestSql "EXEC dbo.AssertBaseline; IF OBJECT_ID(N'dbo.EasyPayPaymentIdReconciliationAudit', N'U') IS NOT NULL THROW 51115, 'Plan mode wrote schema.', 1;" | Out-Null
    Write-Pass 'Plan mode remains read-only even when DryRun is zero.'

    $approved = Copy-Plan $generated
    $approved.ApprovedBy = "Fixture reviewer O'Brien"
    $approved.ApprovalReason = 'Disposable fixture only; literal $(UNDEFINED) must not be expanded.'
    Assert-Rejected $generated 51006 'Missing reviewer approval is rejected.'
    Assert-Rejected $approved 51010 'Flagged exceptions remain unapproved until explicit owner decisions are supplied.'
    foreach ($group in $approved.Groups) {
        if ($group.GroupId -eq 5) { $group.Action = 'ReconcileIdentifiers'; $group.CanonicalPaymentId = 6; $group.Reason = 'Explicit policy decision for disposable fixture.' }
        if ($group.GroupId -eq 7) { $group.Action = 'ReconcileIdentifiers'; $group.CanonicalPaymentId = 7; $group.Reason = 'Explicit owner decision for disposable fixture only.' }
    }

    $bad = Copy-Plan $approved
    $bad.PlanVersion = 1
    Assert-Rejected $bad 51006 'Version-1 plans cannot bypass the new status-aware review rules.'
    $bad = Copy-Plan $approved
    $bad.PlanVersion = 'not-a-version'
    Assert-Rejected $bad 51006 'Malformed plan versions are rejected.'
    $bad = Copy-Plan $approved
    $bad.DatabaseName = 'another-database'
    Assert-Rejected $bad 51007 'Plans cannot target another database.'
    $bad = Copy-Plan $approved
    $bad.ServerName = 'another-server'
    Assert-Rejected $bad 51007 'Plans cannot target another server.'
    $bad = Copy-Plan $approved
    $bad.Groups = @($bad.Groups | Where-Object GroupId -ne 1)
    Assert-Rejected $bad 51007 'Missing approval groups cannot produce partial reconciliation.'
    $bad = Copy-Plan $approved
    $bad.Groups = @($bad.Groups) + @($bad.Groups[0])
    Assert-Rejected $bad 51007 'Duplicate approval groups are rejected.'
    $bad = Copy-Plan $approved
    $extra = Copy-Plan $bad.Groups[0]
    $extra.GroupId = 999
    $bad.Groups = @($bad.Groups) + @($extra)
    Assert-Rejected $bad 51007 'Unexpected approval groups are rejected.'
    $bad = Copy-Plan $approved
    $bad.Groups[0].ExpectedEvidenceHash = '0' * 64
    Assert-Rejected $bad 51007 'A changed per-group evidence hash is rejected.'
    $bad = Copy-Plan $approved
    $bad.Groups[0].CanonicalPaymentId = 9
    Assert-Rejected $bad 51008 'Selected owners must belong to their approved group.'
    $bad = Copy-Plan $approved
    $bad.Groups[0].Reason = ' '
    Assert-Rejected $bad 51002 'Every ownership decision requires a reason.'

    Assert-Rejected $approved 51007 'Any changed payment column invalidates the plan.' "UPDATE dbo.Payments SET ExtraPayload = N'changed' WHERE Id = 2;" "UPDATE dbo.Payments SET ExtraPayload = N'preserve' WHERE Id = 2;"
    Assert-Rejected $approved 51007 'Changed donation evidence invalidates the plan.' 'UPDATE dbo.Donations SET DonationAmount = 16 WHERE Id = 10;' 'UPDATE dbo.Donations SET DonationAmount = 15 WHERE Id = 10;'
    Assert-Rejected $approved 51007 'Changed provider status invalidates an approved plan.' "UPDATE dbo.Payments SET Status = N'failed' WHERE Id = 2;" "UPDATE dbo.Payments SET Status = N'paid' WHERE Id = 2;"
    Assert-Rejected $approved 51007 'Changed donation payment status invalidates an approved plan.' 'UPDATE dbo.Donations SET PaymentStatus = 2 WHERE Id = 10;' 'UPDATE dbo.Donations SET PaymentStatus = 1 WHERE Id = 10;'
    Assert-Rejected $approved 51007 'Changed group membership invalidates the plan.' "UPDATE dbo.Payments SET EasyPayPaymentId = N'matching' WHERE Id = 10;" "UPDATE dbo.Payments SET EasyPayPaymentId = N'other-unrelated' WHERE Id = 10;"
    Assert-Rejected $approved 51007 'A new duplicate group invalidates the complete plan.' "UPDATE dbo.Payments SET EasyPayPaymentId = N'unrelated' WHERE Id = 10;" "UPDATE dbo.Payments SET EasyPayPaymentId = N'other-unrelated' WHERE Id = 10;"
    Assert-Rejected $approved 51009 'Overlength IDs outside duplicate groups block migration readiness.' "UPDATE dbo.Payments SET EasyPayPaymentId = REPLICATE(N'x', 129) WHERE Id = 9;" "UPDATE dbo.Payments SET EasyPayPaymentId = N'unrelated' WHERE Id = 9;"
    Assert-Rejected $approved 51009 'Conflicting invoice sequences block migration readiness.' 'INSERT dbo.Invoices VALUES (2, 2026, 1, 1);' 'DELETE dbo.Invoices WHERE Id = 2;'

    $escapedScript = $sql.Replace("'", "''")
    Invoke-TestSql @"
SET XACT_ABORT ON;
BEGIN TRANSACTION;
INSERT dbo.GuardWork VALUES (1);
EXEC sys.sp_executesql N'$escapedScript';
IF @@TRANCOUNT <> 1 OR XACT_STATE() <> 1 OR NOT EXISTS (SELECT 1 FROM dbo.GuardWork WHERE Id = 1) THROW 51116, 'Caller transaction was harmed.', 1;
ROLLBACK TRANSACTION;
EXEC dbo.AssertBaseline;
"@ | Out-Null
    Write-Pass 'Existing caller transactions remain intact and committable.'

    Assert-Rejected $approved 51090 'Update failures roll back all ID changes and audit-schema creation, and rethrow the original error.' "EXEC(N'CREATE TRIGGER dbo.FailUpdate ON dbo.Payments AFTER UPDATE AS BEGIN ;THROW 51090, ''Injected fixture failure.'', 1; END;');" 'DROP TRIGGER dbo.FailUpdate;'
    Assert-Rejected $approved 51003 'Existing per-payment audit history is never overwritten.' "CREATE TABLE dbo.EasyPayPaymentIdReconciliationAudit (PaymentId int NOT NULL PRIMARY KEY, CanonicalPaymentId int NOT NULL, OriginalEasyPayPaymentId nvarchar(max) NOT NULL, ReconciledAtUtc datetime2(7) NOT NULL); INSERT dbo.EasyPayPaymentIdReconciliationAudit VALUES (2, 1, N'matching', SYSUTCDATETIME());" 'DROP TABLE dbo.EasyPayPaymentIdReconciliationAudit;'

    $approvedPath = Join-Path $temporaryDirectory 'approved.plan.json'
    [System.IO.File]::WriteAllText($approvedPath, ($approved | ConvertTo-Json -Depth 50))
    & $runner -Mode DryRun -ServerInstance $ServerInstance -Database $database -PlanPath $approvedPath | Out-Null
    Invoke-TestSql "EXEC dbo.AssertBaseline; IF OBJECT_ID(N'dbo.EasyPayPaymentIdReconciliationAudit', N'U') IS NOT NULL OR OBJECT_ID(N'dbo.EasyPayPaymentIdReconciliationPlans', N'U') IS NOT NULL THROW 51117, 'Dry run retained audit tables.', 1;" | Out-Null
    Write-Pass 'Runner dry run preserves every row/field/link and rolls back both audit tables.'
    Invoke-TestSql (Get-ReconcileSql $approved -NullDryRun) | Out-Null
    Invoke-TestSql 'EXEC dbo.AssertBaseline;' | Out-Null
    Write-Pass 'A NULL DryRun flag still rolls back.'
    & $runner -Mode Apply -ServerInstance $ServerInstance -Database $database -PlanPath $approvedPath -WhatIf | Out-Null
    Invoke-TestSql 'EXEC dbo.AssertBaseline;' | Out-Null
    Write-Pass 'Apply WhatIf does not change the database.'

    & $runner -Mode Apply -ServerInstance $ServerInstance -Database $database -PlanPath $approvedPath -Confirm:$false | Out-Null
    Invoke-TestSql @'
UPDATE dbo.ExpectedPayments SET EasyPayPaymentId = NULL WHERE Id IN (2, 4, 5, 8);
EXEC dbo.AssertBaseline;
IF (SELECT COUNT(*) FROM dbo.EasyPayPaymentIdReconciliationAudit) <> 4 THROW 51118, 'Incorrect number of retained original IDs.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.EasyPayPaymentIdReconciliationAudit WHERE PaymentId = 2 AND CanonicalPaymentId = 1 AND OriginalEasyPayPaymentId = N'matching')
 OR NOT EXISTS (SELECT 1 FROM dbo.EasyPayPaymentIdReconciliationAudit WHERE PaymentId = 4 AND CanonicalPaymentId = 3 AND OriginalEasyPayPaymentId = N'zero-attempts')
 OR NOT EXISTS (SELECT 1 FROM dbo.EasyPayPaymentIdReconciliationAudit WHERE PaymentId = 5 AND CanonicalPaymentId = 6 AND OriginalEasyPayPaymentId = N'policy-review')
 OR NOT EXISTS (SELECT 1 FROM dbo.EasyPayPaymentIdReconciliationAudit WHERE PaymentId = 8 AND CanonicalPaymentId = 7 AND OriginalEasyPayPaymentId = N'ambiguous') THROW 51119, 'Incorrect audit mapping.', 1;
IF (SELECT COUNT(*) FROM dbo.EasyPayPaymentIdReconciliationPlans) <> 1 THROW 51120, 'Missing approved-plan audit.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.EasyPayPaymentIdReconciliationPlans WHERE ApprovedBy = N'Fixture reviewer O''Brien' AND ApprovalReason LIKE N'%$(UNDEFINED)%' AND ISJSON(ApprovedPlan) = 1 AND ExecutedBy IS NOT NULL) THROW 51121, 'Approval evidence was not preserved.', 1;
'@ | Out-Null
    Write-Pass 'Explicit apply commits only reviewed ID clears, retains all original IDs, and saves reviewer/reasons/full plan/SQL login.'
    Assert-Rejected $approved 51007 'Replaying a consumed plan fails closed without changing existing audit history.' -KeepAudit

    $emptyPath = Join-Path $temporaryDirectory 'empty.plan.json'
    & $runner -Mode Plan -ServerInstance $ServerInstance -Database $database -PlanPath $emptyPath | Out-Null
    $empty = Get-Content -Raw $emptyPath | ConvertFrom-Json
    if ($empty.Groups.Count -ne 0) { throw 'The post-reconciliation plan should be empty.' }
    $empty.ApprovedBy = 'Fixture reviewer'
    $empty.ApprovalReason = 'Verify no-op after reconciliation'
    [System.IO.File]::WriteAllText($emptyPath, ($empty | ConvertTo-Json -Depth 50))
    Invoke-TestSql 'ALTER TABLE dbo.Payments ALTER COLUMN EasyPayPaymentId nvarchar(128) NULL; CREATE UNIQUE INDEX IX_Payments_EasyPayPaymentId ON dbo.Payments(EasyPayPaymentId) WHERE EasyPayPaymentId IS NOT NULL;' | Out-Null
    & $runner -Mode Apply -ServerInstance $ServerInstance -Database $database -PlanPath $emptyPath -Confirm:$false | Out-Null
    Invoke-TestSql "EXEC dbo.AssertBaseline; IF (SELECT COUNT(*) FROM dbo.EasyPayPaymentIdReconciliationAudit) <> 4 OR (SELECT COUNT(*) FROM dbo.EasyPayPaymentIdReconciliationPlans) <> 1 THROW 51122, 'Empty plan changed audit history.', 1;" | Out-Null
    Write-Pass 'Column narrowing and filtered unique index succeed; a fresh empty approved plan is a no-op.'

    Invoke-TestSql @'
DROP INDEX IX_Payments_EasyPayPaymentId ON dbo.Payments;
INSERT dbo.Donations (Id, ConfirmedPaymentId, DonationAmount) VALUES (60, NULL, 12.34), (61, NULL, 5);
INSERT dbo.Payments (Id, EasyPayPaymentId, DonationId, Created, Completed, Discriminator, TransactionKey, Requested, Paid, ExtraPayload, Status) VALUES
 (11, N'delete-group', 60, '2026-02-01', NULL, N'MBWayPayment', N'failed-first', 12.34, 0, N'archive original first row', N'Failed'),
 (12, N'delete-group', 60, '2026-02-02', NULL, N'MBWayPayment', N'failed-second', 12.34, 0, N'archive original second row', N'Failed'),
 (13, N'paid-replacement', 60, '2026-02-03', '2026-02-03', N'MBWayPayment', N'paid-key', 12.34, 12.34, N'keep replacement', N'Paid'),
 (14, N'additional-reconcile', 61, '2026-02-01', '2026-02-02', N'CreditCardPayment', N'additional-key', 5, 5, N'keep confirmed', N'Success'),
 (15, N'additional-reconcile', NULL, '2026-02-01', '2026-02-02', N'CreditCardPayment', N'additional-key', 5, 5, N'keep history', N'Success');
UPDATE dbo.Donations SET ConfirmedPaymentId = 13 WHERE Id = 60;
UPDATE dbo.Donations SET ConfirmedPaymentId = 14 WHERE Id = 61;
INSERT dbo.ExpectedPayments SELECT * FROM dbo.Payments WHERE Id BETWEEN 11 AND 15;
INSERT dbo.ExpectedDonations SELECT * FROM dbo.Donations WHERE Id IN (60, 61);
CREATE TABLE dbo.ExpectedDeletionJson (PaymentId int PRIMARY KEY, PaymentJson nvarchar(max) NOT NULL);
INSERT dbo.ExpectedDeletionJson SELECT payment.Id,
 (SELECT original.* FROM dbo.Payments original WHERE original.Id = payment.Id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES)
 FROM dbo.Payments payment WHERE payment.Id IN (11, 12);
'@ | Out-Null
    $deletionPlan = Get-ApprovedDeletionFixturePlan
    $deletionGroup = $deletionPlan.Groups | Where-Object GroupId -eq 11
    if (-not $deletionGroup.CanDeleteGroup -or $deletionGroup.SuggestedAction -ne 'RetireIdentifier' -or $deletionGroup.DecisionRule -ne 'NoOwnerRetire' -or $deletionGroup.DeletionBlockedByFinancialEvidence -or @($deletionGroup.Members | Where-Object { -not $_.HasSuccessfulReplacement -or $_.DonationConfirmedPaymentId -ne 13 }).Count -ne 0) { throw 'Paid-replacement deletion evidence is incorrect.' }
    if ($deletionGroup.ExternalConfirmationCandidateCount -ne 2 -or @($deletionGroup.Members | Where-Object { -not $_.HasExternalDonationConfirmation -or -not $_.DeletionCandidate -or $_.DeletionCandidateReason -ne 'PaidDonationConfirmedOutsideGroup' }).Count -ne 0) { throw 'Paid donation with an out-of-group confirmation did not mark every related member.' }
    Write-Pass 'A Payed donation confirmed outside the entire group marks each related payment with a specific review reason.'
    Invoke-TestSql 'UPDATE dbo.Donations SET ConfirmedPaymentId = 11 WHERE Id = 60;' | Out-Null
    $insideGroup = ((Invoke-TestSql $sql) -join '') | ConvertFrom-Json
    $insideGroup = $insideGroup.Groups | Where-Object GroupId -eq 11
    if (@($insideGroup.Members | Where-Object HasExternalDonationConfirmation).Count -ne 0 -or @($insideGroup.Members | Where-Object DeletionCandidateReason -eq 'PaidDonationConfirmedOutsideGroup').Count -ne 0) { throw 'A sibling confirmation was mistaken for a confirmation outside the group.' }
    Invoke-TestSql 'UPDATE dbo.Donations SET ConfirmedPaymentId = 13 WHERE Id = 60;' | Out-Null
    Write-Pass 'A confirmation pointing to either sibling is not treated as outside the payment group.'
    Invoke-TestSql 'UPDATE dbo.Donations SET ConfirmedPaymentId = NULL WHERE Id = 60;' | Out-Null
    $missingConfirmation = ((Invoke-TestSql $sql) -join '') | ConvertFrom-Json
    if (@(($missingConfirmation.Groups | Where-Object GroupId -eq 11).Members | Where-Object HasExternalDonationConfirmation).Count -ne 0) { throw 'NULL ConfirmedPaymentId was incorrectly treated as an external payment.' }
    Invoke-TestSql 'UPDATE dbo.Donations SET ConfirmedPaymentId = 13, PaymentStatus = 2 WHERE Id = 60;' | Out-Null
    $unpaidDonation = ((Invoke-TestSql $sql) -join '') | ConvertFrom-Json
    if (@(($unpaidDonation.Groups | Where-Object GroupId -eq 11).Members | Where-Object HasExternalDonationConfirmation).Count -ne 0) { throw 'A non-Payed donation authorized the external-confirmation review rule.' }
    Invoke-TestSql 'UPDATE dbo.Donations SET PaymentStatus = 1 WHERE Id = 60;' | Out-Null
    Write-Pass 'External-confirmation review requires PaymentStatus 1 and a non-null confirmation.'
    $unapprovedDeletion = (((Invoke-TestSql $sql) -join '') | ConvertFrom-Json).Groups | Where-Object GroupId -eq 11
    if ($unapprovedDeletion.Action -ne 'RetireIdentifier' -or $null -ne $unapprovedDeletion.CanonicalPaymentId) { throw 'A generated plan must prefill non-destructive retirement, never deletion.' }
    Write-Pass 'Paid external confirmation resolves the failed-attempt warning and prefills RetireIdentifier, never DeleteGroup.'

    $bad = Copy-Plan $deletionPlan
    $bad.PlanVersion = 4
    Assert-Rejected $bad 51006 'Version-4 plans cannot skip the confirmed-payment decision rules.' -KeepAudit

    Invoke-TestSql 'UPDATE dbo.Payments SET Status = NULL WHERE Id = 12;' | Out-Null
    $fallbackGroup = (((Invoke-TestSql $sql) -join '') | ConvertFrom-Json).Groups | Where-Object GroupId -eq 11
    Invoke-TestSql "UPDATE dbo.Payments SET Status = N'Failed' WHERE Id = 12;" | Out-Null
    $fallbackMember = $fallbackGroup.Members | Where-Object PaymentId -eq 12
    if (-not $fallbackGroup.HasSafeDeletionEvidence -or $fallbackMember.DeletionBlockedByFinancialEvidence -or $fallbackMember.DeletionBasis -ne 'SupersededByReplacement') { throw 'Fallback success from a donation paid by the replacement blocked deletion.' }
    Write-Pass 'A NULL-status attempt whose donation was paid by an external replacement is deletable despite the donation fallback.'

    Invoke-TestSql "UPDATE dbo.Payments SET Discriminator = N'PayPalPayment', Status = N'COMPLETED', Requested = NULL, Paid = NULL, PayPalPaymentId = N'PAYID-FIXTURE', PayerId = N'PAYER-FIXTURE' WHERE Id = 13;" | Out-Null
    $payPalGroup = (((Invoke-TestSql $sql) -join '') | ConvertFrom-Json).Groups | Where-Object GroupId -eq 11
    Invoke-TestSql 'UPDATE dbo.Payments SET PayerId = NULL WHERE Id = 13;' | Out-Null
    $noPayerGroup = (((Invoke-TestSql $sql) -join '') | ConvertFrom-Json).Groups | Where-Object GroupId -eq 11
    Invoke-TestSql "UPDATE dbo.Payments SET Discriminator = N'MBWayPayment', Status = N'Paid', Requested = 12.34, Paid = 12.34, PayPalPaymentId = NULL WHERE Id = 13;" | Out-Null
    Invoke-TestSql 'EXEC dbo.AssertBaseline;' | Out-Null
    if (-not $payPalGroup.HasSafeDeletionEvidence -or $noPayerGroup.HasSafeDeletionEvidence) { throw 'PayPal replacement validation is incorrect.' }
    Write-Pass 'A COMPLETED PayPal replacement with payment and payer IDs covers failed attempts; a missing payer ID does not.'

    $bad = Copy-Plan $deletionPlan
    $bad.PlanVersion = 3
    Assert-Rejected $bad 51006 'Version-3 plans cannot skip per-payment relationship deletion review.' -KeepAudit
    $bad = Copy-Plan $deletionPlan
    $bad.PlanVersion = 2
    Assert-Rejected $bad 51006 'Version-2 plans cannot bypass explicit action and replacement review.' -KeepAudit
    $bad = Copy-Plan $deletionPlan
    ($bad.Groups | Where-Object GroupId -eq 11).Action = $null
    Assert-Rejected $bad 51010 'Missing Action is rejected rather than inferred from a deletion marker.' -KeepAudit
    $bad = Copy-Plan $deletionPlan
    ($bad.Groups | Where-Object GroupId -eq 11).Action = 'DeleteEverything'
    Assert-Rejected $bad 51010 'Unknown actions are rejected.' -KeepAudit
    $bad = Copy-Plan $deletionPlan
    ($bad.Groups | Where-Object GroupId -eq 11).CanonicalPaymentId = 11
    Assert-Rejected $bad 51002 'DeleteGroup cannot specify a survivor.' -KeepAudit
    $bad = Copy-Plan $deletionPlan
    ($bad.Groups | Where-Object GroupId -eq 11).Reason = $null
    Assert-Rejected $bad 51002 'Deletion requires an explicit group reason.' -KeepAudit

    $unsafeCases = @(
        @{ Before = "UPDATE dbo.Payments SET Status = N'Failed' WHERE Id = 13;"; After = "UPDATE dbo.Payments SET Status = N'Paid' WHERE Id = 13;"; Message = 'Failed replacement cannot justify deletion.' },
        @{ Before = 'UPDATE dbo.Payments SET Status = NULL WHERE Id = 13;'; After = "UPDATE dbo.Payments SET Status = N'Paid' WHERE Id = 13;"; Message = 'A null replacement status cannot circularly inherit success from its paid donation.' },
        @{ Before = 'UPDATE dbo.Payments SET Paid = 0 WHERE Id = 13;'; After = 'UPDATE dbo.Payments SET Paid = 12.34 WHERE Id = 13;'; Message = 'Zero paid amount on the replacement blocks deletion.' },
        @{ Before = 'UPDATE dbo.Payments SET Paid = 12 WHERE Id = 13;'; After = 'UPDATE dbo.Payments SET Paid = 12.34 WHERE Id = 13;'; Message = 'Replacement amount must match its donation.' },
        @{ Before = 'UPDATE dbo.Payments SET DonationId = 50 WHERE Id = 13;'; After = 'UPDATE dbo.Payments SET DonationId = 60 WHERE Id = 13;'; Message = 'A replacement linked to another donation is rejected.' },
        @{ Before = 'UPDATE dbo.Donations SET PaymentStatus = 2 WHERE Id = 60;'; After = 'UPDATE dbo.Donations SET PaymentStatus = 1 WHERE Id = 60;'; Message = 'The covering donation must be marked Payed.' },
        @{ Before = 'UPDATE dbo.Donations SET ConfirmedPaymentId = NULL WHERE Id = 60;'; After = 'UPDATE dbo.Donations SET ConfirmedPaymentId = 13 WHERE Id = 60;'; Message = 'Missing replacement confirmation blocks deletion.' },
        @{ Before = 'UPDATE dbo.Donations SET ConfirmedPaymentId = 11 WHERE Id = 60;'; After = 'UPDATE dbo.Donations SET ConfirmedPaymentId = 13 WHERE Id = 60;'; Message = 'A group member cannot serve as its own external replacement.' },
        @{ Before = 'UPDATE dbo.Payments SET Paid = 1 WHERE Id = 11;'; After = 'UPDATE dbo.Payments SET Paid = 0 WHERE Id = 11;'; Message = 'A target with paid money cannot be deleted.' },
        @{ Before = "UPDATE dbo.Payments SET Completed = '2026-02-02' WHERE Id = 11;"; After = 'UPDATE dbo.Payments SET Completed = NULL WHERE Id = 11;'; Message = 'A completed target cannot be deleted.' },
        @{ Before = "UPDATE dbo.Payments SET Status = N'Success' WHERE Id = 11;"; After = "UPDATE dbo.Payments SET Status = N'Failed' WHERE Id = 11;"; Message = 'A successful target cannot be deleted.' },
        @{ Before = "UPDATE dbo.Payments SET DonationId = NULL, Status = N'Success' WHERE Id = 11;"; After = "UPDATE dbo.Payments SET DonationId = 60, Status = N'Failed' WHERE Id = 11;"; Message = 'An unlinked target with its own successful status is neither superseded nor abandoned.' }
    )
    foreach ($case in $unsafeCases) {
        Invoke-TestSql $case.Before | Out-Null
        $unsafe = Get-ApprovedDeletionFixturePlan
        Assert-Rejected $unsafe 51011 $case.Message -After $case.After -KeepAudit
    }

    Assert-Rejected $deletionPlan 51007 'Changing any external replacement column invalidates the plan.' "UPDATE dbo.Payments SET ExtraPayload = N'changed replacement' WHERE Id = 13;" "UPDATE dbo.Payments SET ExtraPayload = N'keep replacement' WHERE Id = 13;" -KeepAudit
    Assert-Rejected $deletionPlan 51012 'A new notification dependency blocks deletion without touching history.' "INSERT dbo.PaymentNotifications VALUES (100, 11, 1, N'Preserve dependency');" "IF NOT EXISTS (SELECT 1 FROM dbo.PaymentNotifications WHERE Id = 100) THROW 51130, 'Notification was lost.', 1; DELETE dbo.PaymentNotifications WHERE Id = 100;" -KeepAudit
    Assert-Rejected $deletionPlan 51012 'A new payment-item/reference dependency blocks deletion.' 'INSERT dbo.PaymentReferences VALUES (100, 11);' 'DELETE dbo.PaymentReferences WHERE Id = 100;' -KeepAudit
    Assert-Rejected $deletionPlan 51012 'CASCADE dependencies are blocked, not silently deleted.' 'CREATE TABLE dbo.CascadeDependency (Id int PRIMARY KEY, PaymentId int REFERENCES dbo.Payments(Id) ON DELETE CASCADE); INSERT dbo.CascadeDependency VALUES (1, 11);' "IF NOT EXISTS (SELECT 1 FROM dbo.CascadeDependency WHERE Id = 1) THROW 51131, 'Cascade removed history.', 1; DROP TABLE dbo.CascadeDependency;" -KeepAudit
    Assert-Rejected $deletionPlan 51012 'SET NULL dependencies are blocked, not silently rewritten.' 'CREATE TABLE dbo.SetNullDependency (Id int PRIMARY KEY, PaymentId int NULL REFERENCES dbo.Payments(Id) ON DELETE SET NULL); INSERT dbo.SetNullDependency VALUES (1, 11);' "IF NOT EXISTS (SELECT 1 FROM dbo.SetNullDependency WHERE Id = 1 AND PaymentId = 11) THROW 51132, 'Dependency link changed.', 1; DROP TABLE dbo.SetNullDependency;" -KeepAudit
    Assert-Rejected $deletionPlan 51012 'Unsupported composite foreign keys fail closed.' 'CREATE UNIQUE INDEX IX_CompositeTarget ON dbo.Payments(Id, DonationId); CREATE TABLE dbo.CompositeDependency (PaymentId int, DonationId int, FOREIGN KEY (PaymentId, DonationId) REFERENCES dbo.Payments(Id, DonationId));' 'DROP TABLE dbo.CompositeDependency; DROP INDEX IX_CompositeTarget ON dbo.Payments;' -KeepAudit
    Assert-Rejected $deletionPlan 51012 'Enabled payment triggers block deletion before side effects.' "EXEC(N'CREATE TRIGGER dbo.BlockUnsafeDelete ON dbo.Payments AFTER DELETE AS BEGIN INSERT dbo.GuardWork VALUES (99); END;');" "IF EXISTS (SELECT 1 FROM dbo.GuardWork WHERE Id = 99) THROW 51133, 'Delete trigger ran.', 1; DROP TRIGGER dbo.BlockUnsafeDelete;" -KeepAudit
    Assert-Rejected $deletionPlan 51012 'Missing database metadata visibility blocks deletion.' 'CREATE USER LimitedDeletionTest WITHOUT LOGIN; GRANT SELECT, UPDATE, DELETE ON dbo.Payments TO LimitedDeletionTest; GRANT SELECT ON dbo.Donations TO LimitedDeletionTest; GRANT SELECT ON dbo.Invoices TO LimitedDeletionTest; EXECUTE AS USER = N''LimitedDeletionTest'';' 'REVERT; DROP USER LimitedDeletionTest;' -KeepAudit

    $archiveDefinition = 'CREATE TABLE dbo.EasyPayPaymentDeletionAudit (PaymentId int PRIMARY KEY, GroupId int NOT NULL, ReplacementPaymentId int NULL, OriginalEasyPayPaymentId nvarchar(max) NOT NULL, DeletedPaymentJson nvarchar(max) NOT NULL, PlanEvidenceHash varchar(64) NOT NULL, DeletedAtUtc datetime2(7) NOT NULL);'
    Assert-Rejected $deletionPlan 51003 'Existing deletion archives cannot be overwritten.' ($archiveDefinition + " INSERT dbo.EasyPayPaymentDeletionAudit VALUES (11, 11, 13, N'delete-group', N'{}', REPLICATE('0',64), SYSUTCDATETIME());") 'DROP TABLE dbo.EasyPayPaymentDeletionAudit;' -KeepAudit
    Assert-Rejected $deletionPlan 51090 'Archive failure rolls back identifier updates and the complete mixed plan.' ($archiveDefinition + " EXEC(N'CREATE TRIGGER dbo.FailArchive ON dbo.EasyPayPaymentDeletionAudit AFTER INSERT AS BEGIN ;THROW 51090, ''Injected archive failure.'', 1; END;');") 'DROP TRIGGER dbo.FailArchive; DROP TABLE dbo.EasyPayPaymentDeletionAudit;' -KeepAudit

    $deletionPath = Join-Path $temporaryDirectory 'delete-approved.plan.json'
    [System.IO.File]::WriteAllText($deletionPath, ($deletionPlan | ConvertTo-Json -Depth 50))
    & $runner -Mode DryRun -ServerInstance $ServerInstance -Database $database -PlanPath $deletionPath | Out-Null
    Invoke-TestSql "EXEC dbo.AssertBaseline; IF OBJECT_ID(N'dbo.EasyPayPaymentDeletionAudit', N'U') IS NOT NULL OR (SELECT COUNT(*) FROM dbo.EasyPayPaymentIdReconciliationPlans) <> 1 THROW 51134, 'Deletion dry run retained changes.', 1;" | Out-Null
    Write-Pass 'Explicit deletion dry run restores all target rows, identifier edits, donation links, and deletion-audit creation.'
    & $runner -Mode Apply -ServerInstance $ServerInstance -Database $database -PlanPath $deletionPath -WhatIf | Out-Null
    Invoke-TestSql 'EXEC dbo.AssertBaseline;' | Out-Null
    Write-Pass 'DeleteGroup Apply WhatIf performs no SQL changes.'
    & $runner -Mode Apply -ServerInstance $ServerInstance -Database $database -PlanPath $deletionPath -Confirm:$false | Out-Null
    Invoke-TestSql @'
DELETE dbo.ExpectedPayments WHERE Id IN (11, 12);
UPDATE dbo.ExpectedPayments SET EasyPayPaymentId = NULL WHERE Id = 15;
EXEC dbo.AssertBaseline;
IF (SELECT COUNT(*) FROM dbo.EasyPayPaymentDeletionAudit) <> 2 THROW 51135, 'Deletion archive count is incorrect.', 1;
IF EXISTS (SELECT 1 FROM dbo.EasyPayPaymentDeletionAudit audit JOIN dbo.ExpectedDeletionJson expected ON expected.PaymentId = audit.PaymentId WHERE audit.DeletedPaymentJson COLLATE Latin1_General_100_BIN2 <> expected.PaymentJson COLLATE Latin1_General_100_BIN2 OR audit.ReplacementPaymentId <> 13 OR audit.GroupId <> 11 OR audit.OriginalEasyPayPaymentId <> N'delete-group') THROW 51136, 'Full-row deletion archive differs from original data.', 1;
IF (SELECT COUNT(*) FROM dbo.EasyPayPaymentIdReconciliationPlans) <> 2 OR (SELECT COUNT(*) FROM dbo.EasyPayPaymentIdReconciliationAudit) <> 5 THROW 51137, 'Mixed-action audit history is incorrect.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.Payments WHERE Id = 13 AND Status = N'Paid' AND DonationId = 60) OR NOT EXISTS (SELECT 1 FROM dbo.Donations WHERE Id = 60 AND ConfirmedPaymentId = 13 AND PaymentStatus = 1) THROW 51138, 'Paid replacement or its donation was modified.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.EasyPayPaymentDeletionAudit archive JOIN dbo.EasyPayPaymentIdReconciliationPlans approved ON approved.PlanEvidenceHash = archive.PlanEvidenceHash CROSS APPLY OPENJSON(approved.ApprovedPlan, '$.Groups') WITH (GroupId int, Action nvarchar(30), Reason nvarchar(2000)) action WHERE action.GroupId = 11 AND action.Action = N'DeleteGroup' AND action.Reason IS NOT NULL) THROW 51139, 'Deletion approval cannot be traced.', 1;
ALTER TABLE dbo.Payments ALTER COLUMN EasyPayPaymentId nvarchar(128) NULL;
CREATE UNIQUE INDEX IX_Payments_EasyPayPaymentId ON dbo.Payments(EasyPayPaymentId) WHERE EasyPayPaymentId IS NOT NULL;
'@ | Out-Null
    Write-Pass 'Explicit mixed apply archives exact full rows before deleting only approved failed attempts, preserves the paid replacement and every other record/link, and permits the unique index.'
    Assert-Rejected $deletionPlan 51007 'Replaying a deletion plan cannot delete or archive a second time.' -KeepAudit

    Invoke-TestSql @'
DROP INDEX IX_Payments_EasyPayPaymentId ON dbo.Payments;
INSERT dbo.Donations (Id, ConfirmedPaymentId, DonationAmount, PaymentStatus) VALUES (70, NULL, 8, 2);
INSERT dbo.Payments (Id, EasyPayPaymentId, DonationId, Created, Completed, Discriminator, TransactionKey, Requested, Paid, ExtraPayload, Status) VALUES
 (16, N'retire-group', 70, '2026-03-01', NULL, N'MBWayPayment', N'retire-first', 8, 0, N'keep first attempt', N'pending'),
 (17, N'retire-group', NULL, '2026-03-02', NULL, N'MBWayPayment', N'retire-second', 0, 0, N'keep second attempt', NULL);
INSERT dbo.PaymentNotifications VALUES (200, 16, 1, N'Keep retired history');
INSERT dbo.ExpectedPayments SELECT * FROM dbo.Payments WHERE Id IN (16, 17);
INSERT dbo.ExpectedDonations SELECT * FROM dbo.Donations WHERE Id = 70;
INSERT dbo.ExpectedNotifications SELECT * FROM dbo.PaymentNotifications WHERE Id = 200;
'@ | Out-Null
    $retirePlan = ((Invoke-TestSql $sql) -join '') | ConvertFrom-Json
    $retireGroup = $retirePlan.Groups | Where-Object GroupId -eq 16
    if (@($retirePlan.Groups).Count -ne 1 -or $retireGroup.DecisionRule -ne 'NoOwnerRetire' -or $retireGroup.Action -ne 'RetireIdentifier' -or $null -ne $retireGroup.CanonicalPaymentId -or [string]::IsNullOrWhiteSpace($retireGroup.Reason)) { throw 'An abandoned attempt group was not prefilled for retirement.' }
    Write-Pass 'Groups with no confirmed, successful, completed, or paid member are prefilled with RetireIdentifier.'

    $retireProbes = @(
        @{ Before = "UPDATE dbo.Payments SET Status = N'Success', Paid = 8, Completed = '2026-03-01' WHERE Id = 16;"; After = "UPDATE dbo.Payments SET Status = N'pending', Paid = 0, Completed = NULL WHERE Id = 16;"; Rule = 'ReviewUnconfirmedCapture' },
        @{ Before = 'UPDATE dbo.Payments SET Paid = 8 WHERE Id = 17;'; After = 'UPDATE dbo.Payments SET Paid = 0 WHERE Id = 17;'; Rule = 'ReviewUnconfirmedCapture' },
        @{ Before = 'UPDATE dbo.Donations SET PaymentStatus = 1 WHERE Id = 70;'; After = 'UPDATE dbo.Donations SET PaymentStatus = 2 WHERE Id = 70;'; Rule = 'ReviewUnconfirmedPaidDonation' },
        @{ Before = 'UPDATE dbo.Donations SET ConfirmedPaymentId = 16 WHERE Id = 70;'; After = 'UPDATE dbo.Donations SET ConfirmedPaymentId = NULL WHERE Id = 70;'; Rule = 'ReviewConfirmedOwner' }
    )
    foreach ($probe in $retireProbes) {
        Invoke-TestSql $probe.Before | Out-Null
        $probeGroup = (((Invoke-TestSql $sql) -join '') | ConvertFrom-Json).Groups | Where-Object GroupId -eq 16
        Invoke-TestSql $probe.After | Out-Null
        if ($probeGroup.DecisionRule -ne $probe.Rule -or $null -ne $probeGroup.Action) { throw "Retirement guard failed: expected $($probe.Rule), got $($probeGroup.DecisionRule)." }
    }
    Invoke-TestSql 'EXEC dbo.AssertBaseline;' | Out-Null
    Write-Pass 'Unconfirmed captures, unconfirmed Payed donations, and confirmed members are never prefilled for retirement.'
    Invoke-TestSql 'UPDATE dbo.Payments SET Status = NULL WHERE Id = 16; UPDATE dbo.Donations SET PaymentStatus = 1, ConfirmedPaymentId = 10 WHERE Id = 70;' | Out-Null
    $probeGroup = (((Invoke-TestSql $sql) -join '') | ConvertFrom-Json).Groups | Where-Object GroupId -eq 16
    Invoke-TestSql "UPDATE dbo.Payments SET Status = N'pending' WHERE Id = 16; UPDATE dbo.Donations SET PaymentStatus = 2, ConfirmedPaymentId = NULL WHERE Id = 70;" | Out-Null
    if ($probeGroup.DecisionRule -ne 'NoOwnerRetire' -or $probeGroup.Action -ne 'RetireIdentifier' -or $probeGroup.SuccessfulStatusCount -ne 1 -or $probeGroup.OwnSuccessCount -ne 0) { throw 'Fallback success from a donation paid elsewhere blocked retirement.' }
    Write-Pass 'NULL-status attempts superseded by a payment outside the group are retired despite the donation fallback.'

    $retireApproved = Copy-Plan $retirePlan
    $retireApproved.ApprovedBy = 'Disposable retirement-test reviewer'
    $retireApproved.ApprovalReason = 'Fixture only: abandoned attempts keep their rows; clear the shared identifier.'
    $bad = Copy-Plan $retireApproved
    ($bad.Groups | Where-Object GroupId -eq 16).CanonicalPaymentId = 16
    Assert-Rejected $bad 51002 'RetireIdentifier cannot specify a survivor.' -KeepAudit
    Invoke-TestSql 'UPDATE dbo.Donations SET ConfirmedPaymentId = 16 WHERE Id = 70;' | Out-Null
    $bad = ((Invoke-TestSql $sql) -join '') | ConvertFrom-Json
    $bad.ApprovedBy = $retireApproved.ApprovedBy
    $bad.ApprovalReason = $retireApproved.ApprovalReason
    $badGroup = $bad.Groups | Where-Object GroupId -eq 16
    $badGroup.Action = 'RetireIdentifier'
    $badGroup.Reason = 'Attempt to retire an identifier stored on a confirmed payment.'
    Assert-Rejected $bad 51013 'RetireIdentifier refuses groups containing a donation-confirmed payment.' -After 'UPDATE dbo.Donations SET ConfirmedPaymentId = NULL WHERE Id = 70;' -KeepAudit

    $retirePath = Join-Path $temporaryDirectory 'retire-approved.plan.json'
    [System.IO.File]::WriteAllText($retirePath, ($retireApproved | ConvertTo-Json -Depth 50))
    & $runner -Mode DryRun -ServerInstance $ServerInstance -Database $database -PlanPath $retirePath | Out-Null
    Invoke-TestSql "EXEC dbo.AssertBaseline; IF OBJECT_ID(N'dbo.EasyPayPaymentIdRetirementAudit', N'U') IS NOT NULL OR (SELECT COUNT(*) FROM dbo.EasyPayPaymentIdReconciliationPlans) <> 2 THROW 51140, 'Retirement dry run retained changes.', 1;" | Out-Null
    Write-Pass 'RetireIdentifier dry run restores every identifier and rolls back retirement-audit creation.'
    & $runner -Mode Apply -ServerInstance $ServerInstance -Database $database -PlanPath $retirePath -Confirm:$false | Out-Null
    Invoke-TestSql @'
UPDATE dbo.ExpectedPayments SET EasyPayPaymentId = NULL WHERE Id IN (16, 17);
EXEC dbo.AssertBaseline;
IF (SELECT COUNT(*) FROM dbo.EasyPayPaymentIdRetirementAudit) <> 2
 OR EXISTS (SELECT 1 FROM dbo.EasyPayPaymentIdRetirementAudit WHERE GroupId <> 16 OR OriginalEasyPayPaymentId <> N'retire-group' OR PaymentId NOT IN (16, 17))
 THROW 51141, 'Retirement audit is incorrect.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.EasyPayPaymentIdRetirementAudit audit JOIN dbo.EasyPayPaymentIdReconciliationPlans approved ON approved.PlanEvidenceHash = audit.PlanEvidenceHash) THROW 51142, 'Retirement approval cannot be traced.', 1;
IF (SELECT COUNT(*) FROM dbo.EasyPayPaymentIdReconciliationPlans) <> 3 OR (SELECT COUNT(*) FROM dbo.EasyPayPaymentIdReconciliationAudit) <> 5 THROW 51143, 'Retirement changed other audit history.', 1;
CREATE UNIQUE INDEX IX_Payments_EasyPayPaymentId ON dbo.Payments(EasyPayPaymentId) WHERE EasyPayPaymentId IS NOT NULL;
'@ | Out-Null
    Write-Pass 'Explicit RetireIdentifier clears the ID on every member, keeps all rows/links/notifications, audits originals, and permits the unique index.'
    Assert-Rejected $retireApproved 51007 'Replaying a retirement plan fails closed.' -KeepAudit

    Invoke-TestSql @'
DROP INDEX IX_Payments_EasyPayPaymentId ON dbo.Payments;
INSERT dbo.Payments (Id, EasyPayPaymentId, DonationId, Created, Completed, Discriminator, TransactionKey, Requested, Paid, ExtraPayload, Status) VALUES
 (18, N'orphan-delete', NULL, '2026-04-01', NULL, N'MultiBankPayment', N'orphan-first', 0, 0, N'archive orphan first', NULL),
 (19, N'orphan-delete', NULL, '2026-04-01', NULL, N'MultiBankPayment', N'orphan-second', 0, 0, N'archive orphan second', NULL);
INSERT dbo.ExpectedPayments SELECT * FROM dbo.Payments WHERE Id IN (18, 19);
DELETE dbo.ExpectedDeletionJson;
INSERT dbo.ExpectedDeletionJson SELECT payment.Id,
 (SELECT original.* FROM dbo.Payments original WHERE original.Id = payment.Id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES)
 FROM dbo.Payments payment WHERE payment.Id IN (18, 19);
'@ | Out-Null
    $orphanPlan = ((Invoke-TestSql $sql) -join '') | ConvertFrom-Json
    $orphanGroup = $orphanPlan.Groups | Where-Object GroupId -eq 18
    if (-not $orphanGroup.CanDeleteGroup -or -not $orphanGroup.HasSafeDeletionEvidence -or $orphanGroup.Action -ne 'RetireIdentifier' -or @($orphanGroup.Members | Where-Object { $_.DeletionBasis -ne 'AbandonedOrphan' -or $_.DeletionBlockedByUnknownStatus }).Count -ne 0) { throw 'Abandoned orphan attempts were not eligible for explicit deletion.' }
    Write-Pass 'Abandoned orphan attempts (no donation, money, completion, or success) qualify for explicit deletion, while retirement stays the default.'

    Invoke-TestSql "UPDATE dbo.Payments SET Status = N'Success' WHERE Id = 19;" | Out-Null
    $bad = ((Invoke-TestSql $sql) -join '') | ConvertFrom-Json
    $bad.ApprovedBy = 'Disposable orphan-test reviewer'
    $bad.ApprovalReason = 'Fixture only.'
    $badGroup = $bad.Groups | Where-Object GroupId -eq 18
    $badGroup.Action = 'DeleteGroup'
    $badGroup.Reason = 'Attempt to delete an unlinked successful payment.'
    Assert-Rejected $bad 51011 'An unlinked member with successful status blocks orphan deletion.' -After 'UPDATE dbo.Payments SET Status = NULL WHERE Id = 19;' -KeepAudit

    $orphanDelete = Copy-Plan $orphanPlan
    $orphanDelete.ApprovedBy = 'Disposable orphan-test reviewer'
    $orphanDelete.ApprovalReason = 'Fixture only: abandoned Multibanco attempts with no donation.'
    $orphanDeleteGroup = $orphanDelete.Groups | Where-Object GroupId -eq 18
    $orphanDeleteGroup.Action = 'DeleteGroup'
    $orphanDeleteGroup.Reason = 'Abandoned attempts without donation, money, or completion; archive and delete.'
    $orphanPath = Join-Path $temporaryDirectory 'orphan-delete.plan.json'
    [System.IO.File]::WriteAllText($orphanPath, ($orphanDelete | ConvertTo-Json -Depth 50))
    & $runner -Mode Apply -ServerInstance $ServerInstance -Database $database -PlanPath $orphanPath -Confirm:$false | Out-Null
    Invoke-TestSql @'
DELETE dbo.ExpectedPayments WHERE Id IN (18, 19);
EXEC dbo.AssertBaseline;
IF (SELECT COUNT(*) FROM dbo.EasyPayPaymentDeletionAudit WHERE PaymentId IN (18, 19) AND GroupId = 18 AND ReplacementPaymentId IS NULL AND OriginalEasyPayPaymentId = N'orphan-delete') <> 2 THROW 51144, 'Orphan deletion archive is incorrect.', 1;
IF EXISTS (SELECT 1 FROM dbo.EasyPayPaymentDeletionAudit audit JOIN dbo.ExpectedDeletionJson expected ON expected.PaymentId = audit.PaymentId WHERE audit.DeletedPaymentJson COLLATE Latin1_General_100_BIN2 <> expected.PaymentJson COLLATE Latin1_General_100_BIN2) THROW 51145, 'Orphan archive differs from original rows.', 1;
CREATE UNIQUE INDEX IX_Payments_EasyPayPaymentId ON dbo.Payments(EasyPayPaymentId) WHERE EasyPayPaymentId IS NOT NULL;
'@ | Out-Null
    Write-Pass 'Explicit DeleteGroup archives and deletes abandoned orphan attempts with a null replacement and preserves every other record.'
    Write-Output "All $passed reconciliation workflow checks passed."
}
finally {
    if ($created) {
        & sqlcmd -S $ServerInstance -E -b -I -x -l 30 -d master -Q "ALTER DATABASE [$database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$database];"
        if ($LASTEXITCODE -ne 0) { throw "Could not remove disposable database $database" }
        Write-Output 'Disposable test database removed.'
    }
    if (Test-Path -LiteralPath $temporaryDirectory) { Remove-Item -LiteralPath $temporaryDirectory -Recurse -Force }
}