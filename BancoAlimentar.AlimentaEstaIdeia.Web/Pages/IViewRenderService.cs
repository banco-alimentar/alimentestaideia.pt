// -----------------------------------------------------------------------
// <copyright file="IViewRenderService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// Render view in memory service.
    /// </summary>
    public interface IViewRenderService
    {
        /// <summary>
        /// Renders page into a string.
        /// </summary>
        /// <typeparam name="T">Type of the page to render.</typeparam>
        /// <param name="viewName">View name.</param>
        /// <param name="area">Area name.</param>
        /// <param name="model">Page model.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<string> RenderToStringAsync<T>(string viewName, string area, T model)
            where T : PageModel;
    }
}
