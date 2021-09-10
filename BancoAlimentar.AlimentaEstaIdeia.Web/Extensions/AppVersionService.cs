// -----------------------------------------------------------------------
// <copyright file="AppVersionService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Extensions
{
    using System.Reflection;
    using Microsoft.AspNetCore.Hosting;

    /// <inheritdoc/>
    public class AppVersionService : IAppVersionService
    {
        private readonly IWebHostEnvironment env;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppVersionService"/> class.
        /// </summary>
        /// <param name="env">Web host environment.</param>
        public AppVersionService(IWebHostEnvironment env)
        {
            this.env = env;
        }

        /// <inheritdoc/>
        public string Version
        {
            get
            {
                return string.Concat(
                    Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion,
                    "-",
                    this.env.EnvironmentName);
            }
        }
    }
}
