// <copyright file="WebUser.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>

namespace BancoAlimentar.AlimentaEstaIdeia.Model.Identity
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using Microsoft.AspNetCore.Identity;

    /// <summary>
    /// This is the default identity class.
    /// </summary>
    public class WebUser : IdentityUser
    {
        /// <summary>
        /// Gets or sets the NIF for the user.
        /// </summary>
        [Column("NIF")]
        [StringLength(20)]
        [PersonalData]
        public string Nif { get; set; }

        /// <summary>
        /// Gets or sets the Company name for the user.
        /// </summary>
        [StringLength(256)]
        [PersonalData]
        public string CompanyName { get; set; }

        /// <summary>
        /// Gets or sets a reference to the <see cref="DonorAddress"/> Address.
        /// </summary>
        [PersonalData]
        public DonorAddress Address { get; set; }

        /// <summary>
        /// Gets or sets the full name of the user.
        /// </summary>
        [PersonalData]
        public string FullName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user is Anonymous or not.
        /// </summary>
        public bool IsAnonymous { get; set; }

        /// <summary>
        /// Gets or sets the list of the <see cref="ApplicationUserClaim"/>.
        /// </summary>
        public virtual ICollection<ApplicationUserClaim> Claims { get; set; }

        /// <summary>
        /// Gets or sets the list of the <see cref="ApplicationUserLogin"/>.
        /// </summary>
        public virtual ICollection<ApplicationUserLogin> Logins { get; set; }

        /// <summary>
        /// Gets or sets the list of the <see cref="ApplicationUserToken"/>.
        /// </summary>
        public virtual ICollection<ApplicationUserToken> Tokens { get; set; }

        /// <summary>
        /// Gets or sets the list of the <see cref="ApplicationUserRole"/>.
        /// </summary>
        public virtual ICollection<ApplicationUserRole> UserRoles { get; set; }

        /// <summary>
        /// Gets or sets the list of the <see cref="Subscription"/>.
        /// </summary>
        public virtual ICollection<Subscription> Subscriptions { get; set; }
    }
}
