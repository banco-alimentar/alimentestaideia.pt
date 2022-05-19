// -----------------------------------------------------------------------
// <copyright file="LoginSharedModel.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Model.Pages.Shared;

using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication;

/// <summary>
/// PageModel for the LoginShared partial view.
/// </summary>
public class LoginSharedModel
{
    /// <summary>
    /// Gets or sets the collection of extenal logins available on the web site.
    /// </summary>
    public IList<AuthenticationScheme> ExternalLogins { get; set; }

    /// <summary>
    /// Gets or sets the return url.
    /// </summary>
    public string ReturnUrl { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user is logged in or not.
    /// </summary>
    public bool IsUserLogged { get; set; }
}
