// -----------------------------------------------------------------------
// <copyright file="TenantLayout.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Layout
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;

    /// <inheritdoc/>
    public class TenantLayout : ITenantLayout
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IWebHostEnvironment webHostEnvironment;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantLayout"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">Httpcontextaccessor.</param>
        /// <param name="webHostEnvironment">IWebHostEnvironment.</param>
        public TenantLayout(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment webHostEnvironment)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.webHostEnvironment = webHostEnvironment;
        }

        /// <inheritdoc/>
        public string Layout
        {
            get
            {
                if (this.httpContextAccessor.HttpContext != null)
                {
                    Tenant tenant = this.httpContextAccessor.HttpContext.GetTenant();
                    string layoutPath = $"/pages/tenants/{tenant.NormalizedName}/Pages/_Layout.cshtml";
                    return layoutPath;
                }

                return "_Layout";
            }
        }

        /// <inheritdoc/>
        public string AdminLayout
        {
            get
            {
                if (this.httpContextAccessor.HttpContext != null)
                {
                    Tenant tenant = this.httpContextAccessor.HttpContext.GetTenant();
                    string layoutPath = $"/pages/tenants/{tenant.NormalizedName}/Pages/_Layout.cshtml";
                    return layoutPath;
                }

                return "/Pages/Shared/_Layout.cshtml";
            }
        }

        /// <inheritdoc/>
        public string Debug
        {
            get
            {
                if (this.httpContextAccessor.HttpContext != null)
                {
                    Tenant tenant = this.httpContextAccessor.HttpContext.GetTenant();
                    string layoutPath = $"/pages/tenants/{tenant.NormalizedName}/Pages/_Layout.cshtml";
                    return layoutPath;
                }
                else
                {
                    return "HttpContext is null";
                }
            }
        }
    }
}
