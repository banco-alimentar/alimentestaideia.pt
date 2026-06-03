// -----------------------------------------------------------------------
// <copyright file="ITenantStaticLocalCacheService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.StaticFileProvider
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Manages the on-disk cache of tenant static files copied from blob storage.
    /// </summary>
    public interface ITenantStaticLocalCacheService
    {
        /// <summary>
        /// Deletes all locally cached static files for a tenant.
        /// </summary>
        /// <param name="tenantPublicId">Tenant public ID.</param>
        /// <returns>Operation result.</returns>
        TenantStaticLocalCacheResult Clear(Guid tenantPublicId);

        /// <summary>
        /// Downloads tenant static files from blob storage into the local cache.
        /// </summary>
        /// <param name="tenantPublicId">Tenant public ID.</param>
        /// <param name="blobContainerName">Tenant blob container name.</param>
        /// <param name="storageConnectionString">Azure Storage connection string.</param>
        /// <param name="onlyIfSizeChanged">When true, skip blobs whose cached file size already matches.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Operation result.</returns>
        Task<TenantStaticLocalCacheResult> ResyncFromBlobAsync(
            Guid tenantPublicId,
            string blobContainerName,
            string storageConnectionString,
            bool onlyIfSizeChanged = false,
            CancellationToken cancellationToken = default);
    }
}
