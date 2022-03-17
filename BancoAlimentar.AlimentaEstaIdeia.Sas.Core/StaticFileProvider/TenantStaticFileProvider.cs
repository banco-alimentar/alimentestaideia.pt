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
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Primitives;

    /// <summary>
    /// Tenant static file provider backed in Azure Storage.
    /// </summary>
    public class TenantStaticFileProvider : IFileProvider
    {
        private readonly PhysicalFileProvider physicalFileProvider;
        private readonly IHttpContextAccessor contextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantStaticFileProvider"/> class.
        /// </summary>
        /// <param name="physicalFileProvider">Existing physical file providers.</param>
        /// <param name="contextAccessor">Http context accessor.</param>
        public TenantStaticFileProvider(PhysicalFileProvider physicalFileProvider, IHttpContextAccessor contextAccessor)
        {
            this.physicalFileProvider = physicalFileProvider;
            this.contextAccessor = contextAccessor;
        }

        /// <inheritdoc/>
        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            return this.physicalFileProvider.GetDirectoryContents(subpath);
        }

        /// <inheritdoc/>
        public IFileInfo GetFileInfo(string subpath)
        {
            BlobContainerClient client = this.CreateBlobServiceClient();
            subpath = string.Concat("/wwwroot", subpath);
            if (client.GetBlobClient(subpath).Exists().Value)
            {
                return new TenantStaticFileInfo(client.GetBlobBaseClient(subpath));
            }

            return this.physicalFileProvider.GetFileInfo(subpath);
        }

        /// <inheritdoc/>
        public IChangeToken Watch(string filter)
        {
            return this.Watch(filter);
        }

        private BlobContainerClient CreateBlobServiceClient()
        {
            IConfiguration? configuration = this.contextAccessor.HttpContext?.RequestServices.GetRequiredService<IConfiguration>();
            Model.Tenant? tenant = this.contextAccessor.HttpContext?.GetTenant();
            BlobContainerClient client = new BlobContainerClient(configuration?["AzureStorage:ConnectionString"], tenant?.Name);
            return client;
        }
    }
}
