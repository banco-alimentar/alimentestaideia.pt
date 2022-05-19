// -----------------------------------------------------------------------
// <copyright file="TenantStaticDirectoryContents.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.StaticFileProvider;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.FileProviders;

/// <summary>
/// Tenant static directory content for Azure Storage.
/// </summary>
public class TenantStaticDirectoryContents : IDirectoryContents
{
    private readonly BlobContainerClient container;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantStaticDirectoryContents"/> class.
    /// </summary>
    /// <param name="container">Blob container client.</param>
    public TenantStaticDirectoryContents(BlobContainerClient container)
    {
        this.container = container;
    }

    /// <inheritdoc/>
    public bool Exists => this.container.Exists().Value;

    /// <inheritdoc/>
    public IEnumerator<IFileInfo> GetEnumerator()
    {
        foreach (var item in this.container.GetBlobs(Azure.Storage.Blobs.Models.BlobTraits.None)
            .Select(p => new TenantStaticFileInfo(this.container.GetBlobBaseClient(p.Name)))
            .AsEnumerable())
        {
            yield return item;
        }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        foreach (var item in this.container.GetBlobs(Azure.Storage.Blobs.Models.BlobTraits.None)
            .Select(p => new TenantStaticFileInfo(this.container.GetBlobBaseClient(p.Name)))
            .AsEnumerable())
        {
            yield return item;
        }
    }
}
