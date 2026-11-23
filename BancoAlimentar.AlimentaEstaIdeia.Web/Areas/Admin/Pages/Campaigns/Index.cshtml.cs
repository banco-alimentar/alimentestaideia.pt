// -----------------------------------------------------------------------
// <copyright file="Index.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Campaigns
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Lists campaigns and manages the homepage central image.
    /// </summary>
    [RequestSizeLimit(11 * 1024 * 1024)]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext context;
        private readonly HomepageImageStorage imageStorage;
        private readonly IStringLocalizer<AdminSharedResources> localizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexModel"/> class.
        /// </summary>
        /// <param name="context">Application database context.</param>
        /// <param name="imageStorage">Homepage image storage.</param>
        /// <param name="localizer">Admin shared localizer.</param>
        public IndexModel(
            ApplicationDbContext context,
            HomepageImageStorage imageStorage,
            IStringLocalizer<AdminSharedResources> localizer)
        {
            this.context = context;
            this.imageStorage = imageStorage;
            this.localizer = localizer;
        }

        /// <summary>
        /// Gets or sets the list of campaigns.
        /// </summary>
        public IList<Campaign> Campaign { get; set; } = new List<Campaign>();

        /// <summary>
        /// Gets the next scheduled campaign.
        /// </summary>
        public Campaign NextCampaign { get; private set; }

        /// <summary>
        /// Gets the uploaded homepage image versions.
        /// </summary>
        public IList<HomepageImageVersion> ImageVersions { get; private set; } = new List<HomepageImageVersion>();

        /// <summary>
        /// Gets or sets an uploaded homepage image.
        /// </summary>
        [BindProperty]
        public IFormFile UploadedImage { get; set; }

        /// <summary>
        /// Loads campaigns and image version history.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task OnGetAsync()
        {
            await this.LoadPageDataAsync();
        }

        /// <summary>
        /// Uploads and selects a new homepage image version.
        /// </summary>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>The current page or a redirect after success.</returns>
        public async Task<IActionResult> OnPostUploadAsync(CancellationToken cancellationToken)
        {
            StoredHomepageImage storedImage;
            try
            {
                storedImage = await this.imageStorage.StoreAsync(this.UploadedImage, cancellationToken);
            }
            catch (InvalidDataException exception)
            {
                string errorKey = exception.Message.Contains("10 MB", StringComparison.Ordinal)
                    ? "HomepageImageUploadInvalidSize"
                    : "HomepageImageUploadInvalidType";
                this.ModelState.AddModelError(nameof(this.UploadedImage), this.localizer[errorKey].Value);
                await this.LoadPageDataAsync();
                return this.Page();
            }

            DateTime uploadedAtUtc = DateTime.UtcNow;
            string uploadedByUserId = this.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? this.User.Identity?.Name ?? "unknown";
            string uploadedByName = this.User.Identity?.Name ?? "unknown";
            var executionStrategy = this.context.Database.CreateExecutionStrategy();
            await executionStrategy.ExecuteAsync(async () =>
            {
                this.context.ChangeTracker.Clear();
                await using var transaction = await this.context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

                foreach (HomepageImageVersion currentImage in await this.context.HomepageImageVersions
                    .Where(image => image.IsCurrent)
                    .ToListAsync(cancellationToken))
                {
                    currentImage.IsCurrent = false;
                }

                await this.context.SaveChangesAsync(cancellationToken);

                HomepageImageVersion? uploadedImageVersion = await this.context.HomepageImageVersions
                    .FirstOrDefaultAsync(image => image.FileName == storedImage.FileName, cancellationToken);
                if (uploadedImageVersion == null)
                {
                    uploadedImageVersion = new HomepageImageVersion
                    {
                        FileName = storedImage.FileName,
                        ContentType = storedImage.ContentType,
                        UploadedAtUtc = uploadedAtUtc,
                        UploadedByUserId = uploadedByUserId,
                        UploadedByName = uploadedByName,
                    };
                    this.context.HomepageImageVersions.Add(uploadedImageVersion);
                }

                uploadedImageVersion.IsCurrent = true;
                await this.context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            });

            this.TempData["HomepageImageStatus"] = "Uploaded";
            return this.RedirectToPage();
        }

        /// <summary>
        /// Selects an earlier uploaded image version.
        /// </summary>
        /// <param name="id">Image version identifier.</param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>A redirect to the campaign page or a not-found result.</returns>
        public async Task<IActionResult> OnPostRestoreAsync(int id, CancellationToken cancellationToken)
        {
            bool restored = false;
            var executionStrategy = this.context.Database.CreateExecutionStrategy();
            await executionStrategy.ExecuteAsync(async () =>
            {
                restored = false;
                this.context.ChangeTracker.Clear();
                await using var transaction = await this.context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

                HomepageImageVersion? selectedImage = await this.context.HomepageImageVersions
                    .FirstOrDefaultAsync(image => image.Id == id, cancellationToken);
                if (selectedImage == null || !System.IO.File.Exists(this.imageStorage.GetPath(selectedImage.FileName)))
                {
                    return;
                }

                foreach (HomepageImageVersion image in await this.context.HomepageImageVersions
                    .Where(image => image.IsCurrent)
                    .ToListAsync(cancellationToken))
                {
                    image.IsCurrent = false;
                }

                await this.context.SaveChangesAsync(cancellationToken);
                selectedImage.IsCurrent = true;
                await this.context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                restored = true;
            });

            if (!restored)
            {
                return this.NotFound();
            }

            this.TempData["HomepageImageStatus"] = "Restored";
            return this.RedirectToPage();
        }

        /// <summary>
        /// Serves an uploaded version for its admin preview.
        /// </summary>
        /// <param name="id">Image version identifier.</param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>The image or a not-found result.</returns>
        public async Task<IActionResult> OnGetImageAsync(int id, CancellationToken cancellationToken)
        {
            HomepageImageVersion? image = await this.context.HomepageImageVersions
                .AsNoTracking()
                .FirstOrDefaultAsync(version => version.Id == id, cancellationToken);
            if (image == null)
            {
                return this.NotFound();
            }

            string path = this.imageStorage.GetPath(image.FileName);
            if (!System.IO.File.Exists(path))
            {
                return this.NotFound();
            }

            this.Response.Headers["Cache-Control"] = "no-store";
            return this.PhysicalFile(path, image.ContentType);
        }

        private async Task LoadPageDataAsync()
        {
            DateTime now = DateTime.Now;
            this.Campaign = await this.context.Campaigns
                .AsNoTracking()
                .OrderByDescending(campaign => campaign.Start)
                .ThenByDescending(campaign => campaign.Id)
                .ToListAsync();

            this.NextCampaign = await this.context.Campaigns
                .AsNoTracking()
                .Where(campaign => !campaign.IsDefaultCampaign && campaign.Start > now)
                .OrderBy(campaign => campaign.Start)
                .ThenBy(campaign => campaign.Id)
                .FirstOrDefaultAsync();

            this.ImageVersions = await this.context.HomepageImageVersions
                .AsNoTracking()
                .OrderByDescending(image => image.IsCurrent)
                .ThenByDescending(image => image.UploadedAtUtc)
                .ThenByDescending(image => image.Id)
                .ToListAsync();
        }
    }
}
