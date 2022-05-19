// -----------------------------------------------------------------------
// <copyright file="AppVersionService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Common.Services;

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
            string version = "0.0.0.0";
            Assembly current = Assembly.GetEntryAssembly();
            if (current != null)
            {
                AssemblyInformationalVersionAttribute versionAttribute = current.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                if (versionAttribute != null)
                {
                    version = string.Concat(
                        versionAttribute.InformationalVersion,
                        "-",
                        this.env.EnvironmentName);
                }
            }

            return version;
        }
    }
}
