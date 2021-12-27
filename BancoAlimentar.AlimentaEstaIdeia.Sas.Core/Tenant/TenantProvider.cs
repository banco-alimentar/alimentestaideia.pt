// -----------------------------------------------------------------------
// <copyright file="TenantProvider.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    /// <inheritdoc/>
    public class TenantProvider : ITenantProvider
    {
        private readonly IHttpContextAccessor httpContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantProvider"/> class.
        /// </summary>
        /// <param name="httpContext">Http context accessor.</param>
        public TenantProvider(IHttpContextAccessor httpContext)
        {
            this.httpContext = httpContext;
        }

        /// <inheritdoc/>
        public TenantData GetTenantData()
        {
            throw new NotImplementedException();
        }
    }
}
