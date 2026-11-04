// -----------------------------------------------------------------------
// <copyright file="AuditDbContext.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data
{
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Maps the audit tables created by <c>MergeDuplicateEasyPayPaymentIds.sql</c>. The tables are not part of
    /// the application model, so this context never creates or migrates them; see <see cref="CreateTablesSql"/>.
    /// </summary>
    public class AuditDbContext : DbContext
    {
        /// <summary>
        /// DDL identical to the SQL script. It only creates missing tables.
        /// </summary>
        public const string CreateTablesSql = @"
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
END;";

        /// <summary>
        /// Initializes a new instance of the <see cref="AuditDbContext"/> class.
        /// </summary>
        /// <param name="options">Options.</param>
        public AuditDbContext(DbContextOptions<AuditDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets the reconciliation audit rows.
        /// </summary>
        public DbSet<ReconciliationAuditRow> ReconciliationAudit => this.Set<ReconciliationAuditRow>();

        /// <summary>
        /// Gets the applied plans.
        /// </summary>
        public DbSet<ReconciliationPlanRow> Plans => this.Set<ReconciliationPlanRow>();

        /// <summary>
        /// Gets the retirement audit rows.
        /// </summary>
        public DbSet<RetirementAuditRow> RetirementAudit => this.Set<RetirementAuditRow>();

        /// <summary>
        /// Gets the deletion audit rows.
        /// </summary>
        public DbSet<DeletionAuditRow> DeletionAudit => this.Set<DeletionAuditRow>();

        /// <inheritdoc/>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ReconciliationAuditRow>(entity =>
            {
                entity.ToTable("EasyPayPaymentIdReconciliationAudit", "dbo");
                entity.HasKey(row => row.PaymentId);
                entity.Property(row => row.PaymentId).ValueGeneratedNever();
                entity.Property(row => row.OriginalEasyPayPaymentId).HasColumnType("nvarchar(max)");
                entity.Property(row => row.ReconciledAtUtc).HasColumnType("datetime2(7)");
            });

            modelBuilder.Entity<ReconciliationPlanRow>(entity =>
            {
                entity.ToTable("EasyPayPaymentIdReconciliationPlans", "dbo");
                entity.HasKey(row => row.PlanEvidenceHash);
                entity.Property(row => row.PlanEvidenceHash).HasColumnType("varchar(64)").HasMaxLength(64).IsUnicode(false);
                entity.Property(row => row.ApprovedBy).HasMaxLength(200);
                entity.Property(row => row.ApprovalReason).HasMaxLength(2000);
                entity.Property(row => row.ApprovedPlan).HasColumnType("nvarchar(max)");
                entity.Property(row => row.AppliedAtUtc).HasColumnType("datetime2(7)");
                entity.Property(row => row.ExecutedBy).HasMaxLength(128);
            });

            modelBuilder.Entity<RetirementAuditRow>(entity =>
            {
                entity.ToTable("EasyPayPaymentIdRetirementAudit", "dbo");
                entity.HasKey(row => row.PaymentId);
                entity.Property(row => row.PaymentId).ValueGeneratedNever();
                entity.Property(row => row.OriginalEasyPayPaymentId).HasColumnType("nvarchar(max)");
                entity.Property(row => row.PlanEvidenceHash).HasColumnType("varchar(64)").HasMaxLength(64).IsUnicode(false);
                entity.Property(row => row.RetiredAtUtc).HasColumnType("datetime2(7)");
            });

            modelBuilder.Entity<DeletionAuditRow>(entity =>
            {
                entity.ToTable("EasyPayPaymentDeletionAudit", "dbo");
                entity.HasKey(row => row.PaymentId);
                entity.Property(row => row.PaymentId).ValueGeneratedNever();
                entity.Property(row => row.OriginalEasyPayPaymentId).HasColumnType("nvarchar(max)");
                entity.Property(row => row.DeletedPaymentJson).HasColumnType("nvarchar(max)");
                entity.Property(row => row.PlanEvidenceHash).HasColumnType("varchar(64)").HasMaxLength(64).IsUnicode(false);
                entity.Property(row => row.DeletedAtUtc).HasColumnType("datetime2(7)");
            });
        }
    }
}
