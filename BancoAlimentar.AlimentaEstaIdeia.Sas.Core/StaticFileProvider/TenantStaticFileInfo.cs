// -----------------------------------------------------------------------
// <copyright file="TenantStaticFileInfo.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.StaticFileProvider
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection.Metadata.Ecma335;
    using System.Text;
    using System.Threading.Tasks;
    using Azure.Storage.Blobs.Models;
    using Azure.Storage.Blobs.Specialized;
    using Microsoft.Extensions.FileProviders;
    using StackExchange.Profiling;

    /// <summary>
    /// Represent a blob file.
    /// </summary>
    public class TenantStaticFileInfo : IFileInfo
    {
        private readonly BlobBaseClient blob;
        private readonly BlobProperties properties;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantStaticFileInfo"/> class.
        /// </summary>
        /// <param name="blob">Blob.</param>
        public TenantStaticFileInfo(BlobBaseClient blob)
        {
            using Timing? root = MiniProfiler.Current.Step("TenantStaticFileInfo.ctor()");
            this.blob = blob;
            this.properties = this.blob.GetProperties().Value;
            root.Stop();
        }

        /// <inheritdoc/>
        public bool Exists => this.blob.Exists().Value;

        /// <inheritdoc/>
        public bool IsDirectory => false;

        /// <inheritdoc/>
        public DateTimeOffset LastModified => this.properties.LastModified;

        /// <inheritdoc/>
        public long Length => this.properties.ContentLength;

        /// <inheritdoc/>
        public string Name => this.blob.Name;

        /// <inheritdoc/>
        public string PhysicalPath => string.Empty;

        /// <inheritdoc/>
        public Stream CreateReadStream()
        {
            using Timing? root = MiniProfiler.Current.Step("TenantStaticFileInfo.CreateReadStream");
            return this.blob.OpenRead(new BlobOpenReadOptions(false));
        }
    }
}
