// -----------------------------------------------------------------------
// <copyright file="AdminHomepageImageTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Campaigns;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.Mvc.Routing;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.Data.Sqlite;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Localization;
    using Moq;
    using Xunit;

    /// <summary>
    /// Integration tests for uploading and selecting homepage image versions.
    /// </summary>
    public sealed class AdminHomepageImageTests
    {
        private static readonly byte[] MinimalPng = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

        /// <summary>
        /// Uploading an image retains the previous version and makes the new version current.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task Upload_MakesNewVersionCurrent_AndKeepsPreviousVersion()
        {
            await using TestFixture fixture = await TestFixture.CreateAsync();
            HomepageImageVersion previousImage = await fixture.AddImageVersionAsync("previous.png", isCurrent: true);
            var model = fixture.CreatePageModel();
            model.UploadedImage = CreateFormFile(MinimalPng, "upload.png");

            IActionResult result = await model.OnPostUploadAsync(CancellationToken.None);

            Assert.IsType<RedirectToPageResult>(result);
            HomepageImageVersion[] versions = await fixture.Context.HomepageImageVersions
                .AsNoTracking()
                .OrderBy(image => image.Id)
                .ToArrayAsync();
            HomepageImageVersion uploadedImage = Assert.Single(versions, image => image.IsCurrent);
            HomepageImageVersion retainedPreviousImage = Assert.Single(versions, image => image.Id == previousImage.Id);

            Assert.False(retainedPreviousImage.IsCurrent);
            Assert.NotEqual("upload.png", uploadedImage.FileName);
            Assert.Equal("image/png", uploadedImage.ContentType);
            Assert.Equal("test-admin-id", uploadedImage.UploadedByUserId);
            Assert.Equal("test-admin@example.test", uploadedImage.UploadedByName);
            Assert.InRange(uploadedImage.UploadedAtUtc, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
            Assert.Equal(MinimalPng, await File.ReadAllBytesAsync(fixture.Storage.GetPath(uploadedImage.FileName)));
        }

        /// <summary>
        /// Restoring an older image changes the current version without deleting either version.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task Restore_PreviousVersion_MakesItCurrent_AndRetainsHistory()
        {
            await using TestFixture fixture = await TestFixture.CreateAsync();
            HomepageImageVersion previousImage = await fixture.AddImageVersionAsync("previous.png", isCurrent: false);
            HomepageImageVersion currentImage = await fixture.AddImageVersionAsync("current.png", isCurrent: true);
            var model = fixture.CreatePageModel();

            IActionResult result = await model.OnPostRestoreAsync(previousImage.Id, CancellationToken.None);

            Assert.IsType<RedirectToPageResult>(result);
            HomepageImageVersion[] versions = await fixture.Context.HomepageImageVersions
                .AsNoTracking()
                .OrderBy(image => image.Id)
                .ToArrayAsync();
            Assert.Equal(2, versions.Length);
            Assert.True(Assert.Single(versions, image => image.Id == previousImage.Id).IsCurrent);
            Assert.False(Assert.Single(versions, image => image.Id == currentImage.Id).IsCurrent);
            Assert.Single(versions, image => image.IsCurrent);
        }

        private static FormFile CreateFormFile(byte[] contents, string fileName)
        {
            var stream = new MemoryStream(contents);
            return new FormFile(stream, 0, stream.Length, "UploadedImage", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png",
            };
        }

        private sealed class TestFixture : IAsyncDisposable
        {
            private readonly SqliteConnection connection;
            private readonly string storagePath;

            private TestFixture(SqliteConnection connection, ApplicationDbContext context, string storagePath)
            {
                this.connection = connection;
                this.Context = context;
                this.storagePath = storagePath;

                var configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string>
                    {
                        ["HomepageImages:StoragePath"] = storagePath,
                    })
                    .Build();
                var environment = new Mock<IWebHostEnvironment>();
                environment.SetupGet(value => value.ContentRootPath).Returns(Path.GetTempPath());
                this.Storage = new HomepageImageStorage(environment.Object, configuration);
            }

            public ApplicationDbContext Context { get; }

            public HomepageImageStorage Storage { get; }

            public static async Task<TestFixture> CreateAsync()
            {
                var connection = new SqliteConnection("Data Source=:memory:");
                await connection.OpenAsync();
                var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseSqlite(connection)
                    .Options;
                var context = new ApplicationDbContext(options);
                await context.Database.EnsureCreatedAsync();
                string storagePath = Path.Combine(Path.GetTempPath(), "homepage-image-tests-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(storagePath);
                return new TestFixture(connection, context, storagePath);
            }

            public async Task<HomepageImageVersion> AddImageVersionAsync(string fileName, bool isCurrent)
            {
                await File.WriteAllBytesAsync(this.Storage.GetPath(fileName), MinimalPng);
                var image = new HomepageImageVersion
                {
                    FileName = fileName,
                    ContentType = "image/png",
                    UploadedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                    UploadedByUserId = "seed-user",
                    UploadedByName = "Seed user",
                    IsCurrent = isCurrent,
                };
                this.Context.HomepageImageVersions.Add(image);
                await this.Context.SaveChangesAsync();
                return image;
            }

            public IndexModel CreatePageModel()
            {
                var httpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, "test-admin-id"),
                        new Claim(ClaimTypes.Name, "test-admin@example.test"),
                    ],
                    "Test")),
                };
                var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor());
                var pageContext = new PageContext(actionContext);
                var model = new IndexModel(
                    this.Context,
                    this.Storage,
                    Mock.Of<IStringLocalizer<AdminSharedResources>>())
                {
                    PageContext = pageContext,
                    TempData = Mock.Of<ITempDataDictionary>(),
                };
                return model;
            }

            public async ValueTask DisposeAsync()
            {
                await this.Context.DisposeAsync();
                await this.connection.DisposeAsync();
                if (Directory.Exists(this.storagePath))
                {
                    Directory.Delete(this.storagePath, recursive: true);
                }
            }
        }
    }
}
