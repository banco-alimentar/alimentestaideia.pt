// -----------------------------------------------------------------------
// <copyright file="StaticFileConfigurationManager.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.FileProviders;

    /// <summary>
    /// Static file configuration manager.
    /// </summary>
    public static class StaticFileConfigurationManager
    {
        private const string BlobClientKeyName = "__blob_client_static_file_provider";

        /// <summary>
        /// Gets the blob service client for the current tenant.
        /// </summary>
        /// <param name="value">A reference to the <see cref="HttpContextAccessor"/>.</param>
        /// <returns>A reference to the <see cref="BlobContainerClient"/>.</returns>
        public static BlobContainerClient? GetBlobServiceClient(this IHttpContextAccessor value)
        {
            BlobContainerClient? result = null;

            Model.Tenant? tenant = value.HttpContext?.GetTenant();
            string? tenantName = tenant?.NormalizedName;
            string itemsKey = string.Concat(BlobClientKeyName, tenantName);

            bool? exists = value.HttpContext?.Items.ContainsKey(itemsKey);
            if (exists.HasValue && exists.Value)
            {
                object? target = value.HttpContext?.Items[itemsKey];
                if (target != null)
                {
                    result = (BlobContainerClient)target;
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the physical file provider for the local cache.
        /// </summary>
        /// <param name="value">A reference to the <see cref="HttpContextAccessor"/>.</param>
        /// <returns>A reference to the <see cref="PhysicalFileProvider"/>.</returns>
        public static PhysicalFileProvider? GetPhysicalFileProvider(this IHttpContextAccessor value)
        {
            PhysicalFileProvider? result = null;

            Model.Tenant? tenant = value.HttpContext?.GetTenant();
            string? tenantName = tenant?.NormalizedName;
            string physicalFileProviderKey = string.Concat(BlobClientKeyName, "-file.provider-", tenantName);

            bool? exists = value.HttpContext?.Items.ContainsKey(physicalFileProviderKey);
            if (exists.HasValue && exists.Value)
            {
                object? target = value.HttpContext?.Items[physicalFileProviderKey];
                if (target != null)
                {
                    result = (PhysicalFileProvider)target;
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the path for the temporal tenant local file.
        /// </summary>
        /// <param name="tenantPublicId">Tenant Public ID.</param>
        /// <param name="fileName">Target file name.</param>
        /// <returns>The normalized local file.</returns>
        public static string GetTenantLocalTemporalFilePath(Guid tenantPublicId, string fileName)
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "tenants",
                tenantPublicId.ToString(),
                fileName);
        }

        /// <summary>
        /// Extension method to get the <see cref="BlobContainerClient"/>.
        /// </summary>
        /// <param name="httpContext">A reference to the <see cref="HttpContext"/>.</param>
        /// <param name="configuration">Configuration.</param>
        /// <param name="tenantName">Tenant name.</param>
        /// <param name="tenantPublicId">Tenant Public Id.</param>
        internal static void CreateBlobServiceClient(
            HttpContext httpContext,
            IConfiguration configuration,
            string tenantName,
            Guid tenantPublicId)
        {
            string itemsKey = string.Concat(BlobClientKeyName, tenantName);
            string physicalFileProviderKey = string.Concat(BlobClientKeyName, "-file.provider-", tenantName);
            BlobContainerClient client = new BlobContainerClient(configuration?["AzureStorage:ConnectionString"], tenantName);
            httpContext.Items.Add(itemsKey, client);
            string tenantDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "tenants",
                tenantPublicId.ToString());
            if (Directory.Exists(tenantDirectory))
            {
                httpContext.Items.Add(physicalFileProviderKey, new PhysicalFileProvider(tenantDirectory));
            }
        }
    }
}
