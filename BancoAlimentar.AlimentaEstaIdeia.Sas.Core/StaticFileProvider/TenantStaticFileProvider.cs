// -----------------------------------------------------------------------
// <copyright file="TenantStaticFileProvider.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.StaticFileProvider
{
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Specialized;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.FileProviders.Physical;
    using Microsoft.Extensions.Primitives;

    /// <summary>
    /// Tenant static file provider backed in Azure Storage.
    /// </summary>
    public class TenantStaticFileProvider : IFileProvider
    {
        private readonly PhysicalFileProvider physicalFileProvider;
        private readonly IHttpContextAccessor httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantStaticFileProvider"/> class.
        /// </summary>
        /// <param name="physicalFileProvider">Existing physical file providers.</param>
        /// <param name="httpContextAccessor">Http context accessor.</param>
        public TenantStaticFileProvider(
            PhysicalFileProvider physicalFileProvider,
            IHttpContextAccessor httpContextAccessor)
        {
            this.physicalFileProvider = physicalFileProvider;
            this.httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc/>
        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            IDirectoryContents physicalContents = this.physicalFileProvider.GetDirectoryContents(subpath);
            if (physicalContents.Exists)
            {
                return physicalContents;
            }

            string blobDirectoryPrefix = StaticFileConfigurationManager.MapWebPathToBlobName(subpath);
            if (!blobDirectoryPrefix.EndsWith('/'))
            {
                blobDirectoryPrefix += "/";
            }

            PhysicalFileProvider? localCache = this.httpContextAccessor.GetPhysicalFileProvider();
            if (localCache != null)
            {
                IDirectoryContents cachedContents = localCache.GetDirectoryContents(blobDirectoryPrefix);
                if (cachedContents.Exists)
                {
                    return cachedContents;
                }
            }

            BlobContainerClient? client = this.httpContextAccessor.GetBlobServiceClient();
            if (client != null)
            {
                return new TenantStaticBlobPrefixDirectoryContents(client, blobDirectoryPrefix);
            }

            return physicalContents;
        }

        /// <inheritdoc/>
        public IFileInfo GetFileInfo(string subpath)
        {
            BlobContainerClient? client = this.httpContextAccessor.GetBlobServiceClient();
            PhysicalFileProvider? localCache = this.httpContextAccessor.GetPhysicalFileProvider();
            string blobPath = StaticFileConfigurationManager.MapWebPathToBlobName(subpath);

            if (localCache != null)
            {
                PhysicalFileInfo? fileInfo = localCache.GetFileInfo(blobPath) as PhysicalFileInfo;
                if (fileInfo != null && fileInfo.Exists)
                {
                    return localCache.GetFileInfo(blobPath);
                }
            }

            if (client != null && client.GetBlobClient(blobPath).Exists().Value)
            {
                return new TenantStaticFileInfo(client.GetBlobBaseClient(blobPath));
            }

            return this.physicalFileProvider.GetFileInfo(subpath);
        }

        /// <inheritdoc/>
        public IChangeToken Watch(string filter)
        {
            return this.physicalFileProvider.Watch(filter);
        }
    }
}
