// <copyright file="ApplicationDbContext.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Default DbContext for the project.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<
        WebUser, ApplicationRole, string,
        ApplicationUserClaim, ApplicationUserRole, ApplicationUserLogin,
        ApplicationRoleClaim, ApplicationUserToken>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
        /// </summary>
        /// <param name="options">DbContextOptions.</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="WebUser"/>.
        /// </summary>
        public DbSet<WebUser> WebUser { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="Donation"/>.
        /// </summary>
        public DbSet<Donation> Donations { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="DonationItem"/>.
        /// </summary>
        public DbSet<DonationItem> DonationItems { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="DonorAddress"/>.
        /// </summary>
        public DbSet<DonorAddress> DonorAddresses { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="FoodBank"/>.
        /// </summary>
        public DbSet<FoodBank> FoodBanks { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="ProductCatalogue"/>.
        /// </summary>
        public DbSet<ProductCatalogue> ProductCatalogues { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="Invoice"/>.
        /// </summary>
        public DbSet<Invoice> Invoices { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for fiscal issuer profiles.
        /// </summary>
        public DbSet<FiscalIssuerProfile> FiscalIssuerProfiles { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for fiscal document series.
        /// </summary>
        public DbSet<FiscalDocumentSeries> FiscalDocumentSeries { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for fiscal documents.
        /// </summary>
        public DbSet<FiscalDocument> FiscalDocuments { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for fiscal document lines.
        /// </summary>
        public DbSet<FiscalDocumentLine> FiscalDocumentLines { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for fiscal document events.
        /// </summary>
        public DbSet<FiscalDocumentEvent> FiscalDocumentEvents { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for fiscal audit events.
        /// </summary>
        public DbSet<FiscalAuditEvent> FiscalAuditEvents { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for development-only simulated receipts.
        /// </summary>
        public DbSet<SimulatedFiscalDocument> SimulatedFiscalDocuments { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for append-only development simulation audit events.
        /// </summary>
        public DbSet<SimulatedFiscalAuditEvent> SimulatedFiscalAuditEvents { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for development-only simulated counters.
        /// </summary>
        public DbSet<SimulatedFiscalSequence> SimulatedFiscalSequences { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="PayPalPayment"/>.
        /// </summary>
        public DbSet<PayPalPayment> PayPalPayments { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="MultiBankPayment"/>.
        /// </summary>
        public DbSet<MultiBankPayment> MultiBankPayments { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="CreditCardPayment"/>.
        /// </summary>
        public DbSet<CreditCardPayment> CreditCardPayments { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="MBWayPayment"/>.
        /// </summary>
        public DbSet<MBWayPayment> MBWayPayments { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="BasePayment"/>.
        /// </summary>
        public DbSet<BasePayment> Payments { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="Campaign"/>.
        /// </summary>
        public DbSet<Campaign> Campaigns { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="Subscription"/>.
        /// </summary>
        public DbSet<Subscription> Subscriptions { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="SubscriptionDonations"/>.
        /// </summary>
        public DbSet<SubscriptionDonations> SubscriptionDonations { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="Referral"/>.
        /// </summary>
        public DbSet<Referral> Referrals { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="ReferralLinkOpen"/>.
        /// </summary>
        public DbSet<ReferralLinkOpen> ReferralLinkOpens { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="UserLoginEvent"/>.
        /// </summary>
        public DbSet<UserLoginEvent> UserLoginEvents { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="PaymentNotifications"/>.
        /// </summary>
        public DbSet<PaymentNotifications> PaymentNotifications { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="EmailCommunication"/> records.
        /// </summary>
        public DbSet<EmailCommunication> EmailCommunications { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="Configuration"/>.
        /// </summary>
        public DbSet<Configuration> Configurations { get; set; }

        /// <inheritdoc />
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            this.ValidateFiscalIssuerProfiles();
            this.ValidateFiscalDocuments();
            this.ProtectSimulatedFiscalEvidence();
            this.UpdateDonationPeriodoOficial();
            this.RejectOrphanDonationItems();
            this.UpdateDonationConcurrencyTokens();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        /// <inheritdoc />
        public override Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            this.ValidateFiscalIssuerProfiles();
            this.ValidateFiscalDocuments();
            this.ProtectSimulatedFiscalEvidence();
            this.UpdateDonationPeriodoOficial();
            this.RejectOrphanDonationItems();
            this.UpdateDonationConcurrencyTokens();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        /// <summary>
        /// This method is beging called when the model is created in runtime.
        /// </summary>
        /// <param name="modelBuilder">A refence to the <see cref="ModelBuilder"/>.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Invoice>()
                .HasIndex("DonationId")
                .IsUnique();

            modelBuilder.Entity<Invoice>()
                .HasIndex(invoice => new { invoice.Year, invoice.SequenceScope, invoice.Sequence })
                .IsUnique();

            modelBuilder.Entity<PaymentNotifications>()
                .HasIndex("PaymentId", "NotificationType")
                .IsUnique()
                .HasFilter("[PaymentId] IS NOT NULL");

            modelBuilder.Entity<PaymentNotifications>()
                .Property(notification => notification.LastError)
                .HasMaxLength(2048);

            modelBuilder.Entity<PaymentNotifications>()
                .Property(notification => notification.RowVersion)
                .IsRowVersion();

            modelBuilder.Entity<EasyPayBaseClass>()
                .Property(payment => payment.EasyPayPaymentId)
                .HasMaxLength(128);

            modelBuilder.Entity<EasyPayBaseClass>()
                .HasIndex(payment => payment.EasyPayPaymentId)
                .IsUnique()
                .HasFilter("[EasyPayPaymentId] IS NOT NULL");

            modelBuilder.Entity<EmailCommunication>(entity =>
            {
                entity.Property(item => item.FromAddress).HasMaxLength(320).IsRequired();
                entity.Property(item => item.ToAddress).HasMaxLength(320).IsRequired();
                entity.Property(item => item.Subject).HasMaxLength(998);
                entity.HasIndex(item => item.SentAtUtc);
                entity.HasIndex(item => item.UserId);
                entity.HasIndex(item => item.PaymentId);
                entity.HasOne(item => item.User)
                    .WithMany()
                    .HasForeignKey(item => item.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(item => item.Payment)
                    .WithMany()
                    .HasForeignKey(item => item.PaymentId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<FiscalIssuerProfile>(entity =>
            {
                entity.Property(item => item.ScopeKind).IsRequired();
                entity.Property(item => item.LegalName).HasMaxLength(256).IsRequired();
                entity.Property(item => item.LegalAddress).HasMaxLength(1_024).IsRequired();
                entity.Property(item => item.TaxRegistrationNumber).HasMaxLength(20).IsRequired();
                entity.Property(item => item.SaftDocumentType).HasMaxLength(16).IsRequired();
                entity.Property(item => item.TaxRule).HasMaxLength(64).IsRequired();
                entity.Property(item => item.SoftwareCertificateNumber).HasMaxLength(32).IsRequired();
                entity.Property(item => item.SigningKeyIdentifier).HasMaxLength(512).IsRequired();
                entity.Property(item => item.SigningKeyVersion).HasMaxLength(256).IsRequired();
                entity.Property(item => item.ValidFromUtc).IsRequired();
                entity.HasIndex(item => new { item.ScopeKind, item.FoodBankId, item.ValidFromUtc });
                entity.ToTable(table => table.HasCheckConstraint(
                    "CK_FiscalIssuerProfiles_ScopeKind_FoodBankId",
                    "([ScopeKind] = 0 AND [FoodBankId] IS NULL) OR ([ScopeKind] = 1 AND [FoodBankId] IS NOT NULL)"));
                entity.HasOne(item => item.FoodBank)
                    .WithMany()
                    .HasForeignKey(item => item.FoodBankId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<FiscalDocumentSeries>(entity =>
            {
                entity.Property(item => item.SeriesCode).HasMaxLength(64).IsRequired();
                entity.Property(item => item.SaftDocumentType).HasMaxLength(16).IsRequired();
                entity.Property(item => item.AtcudValidationCode).HasMaxLength(64).IsRequired();
                entity.Property(item => item.AtCommunicationReference).HasMaxLength(256);
                entity.Property(item => item.RowVersion).IsRowVersion().IsConcurrencyToken();
                entity.HasAlternateKey(item => new { item.Id, item.FiscalIssuerProfileId });
                entity.HasIndex(item => new { item.FiscalIssuerProfileId, item.SaftDocumentType, item.SeriesCode }).IsUnique();
                entity.HasIndex(item => new { item.FiscalIssuerProfileId, item.AtcudValidationCode }).IsUnique();
                entity.HasOne(item => item.FiscalIssuerProfile)
                    .WithMany(item => item.Series)
                    .HasForeignKey(item => item.FiscalIssuerProfileId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<FiscalDocument>(entity =>
            {
                entity.Property(item => item.InvoicingStrategy).IsRequired();
                entity.Property(item => item.IdempotencyKey).HasMaxLength(128);
                entity.Property(item => item.DocumentNumber).HasMaxLength(128).IsRequired();
                entity.Property(item => item.Atcud).HasMaxLength(128).IsRequired();
                entity.Property(item => item.InvoiceDate).HasColumnType("date").IsRequired();
                entity.Property(item => item.SystemEntryDate).IsRequired();
                entity.Property(item => item.Hash).HasMaxLength(1_024).IsRequired();
                entity.Property(item => item.HashControl).HasMaxLength(128).IsRequired();
                entity.Property(item => item.PreviousHash).HasMaxLength(1_024);
                entity.Property(item => item.SigningKeyIdentifier).HasMaxLength(512).IsRequired();
                entity.Property(item => item.SigningKeyVersion).HasMaxLength(256).IsRequired();
                entity.Property(item => item.SigningAlgorithm).HasMaxLength(64).IsRequired();
                entity.Property(item => item.QrPayload).HasMaxLength(4_000).IsRequired();
                entity.Property(item => item.QrPng).HasColumnType("varbinary(max)").IsRequired();
                entity.Property(item => item.SoftwareCertificateNumber).HasMaxLength(32).IsRequired();
                entity.Property(item => item.IssuerLegalName).HasMaxLength(256).IsRequired();
                entity.Property(item => item.IssuerTaxRegistrationNumber).HasMaxLength(20).IsRequired();
                entity.Property(item => item.IssuerLegalAddress).HasMaxLength(1_024).IsRequired();
                entity.Property(item => item.DonorName).HasMaxLength(256);
                entity.Property(item => item.DonorTaxRegistrationNumber).HasMaxLength(20);
                entity.Property(item => item.TaxRule).HasMaxLength(64).IsRequired();
                entity.Property(item => item.PdfChecksum).HasMaxLength(128);
                entity.Property(item => item.PdfTemplateVersion).HasMaxLength(128);
                entity.Property(item => item.NetTotal).HasPrecision(18, 2);
                entity.Property(item => item.TaxTotal).HasPrecision(18, 2);
                entity.Property(item => item.GrossTotal).HasPrecision(18, 2);
                entity.HasIndex(item => new { item.SeriesId, item.Sequence }).IsUnique();
                entity.HasIndex(item => item.IdempotencyKey)
                    .IsUnique()
                    .HasFilter("[IdempotencyKey] IS NOT NULL");
                entity.HasIndex(item => item.Atcud).IsUnique();
                entity.HasIndex(item => new { item.InvoicingStrategy, item.FoodBankId });
                entity.HasIndex(item => item.DonationId)
                    .IsUnique()
                    .HasFilter("[Kind] = 0 /* Original */");
                entity.HasIndex(item => item.OriginalFiscalDocumentId)
                    .IsUnique()
                    .HasFilter("[Kind] = 1 /* Correction */");
                entity.ToTable(table =>
                {
                    table.HasCheckConstraint(
                        "CK_FiscalDocuments_Kind_OriginalFiscalDocumentId",
                        "([Kind] = 0 AND [OriginalFiscalDocumentId] IS NULL) OR ([Kind] IN (1, 2) AND [OriginalFiscalDocumentId] IS NOT NULL)");
                    table.HasCheckConstraint(
                        "CK_FiscalDocuments_InvoicingStrategy_FoodBankId",
                        "([InvoicingStrategy] = 0 AND [FoodBankId] IS NULL) OR ([InvoicingStrategy] = 1 AND [FoodBankId] IS NOT NULL)");
                });
                entity.HasOne(item => item.Donation)
                    .WithMany()
                    .HasForeignKey(item => item.DonationId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.Series)
                    .WithMany(item => item.Documents)
                    .HasForeignKey(item => new { item.SeriesId, item.FiscalIssuerProfileId })
                    .HasPrincipalKey(item => new { item.Id, item.FiscalIssuerProfileId })
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.FiscalIssuerProfile)
                    .WithMany()
                    .HasForeignKey(item => item.FiscalIssuerProfileId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.OriginalFiscalDocument)
                    .WithMany(item => item.Corrections)
                    .HasForeignKey(item => item.OriginalFiscalDocumentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<FiscalDocumentLine>(entity =>
            {
                entity.Property(item => item.ProductCode).HasMaxLength(64).IsRequired();
                entity.Property(item => item.Description).HasMaxLength(512).IsRequired();
                entity.Property(item => item.Quantity).HasPrecision(18, 3);
                entity.Property(item => item.UnitPrice).HasPrecision(18, 2);
                entity.Property(item => item.TaxRate).HasPrecision(9, 4);
                entity.Property(item => item.TaxExemptionReason).HasMaxLength(256);
                entity.Property(item => item.NetTotal).HasPrecision(18, 2);
                entity.Property(item => item.TaxTotal).HasPrecision(18, 2);
                entity.Property(item => item.GrossTotal).HasPrecision(18, 2);
                entity.HasIndex(item => new { item.FiscalDocumentId, item.LineNumber }).IsUnique();
                entity.HasOne(item => item.FiscalDocument)
                    .WithMany(item => item.Lines)
                    .HasForeignKey(item => item.FiscalDocumentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<FiscalDocumentEvent>(entity =>
            {
                entity.Property(item => item.EventType).HasMaxLength(64).IsRequired();
                entity.Property(item => item.ActorId).HasMaxLength(450);
                entity.Property(item => item.Reason).HasMaxLength(1_024);
                entity.Property(item => item.CorrelationId).HasMaxLength(128);
                entity.HasIndex(item => new { item.FiscalDocumentId, item.OccurredAtUtc });
                entity.HasOne(item => item.FiscalDocument)
                    .WithMany(item => item.Events)
                    .HasForeignKey(item => item.FiscalDocumentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<FiscalAuditEvent>(entity =>
            {
                entity.Property(item => item.Action).HasMaxLength(64).IsRequired();
                entity.Property(item => item.Result).HasMaxLength(64).IsRequired();
                entity.Property(item => item.ActorId).HasMaxLength(450);
                entity.Property(item => item.CorrelationId).HasMaxLength(128);
                entity.HasIndex(item => new { item.TenantId, item.OccurredAtUtc });
                entity.HasIndex(item => item.FiscalDocumentId);
                entity.HasOne(item => item.FiscalIssuerProfile)
                    .WithMany()
                    .HasForeignKey(item => item.FiscalIssuerProfileId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.FiscalDocumentSeries)
                    .WithMany()
                    .HasForeignKey(item => item.FiscalDocumentSeriesId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.FiscalDocument)
                    .WithMany()
                    .HasForeignKey(item => item.FiscalDocumentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SimulatedFiscalDocument>(entity =>
            {
                entity.Property(item => item.TenantId).IsRequired();
                entity.Property(item => item.ScopeKey).HasMaxLength(256).IsRequired();
                entity.Property(item => item.SyntheticIdentifier).HasMaxLength(256).IsRequired();
                entity.Property(item => item.LocalChecksum).HasMaxLength(256).IsRequired();
                entity.Property(item => item.SimulationQrPayload).HasMaxLength(4_000).IsRequired();
                entity.Property(item => item.SimulationQrPng).HasColumnType("varbinary(max)").IsRequired();
                entity.Property(item => item.ArtifactReference).HasMaxLength(1_024).IsRequired();
                entity.Property(item => item.OriginEnvironment).HasMaxLength(64).IsRequired();
                entity.Property(item => item.IssuedAtUtc).IsRequired();
                entity.Property(item => item.CreatedAtUtc).IsRequired();
                entity.Property(item => item.RowVersion).IsRowVersion().IsConcurrencyToken();
                entity.HasIndex(item => item.DonationId).IsUnique();
                entity.HasIndex(item => new { item.TenantId, item.FoodBankId, item.IssuedAtUtc });
                entity.HasIndex(item => new { item.ScopeKey, item.Sequence }).IsUnique();
                entity.HasIndex(item => item.SyntheticIdentifier).IsUnique();
                entity.ToTable(table => table.HasCheckConstraint(
                    "CK_SimulatedFiscalDocuments_InvoicingStrategy_FoodBankId",
                    "([InvoicingStrategy] = 0 AND [FoodBankId] IS NULL) OR ([InvoicingStrategy] = 1 AND [FoodBankId] IS NOT NULL)"));
            });

            modelBuilder.Entity<SimulatedFiscalAuditEvent>(entity =>
            {
                entity.Property(item => item.Action).HasMaxLength(64).IsRequired();
                entity.Property(item => item.Result).HasMaxLength(64).IsRequired();
                entity.Property(item => item.Reason).HasMaxLength(1_024).IsRequired();
                entity.Property(item => item.Mode).HasMaxLength(64).IsRequired();
                entity.Property(item => item.ActorId).HasMaxLength(450);
                entity.Property(item => item.CorrelationId).HasMaxLength(128);
                entity.Property(item => item.RowVersion).IsRowVersion().IsConcurrencyToken();
                entity.HasIndex(item => new { item.TenantId, item.OccurredAtUtc });
                entity.HasIndex(item => item.SimulatedFiscalDocumentId);
                entity.HasOne(item => item.SimulatedFiscalDocument)
                    .WithMany()
                    .HasForeignKey(item => item.SimulatedFiscalDocumentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SimulatedFiscalSequence>(entity =>
            {
                entity.Property(item => item.ScopeKey).HasMaxLength(256).IsRequired();
                entity.Property(item => item.RowVersion).IsRowVersion().IsConcurrencyToken();
                entity.HasIndex(item => item.ScopeKey).IsUnique();
                entity.ToTable(table => table.HasCheckConstraint(
                    "CK_SimulatedFiscalSequences_InvoicingStrategy_FoodBankId",
                    "([InvoicingStrategy] = 0 AND [FoodBankId] IS NULL) OR ([InvoicingStrategy] = 1 AND [FoodBankId] IS NOT NULL)"));
            });

            // modelBuilder.Entity<Invoice>()
            //    .HasIndex(u => u.Sequence)
            //    .IsUnique();
            modelBuilder.Entity<WebUser>(b =>
            {
                // Each User can have many UserClaims
                b.HasMany(e => e.Claims)
                    .WithOne(e => e.User)
                    .HasForeignKey(uc => uc.UserId)
                    .IsRequired();

                // Each User can have many UserLogins
                b.HasMany(e => e.Logins)
                    .WithOne(e => e.User)
                    .HasForeignKey(ul => ul.UserId)
                    .IsRequired();

                // Each User can have many UserTokens
                b.HasMany(e => e.Tokens)
                    .WithOne(e => e.User)
                    .HasForeignKey(ut => ut.UserId)
                    .IsRequired();

                // Each User can have many entries in the UserRole join table
                b.HasMany(e => e.UserRoles)
                    .WithOne(e => e.User)
                    .HasForeignKey(ur => ur.UserId)
                    .IsRequired();
            });

            modelBuilder.Entity<ApplicationRole>(b =>
            {
                // Each Role can have many entries in the UserRole join table
                b.HasMany(e => e.UserRoles)
                    .WithOne(e => e.Role)
                    .HasForeignKey(ur => ur.RoleId)
                    .IsRequired();

                // Each Role can have many associated RoleClaims
                b.HasMany(e => e.RoleClaims)
                    .WithOne(e => e.Role)
                    .HasForeignKey(rc => rc.RoleId)
                    .IsRequired();
            });

            modelBuilder.Entity<Referral>(r =>
            {
                r.HasIndex(e => e.Code)
                    .IsUnique()
                    .HasFilter("[Code] IS NOT NULL");

                r.HasMany(e => e.Donations)
                   .WithOne(d => d.ReferralEntity)
                   .HasForeignKey("ReferralId");

                r.HasMany(e => e.LinkOpens)
                    .WithOne(o => o.Referral)
                    .HasForeignKey(o => o.ReferralId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ReferralLinkOpen>(o =>
            {
                o.HasIndex(x => x.OpenedAtUtc);
                o.HasIndex(x => new { x.ReferralId, x.OpenedAtUtc });
            });

            // modelBuilder.Entity<Donation>(r =>
            // {
            //    r.HasMany(e => e.PaymentList)
            //        .WithOne(d => d.Donation)
            //        .HasForeignKey("DonationId");
            // });
            modelBuilder.Entity<Donation>(r =>
            {
                r.HasOne(e => e.ConfirmedPayment)
                     .WithOne(d => d.Donation)
                     .HasForeignKey(typeof(Donation), "ConfirmedPaymentId");

                r.HasOne(e => e.Campaign)
                    .WithMany()
                    .HasForeignKey(e => e.CampaignId);
            });

            modelBuilder.Entity<DonationItem>(item =>
            {
                item.HasOne(i => i.Donation)
                    .WithMany(d => d.DonationItems)
                    .HasForeignKey("DonationId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserLoginEvent>(e =>
            {
                e.HasIndex(x => x.OccurredAtUtc);
                e.HasIndex(x => new { x.CampaignId, x.LoginProvider });
                e.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Campaign)
                    .WithMany()
                    .HasForeignKey(x => x.CampaignId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // modelBuilder.Entity<Donation>()
            //   .HasOne(d => d.ConfirmedPayment)
            //   .WithOne()
            //   .HasForeignKey<BasePayment>(bp => bp.Donation);
        }

        private void ValidateFiscalIssuerProfiles()
        {
            var invalidIssuerProfile = this.ChangeTracker.Entries<FiscalIssuerProfile>()
                .Where(entry => entry.State == EntityState.Added || entry.State == EntityState.Modified)
                .Select(entry => entry.Entity)
                .FirstOrDefault(profile =>
                    (profile.ScopeKind == FiscalScopeKind.Tenant && profile.FoodBankId.HasValue) ||
                    (profile.ScopeKind == FiscalScopeKind.FoodBank && !profile.FoodBankId.HasValue) ||
                    (profile.ScopeKind != FiscalScopeKind.Tenant && profile.ScopeKind != FiscalScopeKind.FoodBank));

            if (invalidIssuerProfile != null)
            {
                throw new InvalidOperationException(
                    "A FiscalIssuerProfile must have a FoodBankId only when its scope is FoodBank.");
            }
        }

        private void ProtectSimulatedFiscalEvidence()
        {
            bool hasMutableIssuedDocument = this.ChangeTracker.Entries<SimulatedFiscalDocument>()
                .Any(entry => entry.State == EntityState.Modified || entry.State == EntityState.Deleted);
            if (hasMutableIssuedDocument)
            {
                throw new InvalidOperationException("Issued simulated fiscal documents are immutable and cannot be deleted.");
            }

            bool hasMutableAuditEvent = this.ChangeTracker.Entries<SimulatedFiscalAuditEvent>()
                .Any(entry => entry.State == EntityState.Modified || entry.State == EntityState.Deleted);
            if (hasMutableAuditEvent)
            {
                throw new InvalidOperationException("Simulated fiscal audit events are append-only and cannot be changed or deleted.");
            }
        }

        private void ValidateFiscalDocuments()
        {
            var fiscalDocuments = this.ChangeTracker.Entries<FiscalDocument>()
                .Where(entry => entry.State == EntityState.Added || entry.State == EntityState.Modified)
                .Select(entry => entry.Entity)
                .ToList();

            foreach (FiscalDocument fiscalDocument in fiscalDocuments)
            {
                if (!this.IsValidFiscalDocumentScope(fiscalDocument))
                {
                    throw new InvalidOperationException(
                        "A SingleInvoiceTable FiscalDocument must not have a FoodBankId, and a MultipleTablesPerFoodBank FiscalDocument must have one.");
                }

                if (!this.HasValidFiscalDocumentKindReference(fiscalDocument))
                {
                    throw new InvalidOperationException(
                        "An original FiscalDocument cannot reference another document, and corrections or credits must reference an original document.");
                }

                if (fiscalDocument.Kind == FiscalDocumentKind.Correction || fiscalDocument.Kind == FiscalDocumentKind.Credit)
                {
                    FiscalDocument originalDocument = this.FiscalDocuments.Find(fiscalDocument.OriginalFiscalDocumentId.Value);
                    if (!this.IsValidFiscalDocumentCorrection(fiscalDocument, originalDocument))
                    {
                        throw new InvalidOperationException(
                            "A fiscal correction or credit must reference an original document with the same donation, issuer, invoicing strategy, and FoodBank scope.");
                    }
                }
            }
        }

        private bool IsValidFiscalDocumentScope(FiscalDocument fiscalDocument)
        {
            return (fiscalDocument.InvoicingStrategy == InvoicingStrategy.SingleInvoiceTable && !fiscalDocument.FoodBankId.HasValue) ||
                (fiscalDocument.InvoicingStrategy == InvoicingStrategy.MultipleTablesPerFoodBank && fiscalDocument.FoodBankId.HasValue);
        }

        private bool HasValidFiscalDocumentKindReference(FiscalDocument fiscalDocument)
        {
            return (fiscalDocument.Kind == FiscalDocumentKind.Original && !fiscalDocument.OriginalFiscalDocumentId.HasValue) ||
                ((fiscalDocument.Kind == FiscalDocumentKind.Correction || fiscalDocument.Kind == FiscalDocumentKind.Credit) &&
                    fiscalDocument.OriginalFiscalDocumentId.HasValue);
        }

        private bool IsValidFiscalDocumentCorrection(FiscalDocument fiscalDocument, FiscalDocument originalDocument)
        {
            return originalDocument != null &&
                originalDocument.Kind == FiscalDocumentKind.Original &&
                originalDocument.DonationId == fiscalDocument.DonationId &&
                originalDocument.FiscalIssuerProfileId == fiscalDocument.FiscalIssuerProfileId &&
                originalDocument.InvoicingStrategy == fiscalDocument.InvoicingStrategy &&
                originalDocument.FoodBankId == fiscalDocument.FoodBankId;
        }

        private void UpdateDonationPeriodoOficial()
        {
            var donationEntries = this.ChangeTracker.Entries<Donation>()
                .Where(entry => entry.State == EntityState.Added || entry.State == EntityState.Modified)
                .ToList();

            if (donationEntries.Count == 0)
            {
                return;
            }

            var campaignIds = donationEntries
                .Select(entry => entry.Entity.CampaignId)
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .Distinct()
                .ToList();

            var campaignsById = campaignIds.Count == 0
                ? new Dictionary<int, Campaign>()
                : this.Campaigns
                    .AsNoTracking()
                    .Where(campaign => campaignIds.Contains(campaign.Id))
                    .ToDictionary(campaign => campaign.Id);

            foreach (var entry in donationEntries)
            {
                Donation donation = entry.Entity;
                if (!donation.CampaignId.HasValue
                    || !campaignsById.TryGetValue(donation.CampaignId.Value, out Campaign campaign))
                {
                    donation.PeriodoOficial = false;
                    continue;
                }

                donation.PeriodoOficial = DonationPeriodoOficial.IsWithinOfficialPeriod(
                    donation.DonationDate,
                    campaign);
            }
        }

        private void UpdateDonationConcurrencyTokens()
        {
            foreach (var entry in this.ChangeTracker.Entries<Donation>()
                         .Where(entry => entry.State == EntityState.Added || entry.State == EntityState.Modified))
            {
                entry.Entity.ConcurrencyToken = Guid.NewGuid();
            }
        }

        private void RejectOrphanDonationItems()
        {
            var orphanItems = this.ChangeTracker.Entries<DonationItem>()
                .Where(entry => entry.State == EntityState.Added || entry.State == EntityState.Modified)
                .Where(entry => entry.Entity.Donation == null)
                .Select(entry => entry.Entity)
                .ToList();

            if (orphanItems.Count > 0)
            {
                throw new InvalidOperationException(
                    "DonationItem rows must be linked to a Donation before they are saved.");
            }
        }
    }
}
