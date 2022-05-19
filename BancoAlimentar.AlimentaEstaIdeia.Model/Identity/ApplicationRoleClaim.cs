// -----------------------------------------------------------------------
// <copyright file="ApplicationRoleClaim.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model.Identity;

using Microsoft.AspNetCore.Identity;

/// <summary>
/// This is the application role claim.
/// </summary>
public class ApplicationRoleClaim : IdentityRoleClaim<string>
{
    /// <summary>
    /// Gets or sets the <see cref="ApplicationRole"/>.
    /// </summary>
    public virtual ApplicationRole Role { get; set; }
}
