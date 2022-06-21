// -----------------------------------------------------------------------
// <copyright file="DoarFluidViewEngineOptionsSetup.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Common.Fluid
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.StaticFileProvider;
    using global::Fluid;
    using global::Fluid.MvcViewEngine;
    using global::Fluid.ViewEngine;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Define the multitenancy fluid view engine configuration.
    /// </summary>
    public class DoarFluidViewEngineOptionsSetup : ConfigureOptions<FluidMvcViewOptions>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DoarFluidViewEngineOptionsSetup"/> class.
        /// </summary>
        /// <param name="webHostEnvironment">Web hosting environment.</param>
        /// <param name="httpContextAccessor">Http context accessor.</param>
        public DoarFluidViewEngineOptionsSetup(
            IWebHostEnvironment webHostEnvironment,
            IHttpContextAccessor httpContextAccessor)
            : base(options =>
            {
                options.PartialsFileProvider = new TenantStaticFileProvider(
                    webHostEnvironment.ContentRootFileProvider,
                    httpContextAccessor,
                    "Views");
                options.ViewsFileProvider = new TenantStaticFileProvider(
                    webHostEnvironment.ContentRootFileProvider,
                    httpContextAccessor,
                    "Views");
                options.TemplateOptions.MemberAccessStrategy = UnsafeMemberAccessStrategy.Instance;
                options.ViewsLocationFormats.Clear();
                options.ViewsLocationFormats.Add("/{1}/{0}" + Constants.ViewExtension);
                options.ViewsLocationFormats.Add("/Shared/{0}" + Constants.ViewExtension);

                options.PartialsLocationFormats.Clear();
                options.PartialsLocationFormats.Add("{0}" + Constants.ViewExtension);
                options.PartialsLocationFormats.Add("/Partials/{0}" + Constants.ViewExtension);
                options.PartialsLocationFormats.Add("/Partials/{1}/{0}" + Constants.ViewExtension);
                options.PartialsLocationFormats.Add("/Shared/Partials/{0}" + Constants.ViewExtension);

                options.LayoutsLocationFormats.Clear();
                options.LayoutsLocationFormats.Add("/Shared/{0}" + Constants.ViewExtension);
            })
        {
        }
    }
}
