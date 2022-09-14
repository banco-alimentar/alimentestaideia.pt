// -----------------------------------------------------------------------
// <copyright file="DeploymentTemplateProvider.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Deployment
{
    using System;
    using Azure.Core;
    using Azure.Identity;
    using Azure.Storage.Blobs;
    using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

    /// <summary>
    /// A provider class to retrieve an ARM template from storage account.
    /// </summary>
    public class DeploymentTemplateProvider
    {
        private readonly BlobClient blobClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeploymentTemplateProvider"/> class.
        /// </summary>
        public DeploymentTemplateProvider(TemplateStorageOptions options)
        {
            _ = options?.BlobUrl ?? throw new ArgumentNullException(nameof(options));

            TokenCredential cred = new ManagedIdentityCredential();
            Uri blobUrl = new (options.BlobUrl);
            this.blobClient = new (blobUrl, cred);
        }

        /// <summary>
        /// Downloads the ARM template from the storage account.
        /// </summary>
        /// <returns>The <see cref="BinaryData"/> containing the binary content of the file.</returns>
        public async Task<BinaryData> GetArmTemplateAsync()
        {
            var rslt = await this.blobClient.DownloadContentAsync();

            return rslt.Value.Content;
        }
    }
}
