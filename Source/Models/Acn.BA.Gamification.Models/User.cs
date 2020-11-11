namespace Acn.BA.Gamification.Models
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.ModelConfiguration;
    using System.Data.Entity.Spatial;
    using System.Linq;

    [Table("User")]
    public partial class User
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public User()
        {
            Donation = new HashSet<Donation>();
            Invited = new HashSet<Invite>();
            InvitedBy = new HashSet<Invite>();
        }

        public int Id { get; set; }

        [Required, MaxLength(256)]
        public string Name { get; set; }

        [Required, MaxLength(256)]
        public string Email { get; set; }

        [Required]
        public string SessionCode { get; set; }

        private string _badges { get; set; }
        public List<Badge> Badges { 
            get {
                if (_badges != null)
                {
                    return JsonConvert.DeserializeObject<List<int>>(_badges)
                        .Select(b => Badge.Parse(b))
                        .ToList();
                }
                else
                    return new List<Badge>();
            }
            set {
                _badges = JsonConvert.SerializeObject(value.Select(b => b.Id).ToList());
            }
        }

        public DateTime CreatedTs { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Donation> Donation { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Invite> Invited { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Invite> InvitedBy { get; set; }



        public class UserConfiguration : EntityTypeConfiguration<User>
        {
            public UserConfiguration()
            {
                Property(p => p._badges).HasColumnName("Badges");
                Ignore(u => u.Badges);

                HasMany(e => e.Donation)
                .WithRequired(e => e.User)
                .HasForeignKey(e => e.UserId)
                .WillCascadeOnDelete(false);

                HasMany(e => e.Invited)
                .WithRequired(e => e.UserFrom)
                .HasForeignKey(e => e.FromUserId)
                .WillCascadeOnDelete(false);

                HasMany(e => e.InvitedBy)
                .WithRequired(e => e.UserTo)
                .HasForeignKey(e => e.ToUserId)
                .WillCascadeOnDelete(false);

                HasIndex(u => u.Email)
                .IsUnique();
            }
        }
    }
}
