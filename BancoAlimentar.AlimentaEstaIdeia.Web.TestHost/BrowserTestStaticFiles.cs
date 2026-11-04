// -----------------------------------------------------------------------
// <copyright file="BrowserTestStaticFiles.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.TestHost
{
    using System;
    using System.IO;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;

    /// <summary>Serves bundled assets locally, without the deployed tenant blob fallback.</summary>
    internal sealed class BrowserTestStaticFiles : IStartupFilter
    {
        private readonly BrowserTestUploads uploads;

        public BrowserTestStaticFiles(BrowserTestUploads uploads)
        {
            this.uploads = uploads;
        }

        /// <inheritdoc/>
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                // Identity templates use /Identity/lib whereas this repository bundles /lib.
                // Serve both paths before tenant middleware can consult blob storage for a missing file.
                app.Use(async (context, remaining) =>
                {
                    PathString original = context.Request.Path;
                    if (original.StartsWithSegments("/Identity/lib", out PathString asset))
                    {
                        context.Request.Path = "/lib" + asset.Value.Replace("/jquery-validation/dist/", "/jquery-validation/", StringComparison.Ordinal);
                    }

                    try
                    {
                        await remaining();
                    }
                    finally
                    {
                        context.Request.Path = original;
                    }
                });
                app.UseStaticFiles(new StaticFileOptions { FileProvider = this.uploads.Files });
                app.UseStaticFiles();
                app.Use(async (context, remaining) =>
                {
                    string extension = Path.GetExtension(context.Request.Path.Value).ToLowerInvariant();
                    if (extension is ".js" or ".css" or ".png" or ".jpg" or ".jpeg" or ".svg" or ".ico" or ".woff" or ".woff2" or ".ttf" or ".map")
                    {
                        context.Response.StatusCode = 404;
                        return;
                    }

                    await remaining();
                });
                next(app);
            };
        }
    }
}
