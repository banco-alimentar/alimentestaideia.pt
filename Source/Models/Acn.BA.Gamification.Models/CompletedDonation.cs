namespace Acn.BA.Gamification.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("CompletedDonation")]
    public partial class CompletedDonation
    {
        public int Id { get; set; }

        [Required, MaxLength(256)]
        public string Email { get; set; }

        [Required, MaxLength(256)]
        public string Name { get; set; }

        public decimal Amount { get; set; }

        [MaxLength(256)]
        public string User1Name { get; set; }
        
        [MaxLength(256)]
        public string User1Email { get; set; }

        [MaxLength(256)]
        public string User2Name { get; set; }

        [MaxLength(256)]
        public string User2Email { get; set; }

        [MaxLength(256)]
        public string User3Name { get; set; }

        [MaxLength(256)]
        public string User3Email { get; set; }

        public string LoadError { get; set; }

        [NotMapped]
        public int InviteCount {
            get {
                int res = 0;
                if (!String.IsNullOrEmpty(User1Email) && !String.IsNullOrEmpty(User1Name)) res++;
                if (!String.IsNullOrEmpty(User2Email) && !String.IsNullOrEmpty(User2Name)) res++;
                if (!String.IsNullOrEmpty(User3Email) && !String.IsNullOrEmpty(User3Name)) res++;

                return res;
            }
        }
    }
}
