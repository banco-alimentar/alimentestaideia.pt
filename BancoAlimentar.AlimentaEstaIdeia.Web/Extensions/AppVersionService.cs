// -----------------------------------------------------------------------
// <copyright file="AppVersionService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Extensions
{
    using Microsoft.AspNetCore.Hosting;
    using System.Reflection;

    public class AppVersionService : IAppVersionService
    {
        private readonly IWebHostEnvironment env;

        public AppVersionService(IWebHostEnvironment env)
        {
            this.env = env;
        }

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
