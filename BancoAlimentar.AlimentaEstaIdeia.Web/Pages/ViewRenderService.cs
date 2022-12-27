// -----------------------------------------------------------------------
// <copyright file="ViewRenderService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.ModelBinding;
    using Microsoft.AspNetCore.Mvc.Razor;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    using Microsoft.AspNetCore.Routing;

    /// <inheritdoc/>
    public class ViewRenderService : IViewRenderService
    {
        private readonly IRazorViewEngine razorViewEngine;
        private readonly ITempDataProvider tempDataProvider;
        private readonly IServiceProvider serviceProvider;
        private readonly IHttpContextAccessor httpContext;
        private readonly IRazorPageActivator activator;

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewRenderService"/> class.
        /// </summary>
        /// <param name="razorViewEngine">Razor View Engine.</param>
        /// <param name="tempDataProvider">Temporal data provider.</param>
        /// <param name="serviceProvider">Service provider.</param>
        /// <param name="httpContext">Http content accessor.</param>
        /// <param name="activator">Razor page activator.</param>
        public ViewRenderService(
            IRazorViewEngine razorViewEngine,
            ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider,
            IHttpContextAccessor httpContext,
            IRazorPageActivator activator)
        {
            this.razorViewEngine = razorViewEngine;
            this.tempDataProvider = tempDataProvider;
            this.serviceProvider = serviceProvider;

            this.httpContext = httpContext;
            this.activator = activator;
        }

        /// <inheritdoc/>
        public async Task<string> RenderToStringAsync(string pageName, string area, PageModel model)
        {
            var actionContext =
                new ActionContext(
                    httpContext.HttpContext,
                    new RouteData(new RouteValueDictionary() { { "area", area }, { "page", pageName } }),
                    new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor() { RouteValues = new Dictionary<string, string>() { { "area", area }, { "page", pageName } } });

            using var sw = new StringWriter();
            var result = razorViewEngine.FindPage(actionContext, pageName);

            if (result.Page == null)
            {
                throw new ArgumentNullException($"The page {pageName} cannot be found.");
            }

            var view = new RazorView(
                razorViewEngine,
                activator,
                new List<IRazorPage>(),
                result.Page,
                HtmlEncoder.Default,
                new DiagnosticListener("ViewRenderService"));

            var viewContext = new ViewContext(
                actionContext,
                view,
                new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = model,
                },
                new TempDataDictionary(
                    httpContext.HttpContext,
                    tempDataProvider),
                sw,
                new HtmlHelperOptions());

            var page = (Page)result.Page;
            page.PageContext = new Microsoft.AspNetCore.Mvc.RazorPages.PageContext
            {
                ViewData = viewContext.ViewData,
            };

            page.ViewContext = viewContext;
            activator.Activate(page, viewContext);
            await page.ExecuteAsync();

            return sw.ToString();
        }
    }
}
