// -----------------------------------------------------------------------
// <copyright file="IdentityHostingStartup.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Hosting;

[assembly: HostingStartup(typeof(BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.IdentityHostingStartup))]

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity
{
    /// <summary>
    /// Identity hosting startup class.
    /// </summary>
    public class IdentityHostingStartup : IHostingStartup
    {
        /// <summary>
        /// Configure the area.
        /// </summary>
        /// <param name="builder">Web host builder.</param>
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => { });
        }
    }
}