// -----------------------------------------------------------------------
// <copyright file="Thanks.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages.Tenants.BancoAlimentar.Pages
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Security.Claims;
    using global::BancoAlimentar.AlimentaEstaIdeia.Model;
    using global::BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using global::BancoAlimentar.AlimentaEstaIdeia.Repository;
    using global::BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// Thanks Model.
    /// </summary>
    public class ThanksModel : PageModel
    {
        /// <summary>
        /// Execute the get operation.
        /// </summary>
        public void OnGet()
        {
        }
    }
}
