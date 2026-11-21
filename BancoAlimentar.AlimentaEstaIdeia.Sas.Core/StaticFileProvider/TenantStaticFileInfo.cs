// -----------------------------------------------------------------------
// <copyright file="TenantStaticFileInfo.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.StaticFileProvider
{
    using System;
    using System.IO;
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
            : this(blob, blob.GetProperties().Value)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantStaticFileInfo"/> class using known metadata.
        /// </summary>
        /// <param name="blob">Blob client.</param>
        /// <param name="properties">Existing blob metadata.</param>
        public TenantStaticFileInfo(BlobBaseClient blob, BlobProperties properties)
        {
            ArgumentNullException.ThrowIfNull(blob);
            ArgumentNullException.ThrowIfNull(properties);
            this.blob = blob;
            this.properties = properties;
        }

        /// <inheritdoc/>
        public bool Exists => true;

        /// <inheritdoc/>
        public bool IsDirectory => false;

        /// <inheritdoc/>
        public DateTimeOffset LastModified => this.properties.LastModified;

        /// <inheritdoc/>
        public long Length => this.properties.ContentLength;

        /// <inheritdoc/>
        public string Name => Path.GetFileName(this.blob.Name.Replace('\\', '/'));

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
