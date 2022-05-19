// -----------------------------------------------------------------------
// <copyright file="AdminHostingStartup.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

[assembly: Microsoft.AspNetCore.Hosting.HostingStartup(typeof(BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.AdminHostingStartup))]

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin;

using Microsoft.AspNetCore.Hosting;

/// <summary>
/// Setup the admin area.
/// </summary>
public class AdminHostingStartup : IHostingStartup
{
    /// <summary>
    /// Configure the Admin area.
    /// </summary>
    /// <param name="builder">A reference to the <see cref="IWebHostBuilder"/>.</param>
    public void Configure(IWebHostBuilder builder)
    {
        builder.ConfigureServices((context, services) =>
        {
        });
    }
}
