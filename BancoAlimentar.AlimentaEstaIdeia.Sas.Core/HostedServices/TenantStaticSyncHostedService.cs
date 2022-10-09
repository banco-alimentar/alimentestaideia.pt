// -----------------------------------------------------------------------
// <copyright file="TenantStaticSyncHostedService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.HostedServices
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Autofac.Core;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using Azure.Storage.Blobs.Specialized;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Options;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Repository;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Azure;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    /// <summary>
    /// Synchronize the tenant static files to the AppService local hard drive.
    /// </summary>
    public class TenantStaticSyncHostedService : BackgroundService
    {
        private readonly IServiceProvider services;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantStaticSyncHostedService"/> class.
        /// </summary>
        /// <param name="services">Service provider.</param>
        public TenantStaticSyncHostedService(IServiceProvider services)
        {
            this.services = services;
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (IServiceScope scope = this.services.CreateScope())
            {
                IWebHostEnvironment webHostEnvironment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
                ITenantProvider tenantProvider = scope.ServiceProvider.GetRequiredService<ITenantProvider>();
                IKeyVaultConfigurationManager keyVaultConfigurationManager = scope.ServiceProvider.GetRequiredService<IKeyVaultConfigurationManager>();
                using IInfrastructureUnitOfWork infrastructureUnitOfWork = scope.ServiceProvider.GetRequiredService<IInfrastructureUnitOfWork>();
                List<Model.Tenant> allTenants = infrastructureUnitOfWork
                    .TenantRepository
                    .GetAllTenantForEnvironment(webHostEnvironment.EnvironmentName);

                foreach (var tenant in allTenants)
                {
                    await keyVaultConfigurationManager.EnsureTenantConfigurationLoaded(tenant.Id, TenantDevelopmentOptions.ProductionOptions);
                    var configuration = keyVaultConfigurationManager.GetTenantConfiguration(tenant.Id);
                    BlobContainerClient client = new BlobContainerClient(configuration?["AzureStorage:ConnectionString"], tenant.NormalizedName);
                    List<BlobItem> allBlobs = new List<BlobItem>();
                    var asyncEnumerator = client.GetBlobsAsync(BlobTraits.Metadata, BlobStates.None, "wwwroot/", stoppingToken).GetAsyncEnumerator();
                    try
                    {
                        while (await asyncEnumerator.MoveNextAsync())
                        {
                            BlobBaseClient blobClient = client.GetBlobBaseClient(asyncEnumerator.Current.Name);
                            string targetFile = Path.Combine(
                                    webHostEnvironment.ContentRootPath,
                                    tenant.PublicId.ToString(),
                                    asyncEnumerator.Current.Name);

                            bool needUpdate = false;
                            if (File.Exists(targetFile))
                            {
                                FileInfo fileInfo = new FileInfo(targetFile);
                                if (fileInfo.LastWriteTime != asyncEnumerator.Current.Properties.LastModified)
                                {
                                    needUpdate = true;
                                }
                            }
                            else
                            {
                                needUpdate = true;
                            }

                            if (needUpdate)
                            {
                                await blobClient.DownloadToAsync(
                                    Path.Combine(
                                        webHostEnvironment.ContentRootPath,
                                        tenant.PublicId.ToString(),
                                        asyncEnumerator.Current.Name));
                            }
                        }
                    }
                    finally
                    {
                        await asyncEnumerator.DisposeAsync();
                    }
                }
            }
        }
    }
}
