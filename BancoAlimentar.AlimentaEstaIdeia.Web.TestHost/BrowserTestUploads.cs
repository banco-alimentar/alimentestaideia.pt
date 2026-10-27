// <copyright file="BrowserTestUploads.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>

namespace BancoAlimentar.AlimentaEstaIdeia.Web.TestHost
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.FileProviders;

    /// <summary>Owns disposable upload storage outside the application's source tree.</summary>
    internal sealed class BrowserTestUploads : IDisposable
    {
        public BrowserTestUploads()
        {
            this.Root = Directory.CreateTempSubdirectory("alimentestaideia-browser-").FullName;
            this.Files = new PhysicalFileProvider(this.Root);
        }

        public string Root { get; }

        public PhysicalFileProvider Files { get; }

        public ReferralImageService CreateImageService(IWebHostEnvironment original)
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                ["AzureStorage:ConnectionString"] = string.Empty,
            }).Build();
            return new ReferralImageService(configuration, new UploadEnvironment(original, this));
        }

        public void Dispose()
        {
            this.Files.Dispose();
            if (Directory.Exists(this.Root))
            {
                // Root is created by this instance, never read from user configuration.
                Directory.Delete(this.Root, recursive: true);
            }
        }

        private sealed class UploadEnvironment : IWebHostEnvironment
        {
            public UploadEnvironment(IWebHostEnvironment original, BrowserTestUploads uploads)
            {
                this.ApplicationName = original.ApplicationName;
                this.EnvironmentName = original.EnvironmentName;
                this.ContentRootPath = original.ContentRootPath;
                this.ContentRootFileProvider = original.ContentRootFileProvider;
                this.WebRootPath = uploads.Root;
                this.WebRootFileProvider = uploads.Files;
            }

            public string ApplicationName { get; set; }

            public string EnvironmentName { get; set; }

            public string ContentRootPath { get; set; }

            public IFileProvider ContentRootFileProvider { get; set; }

            public string WebRootPath { get; set; }

            public IFileProvider WebRootFileProvider { get; set; }
        }
    }
}
