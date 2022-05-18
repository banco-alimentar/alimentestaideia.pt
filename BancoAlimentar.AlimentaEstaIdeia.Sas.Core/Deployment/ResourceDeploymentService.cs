// -----------------------------------------------------------------------
// <copyright file="ResourceDeploymentService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Deployment
{
    using System;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Azure.Core;
    using Azure.Identity;
    using Azure.ResourceManager;
    using Azure.ResourceManager.Resources;
    using Azure.ResourceManager.Resources.Models;

    /// <summary>
    /// Service for deploying Azure resources with an ARM template.
    /// </summary>
    public class ResourceDeploymentService
    {
        private readonly ArmClient armClient;
        private readonly List<ArmOperation<ArmDeploymentResource>> operations = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceDeploymentService"/> class.
        /// </summary>
        public ResourceDeploymentService()
        {
            var cred = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeAzureCliCredential = true,
                ExcludeAzurePowerShellCredential = true,
                ExcludeInteractiveBrowserCredential = true,
                ExcludeSharedTokenCacheCredential = true,
                ExcludeVisualStudioCodeCredential = true,
                ExcludeVisualStudioCredential = true,
            });

            this.armClient = new (cred);
        }

        /// <summary>
        /// Initiates a deployment of an ARM template.
        /// </summary>
        /// <param name="subscriptionId">The id of the Azure subscription to deploy to.</param>
        /// <param name="location">The Azure location to deploy to.</param>
        /// <param name="tenantId">The id of the customer tenant.</param>
        /// <returns>The deployment identifier.</returns>
        public async Task<string?> CreateTenantDeploymentAsync(string subscriptionId, string location, string tenantId)
        {
            AzureLocation deployLocation = default;
            var sub = (await this.armClient.GetSubscriptions().GetAsync(subscriptionId)).Value;
            await foreach (var loc in sub.GetLocationsAsync())
            {
                if (location.Equals(loc.Name, StringComparison.OrdinalIgnoreCase) ||
                    location.Equals(loc.DisplayName, StringComparison.OrdinalIgnoreCase))
                {
                    deployLocation = loc;
                    break;
                }
            }

            if (deployLocation == default)
            {
                throw new ArgumentOutOfRangeException(location, $"The location \"{location}\" is not an available Azure location.");
            }

            if (sub != null)
            {
                var deployments = sub.GetArmDeployments();

                var deploymentName = $"{tenantId}-{DateTime.Now:yyyy-MM-dd-HHmm}";

                ArmDeploymentContent deploymentContent = new (new (ArmDeploymentMode.Incremental)
                {
                    Template = await LoadArmTemplate(),
                    Parameters = GetDeploymentParameters(deployLocation.Name, tenantId),
                })
                { Location = deployLocation };

                this.operations.Add(await deployments.CreateOrUpdateAsync(Azure.WaitUntil.Started, deploymentName, deploymentContent));

                var depData = deployments.Where(d => d.HasData && d.Data.Name.Equals(deploymentName, StringComparison.Ordinal))
                           .Select(d => d.Data).FirstOrDefault();

                return depData?.Id?.ToString();
            }

            return null;
        }

        /// <summary>
        /// Loads an ARM template from a file.
        /// </summary>
        /// <returns>The <see cref="BinaryData"/> containing the template data.</returns>
        private static async Task<BinaryData> LoadArmTemplate()
        {
            using FileStream fs = new ("tenant-infra.json", FileMode.Open, FileAccess.Read, FileShare.Read);
            return await BinaryData.FromStreamAsync(fs);
        }

        private static BinaryData GetDeploymentParameters(string location, string tenantId)
        {
            Dictionary<string, ParameterValue> parameters = new ()
            {
                { "tenantId", new ParameterValue(tenantId) },
                { "location", new ParameterValue(location) },
            };

            return BinaryData.FromObjectAsJson<Dictionary<string, ParameterValue>>(parameters);
        }
    }
}
