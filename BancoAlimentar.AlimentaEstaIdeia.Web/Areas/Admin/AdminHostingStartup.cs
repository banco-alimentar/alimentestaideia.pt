// -----------------------------------------------------------------------
// <copyright file="AdminHostingStartup.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

[assembly: Microsoft.AspNetCore.Hosting.HostingStartup(typeof(BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.AdminHostingStartup))]

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin
{
    using Microsoft.AspNetCore.Hosting;

    public class AdminHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) =>
            {
            });
        }
    }
}
