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
        /// <summary>
        /// Extension method to get the <see cref="BlobContainerClient"/>.
        /// </summary>
        /// <param name="value">A reference to the <see cref="HttpContextAccessor"/>.</param>
        /// <returns>The Blob Container Client.</returns>
        public static BlobContainerClient CreateBlobServiceClient(this IHttpContextAccessor value)
        {
            IConfiguration? configuration = value.HttpContext?.RequestServices.GetRequiredService<IConfiguration>();
            Model.Tenant? tenant = value.HttpContext?.GetTenant();
            string? tenantName = tenant?.Name;
            tenantName = tenantName?.Replace("-", string.Empty);
            tenantName = tenantName?.Replace(".", string.Empty);
            BlobContainerClient client = new BlobContainerClient(configuration?["AzureStorage:ConnectionString"], tenantName);
            return client;
        }
    }
}
