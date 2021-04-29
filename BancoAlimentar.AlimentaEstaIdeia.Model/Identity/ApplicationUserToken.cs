// -----------------------------------------------------------------------
// <copyright file="ApplicationUserToken.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model.Identity
{
    using Microsoft.AspNetCore.Identity;

    /// <summary>
    /// This is the Application User Token.
    /// </summary>
    public class ApplicationUserToken : IdentityUserToken<string>
    {
        /// <summary>
        /// Gets or sets the <see cref="WebUser"/>.
        /// </summary>
        public virtual WebUser User { get; set; }
    }
}
