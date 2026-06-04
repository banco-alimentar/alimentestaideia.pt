// -----------------------------------------------------------------------
// <copyright file="TenantStaticBlobPrefixDirectoryContents.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.StaticFileProvider
{
    using System.Collections;
    using System.Collections.Generic;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using Microsoft.Extensions.FileProviders;

    /// <summary>
    /// Directory listing for a blob prefix within a tenant container.
    /// </summary>
    internal sealed class TenantStaticBlobPrefixDirectoryContents : IDirectoryContents
    {
        private readonly BlobContainerClient container;
        private readonly string prefix;
        private bool? exists;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantStaticBlobPrefixDirectoryContents"/> class.
        /// </summary>
        /// <param name="container">Blob container client.</param>
        /// <param name="prefix">Blob prefix ending with '/'.</param>
        public TenantStaticBlobPrefixDirectoryContents(BlobContainerClient container, string prefix)
        {
            this.container = container;
            this.prefix = prefix;
        }

        /// <inheritdoc/>
        public bool Exists
        {
            get
            {
                if (!this.exists.HasValue)
                {
                    this.exists = false;
                    foreach (BlobHierarchyItem item in this.container.GetBlobsByHierarchy(prefix: this.prefix))
                    {
                        if (item.IsBlob)
                        {
                            this.exists = true;
                            break;
                        }
                    }
                }

                return this.exists.Value;
            }
        }

        /// <inheritdoc/>
        public IEnumerator<IFileInfo> GetEnumerator()
        {
            foreach (BlobHierarchyItem item in this.container.GetBlobsByHierarchy(prefix: this.prefix))
            {
                if (item.IsBlob)
                {
                    yield return new TenantStaticFileInfo(this.container.GetBlobClient(item.Blob.Name));
                }
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
