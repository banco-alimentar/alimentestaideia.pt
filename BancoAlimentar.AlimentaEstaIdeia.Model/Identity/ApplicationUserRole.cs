// -----------------------------------------------------------------------
// <copyright file="ApplicationUserRole.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model.Identity;

using Microsoft.AspNetCore.Identity;

/// <summary>
/// This is the Application user role.
/// </summary>
public class ApplicationUserRole : IdentityUserRole<string>
{
    /// <summary>
    /// Gets or sets the <see cref="WebUser"/>.
    /// </summary>
    public virtual WebUser User { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="ApplicationRole"/>.
    /// </summary>
    public virtual ApplicationRole Role { get; set; }
}
