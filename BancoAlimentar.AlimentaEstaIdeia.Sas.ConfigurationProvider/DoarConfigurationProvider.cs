// -----------------------------------------------------------------------
// <copyright file="DoarConfigurationProvider.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    ///  This is the Doar SaS configuration provider that map the current tenant to their configuration.
    /// </summary>
    public class DoarConfigurationProvider : Microsoft.Extensions.Configuration.ConfigurationProvider
    {
        private readonly IHttpContextAccessor httpContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="DoarConfigurationProvider"/> class.
        /// </summary>
        /// <param name="httpContext">A reference to the HttpContextAccessor.</param>
        public DoarConfigurationProvider(IHttpContextAccessor httpContext)
        {
            this.httpContext = httpContext;
        }
    }
}
