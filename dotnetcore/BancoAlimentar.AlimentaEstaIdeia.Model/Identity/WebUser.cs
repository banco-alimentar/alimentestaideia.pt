// <copyright file="WebUser.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>

namespace BancoAlimentar.AlimentaEstaIdeia.Model.Identity
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using Microsoft.AspNetCore.Identity;

    /// <summary>
    /// This is the default identity class.
    /// </summary>
    public class WebUser : IdentityUser
    {
        /// <summary>
        /// Gets or sets the prefered food bank for the user.
        /// </summary>
        public string PreferedFoodBank { get; set; }

        /// <summary>
        /// Gets or sets the NIF for the user.
        /// </summary>
        [Column("NIF")]
        [StringLength(20)]
        public string Nif { get; set; }

        /// <summary>
        /// Gets or sets the Company name for the user.
        /// </summary>
        [StringLength(256)]
        public string CompanyName { get; set; }

        /// <summary>
        /// Gets or sets a reference to the <see cref="DonorAddress"/> Address.
        /// </summary>
        public DonorAddress Address { get; set; }

        /// <summary>
        /// Gets or sets the full name of the user.
        /// </summary>
        public string FullName { get; set; }
    }
}
