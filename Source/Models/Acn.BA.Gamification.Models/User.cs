namespace Acn.BA.Gamification.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

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

        public DateTime CreatedTs { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Donation> Donation { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Invite> Invited { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Invite> InvitedBy { get; set; }
    }
}
