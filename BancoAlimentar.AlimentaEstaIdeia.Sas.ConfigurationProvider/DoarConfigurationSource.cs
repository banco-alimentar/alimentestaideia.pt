// -----------------------------------------------------------------------
// <copyright file="DoarConfigurationSource.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// This is the Doar SaS configuration source implementation.
    /// </summary>
    public class DoarConfigurationSource : IConfigurationSource
    {
        private readonly IHttpContextAccessor httpContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="DoarConfigurationSource"/> class.
        /// </summary>
        /// <param name="httpContext">A reference to the HttpContextAccessor.</param>
        public DoarConfigurationSource(IHttpContextAccessor httpContext)
        {
            this.httpContext = httpContext;
        }

        /// <summary>
        /// Builds the configuration source.
        /// </summary>
        /// <param name="builder">A reference to the <see cref="IConfigurationBuilder"/>.</param>
        /// <returns>The <see cref="DoarConfigurationProvider"/> instance.</returns>
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new DoarConfigurationProvider(this.httpContext);
        }
    }
}
