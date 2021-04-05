// -----------------------------------------------------------------------
// <copyright file="ApplicationUserClaim.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model.Identity
{
    using Microsoft.AspNetCore.Identity;

    /// <summary>
    /// This is the application user claim.
    /// </summary>
    public class ApplicationUserClaim : IdentityUserClaim<string>
    {
        /// <summary>
        /// Gets or sets the <see cref="WebUser"/>.
        /// </summary>
        public virtual WebUser User { get; set; }
    }
}
