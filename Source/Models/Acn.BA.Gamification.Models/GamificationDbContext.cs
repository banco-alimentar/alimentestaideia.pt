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

            // User configuration contains a private variable
            modelBuilder.Configurations.Add(new User.UserConfiguration());
        }
    }
}
