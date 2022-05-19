// -----------------------------------------------------------------------
// <copyright file="DeleteUser.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages;

using BancoAlimentar.AlimentaEstaIdeia.Repository;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// Disable a user.
/// </summary>
public class DeleteUserModel : PageModel
{
    private readonly IUnitOfWork context;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteUserModel"/> class.
    /// </summary>
    /// <param name="context">Unit of work.</param>
    public DeleteUserModel(IUnitOfWork context)
    {
        this.context = context;
    }

    /// <summary>
    /// Execute the get operation.
    /// </summary>
    public void OnGet()
    {
      // this.context.User.DeleteAllUsers();
    }
}
