// -----------------------------------------------------------------------
// <copyright file="TenantStaticFileProvider.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.StaticFileProvider
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Specialized;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.StaticFileProvider;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.FileProviders.Physical;
    using Microsoft.Extensions.Primitives;
    using StackExchange.Profiling;

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
            return this.physicalFileProvider.GetDirectoryContents(subpath);
        }

        /// <inheritdoc/>
        public IFileInfo GetFileInfo(string subpath)
        {
            using Timing? root = MiniProfiler.Current.Step("TenantStaticFileProvider.GetFileInfo");
            Timing? getBlobServiceClient = MiniProfiler.Current.Step("TenantStaticFileProvider.GetBlobServiceClient");
            BlobContainerClient? client = this.httpContextAccessor.GetBlobServiceClient();
            PhysicalFileProvider? localCache = this.httpContextAccessor.GetPhysicalFileProvider();
            getBlobServiceClient?.Stop();
            string remoteSubpath = string.Concat("/wwwroot", subpath);

            if (localCache != null)
            {
                PhysicalFileInfo? fileInfo = localCache.GetFileInfo(remoteSubpath) as PhysicalFileInfo;
                if (fileInfo != null && fileInfo.Exists)
                {
                    return localCache.GetFileInfo(remoteSubpath);
                }
            }
            else if (client!.GetBlobClient(remoteSubpath).Exists().Value)
            {
                return new TenantStaticFileInfo(client.GetBlobBaseClient(remoteSubpath));
            }

            return this.physicalFileProvider.GetFileInfo(subpath);
        }

        /// <inheritdoc/>
        public IChangeToken Watch(string filter)
        {
            return this.Watch(filter);
        }
    }
}
