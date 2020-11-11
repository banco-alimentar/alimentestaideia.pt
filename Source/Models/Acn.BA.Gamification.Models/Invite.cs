//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Acn.BA.Gamification.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class Invite
    {
        public int Id { get; set; }
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }
        public string Nickname { get; set; }
        public System.DateTime LastPokeTs { get; set; }
        public int DonationId { get; set; }
        public System.DateTime CreatedTs { get; set; }
    
        public virtual User InvitedBy { get; set; }
        public virtual User Invited { get; set; }
        public virtual Donation Donation { get; set; }
    }
}
