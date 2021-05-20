// <copyright file="ApplicationDbContext.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
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
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="PaymentItem"/>.
        /// </summary>
        public DbSet<PaymentItem> PaymentItems { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="Subscription"/>.
        /// </summary>
        public DbSet<Subscription> Subscriptions { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="SubscriptionDonations"/>.
        /// </summary>
        public DbSet<SubscriptionDonations> SubscriptionDonations { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="WebUserSubscriptions"/>.
        /// </summary>
        public DbSet<WebUserSubscriptions> UsersSubscriptions { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="Referral"/>.
        /// </summary>
        public DbSet<Referral> Referrals { get; set; }

        /// <summary>
        /// This method is beging called when the model is created in runtime.
        /// </summary>
        /// <param name="modelBuilder">A refence to the <see cref="ModelBuilder"/>.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
                r.HasMany(e => e.Donations)
                   .WithOne(d => d.ReferralEntity)
                   .HasForeignKey("ReferralId");
            });
        }
    }
}
