// -----------------------------------------------------------------------
// <copyright file="ApplicationRole.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model.Identity
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Identity;

    /// <summary>
    /// This is the custom role.
    /// </summary>
    public class ApplicationRole : IdentityRole
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationRole"/> class.
        /// </summary>
        public ApplicationRole()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationRole"/> class.
        /// </summary>
        /// <param name="rolName">Name of the role.</param>
        public ApplicationRole(string rolName)
            : base(rolName)
        {
        }

        /// <summary>
        /// Gets or sets the user roles.
        /// </summary>
        public virtual ICollection<ApplicationUserRole> UserRoles { get; set; }

        /// <summary>
        /// Gets or sets the role claims.
        /// </summary>
        public virtual ICollection<ApplicationRoleClaim> RoleClaims { get; set; }
    }
}
