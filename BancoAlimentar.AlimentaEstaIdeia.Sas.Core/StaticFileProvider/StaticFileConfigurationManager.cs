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
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Static file configuratio manager.
    /// </summary>
    public static class StaticFileConfigurationManager
    {
        private const string BlobClientKeyName = "__blob_client_static_file_provider";

        /// <summary>
        /// Gets the blob service client for the current tenant.
        /// </summary>
        /// <param name="value">A reference to the <see cref="HttpContextAccessor"/>.</param>
        /// <returns>The Blob Container Client.</returns>
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
        /// Extension method to get the <see cref="BlobContainerClient"/>.
        /// </summary>
        /// <param name="httpContext">A reference to the <see cref="HttpContext"/>.</param>
        /// <param name="configuration">Configuration.</param>
        /// <param name="tenantName">Tenant name.</param>
        internal static void CreateBlobServiceClient(
            HttpContext httpContext,
            IConfiguration configuration,
            string tenantName)
        {
            string itemsKey = string.Concat(BlobClientKeyName, tenantName);
            BlobContainerClient client = new BlobContainerClient(configuration?["AzureStorage:ConnectionString"], tenantName);
            httpContext.Items.Add(itemsKey, client);
        }
    }
}
