﻿// -----------------------------------------------------------------------
// <copyright file="ApplicationUserLogin.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model.Identity
{
    using Microsoft.AspNetCore.Identity;

    /// <summary>
    /// This is the Application user login.
    /// </summary>
    public class ApplicationUserLogin : IdentityUserLogin<string>
    {
        /// <summary>
        /// Gets or sets the <see cref="WebUser"/>.
        /// </summary>
        public virtual WebUser User { get; set; }
    }
}
