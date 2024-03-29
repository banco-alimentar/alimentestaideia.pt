﻿// -----------------------------------------------------------------------
// <copyright file="Logout.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account
{
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Logout model.
    /// </summary>
    [AllowAnonymous]
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<WebUser> signInManager;
        private readonly ILogger<LogoutModel> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogoutModel"/> class.
        /// </summary>
        /// <param name="signInManager">Sign in manager.</param>
        /// <param name="logger">Logger.</param>
        public LogoutModel(SignInManager<WebUser> signInManager, ILogger<LogoutModel> logger)
        {
            this.signInManager = signInManager;
            this.logger = logger;
        }

        /// <summary>
        /// Execute the post operation.
        /// </summary>
        /// <param name="returnUrl">Return url.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            await signInManager.SignOutAsync();
            logger.LogInformation("User logged out.");
            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                return RedirectToPage();
            }
        }
    }
}
