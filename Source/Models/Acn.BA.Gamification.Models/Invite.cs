namespace Acn.BA.Gamification.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Invite")]
    public partial class Invite
    {
        public int Id { get; set; }

        public int FromUserId { get; set; }

        public int ToUserId { get; set; }

        [Required, MaxLength(256)]
        public string Nickname { get; set; }

        public DateTime LastPokeTs { get; set; }

        public int DonationId { get; set; }

        public DateTime CreatedTs { get; set; }

        public virtual Donation Donation { get; set; }

        public virtual User UserFrom { get; set; }

        public virtual User UserTo { get; set; }
    }
}
