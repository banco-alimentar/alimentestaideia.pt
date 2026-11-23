// -----------------------------------------------------------------------
// <copyright file="HomepageImageMiddleware.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Serves the selected homepage image without allowing caches to retain an old version.
    /// </summary>
    public class HomepageImageMiddleware
    {
        private readonly RequestDelegate next;

        /// <summary>
        /// Initializes a new instance of the <see cref="HomepageImageMiddleware"/> class.
        /// </summary>
        /// <param name="next">Next middleware.</param>
        public HomepageImageMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        /// <summary>
        /// Serves the selected uploaded image or falls through to the original static image.
        /// </summary>
        /// <param name="context">HTTP context.</param>
        /// <param name="dbContext">Application database context.</param>
        /// <param name="storage">Image storage.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext, HomepageImageStorage storage)
        {
            if (!string.Equals(context.Request.Path.Value, "/img/comida.png", StringComparison.OrdinalIgnoreCase))
            {
                await this.next(context);
                return;
            }

            HomepageImageVersion? selectedImage = await dbContext.HomepageImageVersions
                .AsNoTracking()
                .Where(image => image.IsCurrent)
                .OrderByDescending(image => image.UploadedAtUtc)
                .ThenByDescending(image => image.Id)
                .FirstOrDefaultAsync(context.RequestAborted);

            if (selectedImage == null)
            {
                await this.next(context);
                return;
            }

            string imagePath = storage.GetPath(selectedImage.FileName);
            if (!File.Exists(imagePath))
            {
                await this.next(context);
                return;
            }

            context.Response.ContentType = selectedImage.ContentType;
            context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            context.Response.Headers["Pragma"] = "no-cache";
            context.Response.Headers["Expires"] = "0";
            context.Response.ContentLength = new FileInfo(imagePath).Length;
            await context.Response.SendFileAsync(imagePath, context.RequestAborted);
        }
    }
}
