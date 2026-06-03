// -----------------------------------------------------------------------
// <copyright file="TenantStaticLocalCacheResult.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.StaticFileProvider
{
    /// <summary>
    /// Result of a tenant static local cache operation.
    /// </summary>
    public class TenantStaticLocalCacheResult
    {
        /// <summary>
        /// Gets or sets the tenant cache root directory.
        /// </summary>
        public string CacheRootPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of files removed from local cache.
        /// </summary>
        public int FilesRemoved { get; set; }

        /// <summary>
        /// Gets or sets the number of files downloaded from blob storage.
        /// </summary>
        public int FilesDownloaded { get; set; }
    }
}
