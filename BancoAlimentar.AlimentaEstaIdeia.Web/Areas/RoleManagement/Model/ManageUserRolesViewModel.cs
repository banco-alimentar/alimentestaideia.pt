// -----------------------------------------------------------------------
// <copyright file="ManageUserRolesViewModel.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.RoleManagement.Model;

/// <summary>
/// Manage user roles view model.
/// </summary>
public class ManageUserRolesViewModel
{
    /// <summary>
    /// Gets or sets the role id.
    /// </summary>
    public string RoleId { get; set; }

    /// <summary>
    /// Gets or sets the role name.
    /// </summary>
    public string RoleName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the role has been selected or not.
    /// </summary>
    public bool Selected { get; set; }
}
