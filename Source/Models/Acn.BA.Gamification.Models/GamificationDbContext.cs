namespace Acn.BA.Gamification.Models
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class GamificationDbContext : DbContext
    {
        public GamificationDbContext()
            : base("name=GamificationDb")
        {
        }

        public virtual DbSet<CompletedDonation> CompletedDonation { get; set; }
        public virtual DbSet<Donation> Donation { get; set; }
        public virtual DbSet<Invite> Invite { get; set; }
        public virtual DbSet<User> User { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CompletedDonation>()
                .Property(e => e.Amount)
                .HasPrecision(18, 0);

            modelBuilder.Entity<Donation>()
                .Property(e => e.Amount)
                .HasPrecision(18, 0);

            modelBuilder.Entity<Donation>()
                .HasMany(e => e.Invites)
                .WithRequired(e => e.Donation)
                .HasForeignKey(e => e.DonationId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<User>()
                .HasMany(e => e.Donation)
                .WithRequired(e => e.User)
                .HasForeignKey(e => e.UserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<User>()
                .HasMany(e => e.Invited)
                .WithRequired(e => e.UserFrom)
                .HasForeignKey(e => e.FromUserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<User>()
                .HasMany(e => e.InvitedBy)
                .WithRequired(e => e.UserTo)
                .HasForeignKey(e => e.ToUserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
        }
    }
}
