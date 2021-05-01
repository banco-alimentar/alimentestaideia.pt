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
    using Microsoft.AspNetCore.Mvc.Abstractions;
    using Microsoft.AspNetCore.Mvc.Infrastructure;
    using Microsoft.AspNetCore.Mvc.ModelBinding;
    using Microsoft.AspNetCore.Mvc.Razor;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    using Microsoft.AspNetCore.Routing;

    public class ViewRenderService : IViewRenderService
    {
        private readonly IRazorViewEngine razorViewEngine;
        private readonly ITempDataProvider tempDataProvider;
        private readonly IServiceProvider serviceProvider;
        private readonly IHttpContextAccessor httpContext;
        private readonly IActionContextAccessor actionContext;
        private readonly IRazorPageActivator activator;

        public ViewRenderService(
            IRazorViewEngine razorViewEngine,
            ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider,
            IHttpContextAccessor httpContext,
            IRazorPageActivator activator,
            IActionContextAccessor actionContext)
        {
            this.razorViewEngine = razorViewEngine;
            this.tempDataProvider = tempDataProvider;
            this.serviceProvider = serviceProvider;

            this.httpContext = httpContext;
            this.actionContext = actionContext;
            this.activator = activator;
        }

        public async Task<string> RenderToStringAsync<T>(string pageName, string area, T model)
            where T : PageModel
        {
            var actionContext =
                new ActionContext(
                    httpContext.HttpContext,
                    new RouteData(new RouteValueDictionary() { { "area", area }, { "page", pageName } }),
                    new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor() { RouteValues = new Dictionary<string, string>() { { "area", area }, { "page", pageName } } });

            using (var sw = new StringWriter())
            {
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
                    new ViewDataDictionary<T>(new EmptyModelMetadataProvider(), new ModelStateDictionary())
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

        internal class CustomRouter : IRouter
        {
            public VirtualPathData GetVirtualPath(VirtualPathContext context)
            {
                return null;
            }

            public Task RouteAsync(RouteContext context)
            {
                return Task.CompletedTask;
            }
        }

        private ActionContext GetActionContext()
        {
            var httpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider,
            };
            var routeData = new RouteData();
            routeData.Routers.Add(new CustomRouter());
            return new ActionContext(httpContext, routeData, new ActionDescriptor());
        }
    }
}
