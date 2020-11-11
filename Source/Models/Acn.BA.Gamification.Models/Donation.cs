namespace Acn.BA.Gamification.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Donation")]
    public partial class Donation
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Donation()
        {
            Invites = new HashSet<Invite>();
        }

        public int Id { get; set; }

        public int UserId { get; set; }

        public decimal Amount { get; set; }

        public DateTime CreatedTs { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Invite> Invites { get; set; }

        public virtual User User { get; set; }
    }
}
