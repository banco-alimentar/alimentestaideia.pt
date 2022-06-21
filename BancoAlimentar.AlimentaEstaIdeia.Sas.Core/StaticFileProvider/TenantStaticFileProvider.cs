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
    using Microsoft.Extensions.Primitives;

    /// <summary>
    /// Tenant static file provider backed in Azure Storage.
    /// </summary>
    public class TenantStaticFileProvider : IFileProvider
    {
        private readonly IFileProvider physicalFileProvider;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly string mappedFolder;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantStaticFileProvider"/> class.
        /// </summary>
        /// <param name="contentFileProvider">Existing physical file providers.</param>
        /// <param name="httpContextAccessor">Http context accessor.</param>
        /// <param name="mappedFolder">Mapped folder.</param>
        public TenantStaticFileProvider(
            IFileProvider contentFileProvider,
            IHttpContextAccessor httpContextAccessor,
            string mappedFolder)
        {
            this.physicalFileProvider = contentFileProvider;
            this.httpContextAccessor = httpContextAccessor;
            this.mappedFolder = mappedFolder;
        }

        /// <inheritdoc/>
        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            return this.physicalFileProvider.GetDirectoryContents(subpath);
        }

        /// <inheritdoc/>
        public IFileInfo GetFileInfo(string subpath)
        {
            BlobContainerClient client = this.httpContextAccessor.CreateBlobServiceClient();
            string remoteSubpath = string.Concat(this.mappedFolder, subpath);
            if (client.GetBlobClient(remoteSubpath).Exists().Value)
            {
                return new TenantStaticFileInfo(client.GetBlobBaseClient(remoteSubpath));
            }

            IFileInfo file = this.physicalFileProvider.GetFileInfo(subpath);
            if (!file.Exists)
            {
                file = this.physicalFileProvider.GetFileInfo(remoteSubpath);
            }

            return file;
        }

        /// <inheritdoc/>
        public IChangeToken Watch(string filter)
        {
            return this.physicalFileProvider.Watch(filter);
        }
    }
}
